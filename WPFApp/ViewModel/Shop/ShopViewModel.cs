using System.Windows;
using System.Windows.Threading;
using BusinessLogicLayer.Contracts.Shops;
using BusinessLogicLayer.Services.Abstractions;
using WPFApp.Applications.Notifications;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation.Rules;
using WPFApp.UI.Dialogs;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Dialogs;
using WPFApp.ViewModel.Shop.Helpers;
using WPFApp.ViewModel.Shared;
using WPFApp.Applications.Diagnostics;

namespace WPFApp.ViewModel.Shop
{
    public enum ShopSection
    {
        List,
        Edit,
        Profile
    }

    /// <summary>
    /// ShopViewModel — owner/coordinator модуля Shop.
    ///
    /// Відповідальність:
    /// - тримати 3 під-VM: List/Edit/Profile
    /// - керувати навігацією: CurrentSection + Mode + CancelTarget
    /// - виконувати CRUD через IShopFacade
    /// - показувати повідомлення користувачу
    ///
    /// Оптимізації:
    /// 1) EnsureInitializedAsync:
    ///    - конкурентні виклики чекають один init-task
    ///    - _initialized стає true лише після успішного завершення
    /// 2) Валідація перенесена в ShopValidationRules
    /// 3) Після Save:
    ///    - робимо reload списку
    ///    - відновлюємо selection
    ///    - якщо повертаємось у Profile — refresh profile
    /// </summary>
    public sealed class ShopViewModel : ViewModelBase
    {
        private readonly IShopFacade _shopService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
                private int _databaseReloadInProgress;

        // ----------------------------
        // Initialization (safe, без гонок)
        // ----------------------------

        private bool _initialized;
        private Task? _initializeTask;
        private readonly object _initLock = new();

        // Id профілю, який зараз відкрито (щоб після Save можна було refresh).
        private int? _openedProfileShopId;

        private bool _isNavStatusVisible;
        public bool IsNavStatusVisible
        {
            get => _isNavStatusVisible;
            private set => SetProperty(ref _isNavStatusVisible, value);
        }

        private UIStatusKind _navStatus = UIStatusKind.Success;
        public UIStatusKind NavStatus
        {
            get => _navStatus;
            private set => SetProperty(ref _navStatus, value);
        }

        private CancellationTokenSource? _navUiCts;

        private CancellationToken ResetNavUiCts(CancellationToken outer)
        {
            _navUiCts?.Cancel();
            _navUiCts?.Dispose();
            _navUiCts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            return _navUiCts.Token;
        }

        private Task ShowNavWorkingAsync()
            => RunOnUiThreadAsync(() =>
            {
                NavStatus = UIStatusKind.Working;
                IsNavStatusVisible = true;
            });

        private Task HideNavStatusAsync()
            => RunOnUiThreadAsync(() => IsNavStatusVisible = false);

        private async Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 900)
        {
            await RunOnUiThreadAsync(() =>
            {
                NavStatus = UIStatusKind.Success;
                IsNavStatusVisible = true;
            }).ConfigureAwait(false);

            try { await Task.Delay(ms, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }

            await HideNavStatusAsync().ConfigureAwait(false);
        }

        // ----------------------------
        // Navigation state
        // ----------------------------

        private object _currentSection = null!;
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        private ShopSection _mode = ShopSection.List;
        public ShopSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        /// <summary>
        /// CancelTarget — куди повертаємось з Edit при Cancel (List або Profile).
        /// </summary>
        public ShopSection CancelTarget { get; private set; } = ShopSection.List;

        // ----------------------------
        // Child VMs
        // ----------------------------

        public ShopListViewModel ListVm { get; }
        public ShopEditViewModel EditVm { get; }
        public ShopProfileViewModel ProfileVm { get; }

        public ShopViewModel(
            IShopFacade shopService,
            IDatabaseChangeNotifier databaseChangeNotifier
            )
        {
            _shopService = shopService;
            _databaseChangeNotifier = databaseChangeNotifier;

            ListVm = new ShopListViewModel(this);
            EditVm = new ShopEditViewModel(this);
            ProfileVm = new ShopProfileViewModel(this);

            _databaseChangeNotifier.DatabaseChanged += OnDatabaseChanged;
            CurrentSection = ListVm;
        }

        // =========================================================
        // Initialization
        // =========================================================

        public Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            // 1) Якщо вже ініціалізовано — ок.
            if (_initialized)
                return Task.CompletedTask;

            // 2) Якщо task вже існує — повертаємо його.
            if (_initializeTask != null)
                return _initializeTask;

            // 3) Створюємо init-task під lock, щоб не запускати двічі.
            lock (_initLock)
            {
                // 4) Перевірка повторно, бо поки чекали lock стан міг змінитись.
                if (_initialized)
                    return Task.CompletedTask;

                if (_initializeTask != null)
                    return _initializeTask;

                // 5) Створюємо реальний init-task.
                _initializeTask = InitializeCoreAsync(ct);

                return _initializeTask;
            }
        }

        private async Task InitializeCoreAsync(CancellationToken ct)
        {
            try
            {
                // 1) Завантажуємо початковий список.
                await LoadShopsAsync(ct, selectId: null);

                // 2) Тільки тут ставимо _initialized=true (після успіху).
                _initialized = true;
            }
            catch
            {
                // 3) Якщо впали — дозволяємо повторити init.
                lock (_initLock)
                {
                    _initializeTask = null;
                    _initialized = false;
                }

                throw;
            }
        }

        // =========================================================
        // List flows
        // =========================================================

        internal async Task SearchAsync(CancellationToken ct = default)
        {
            // 1) Беремо search term.
            var term = ListVm.SearchText;

            // 2) Якщо пусто — беремо все, інакше — фільтр.
            var list = string.IsNullOrWhiteSpace(term)
                ? await _shopService.GetAllAsync(ct)
                : await _shopService.GetByValueAsync(term, ct);

            // 3) Віддаємо в ListVm.
            ListVm.SetItems(list);
        }

        internal Task StartAddAsync(CancellationToken ct = default)
            => UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.ResetForNew();
                        CancelTarget = ShopSection.List;
                    });

                    await SwitchToEditAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var latest = await _shopService.GetAsync(selected.Id, uiToken) ?? selected;

                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.SetShop(latest);

                        CancelTarget = Mode == ShopSection.Profile
                            ? ShopSection.Profile
                            : ShopSection.List;
                    });

                    await SwitchToEditAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);
        }

        // =========================================================
        // Save / Delete / Profile flows
        // =========================================================

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var request = EditVm.ToRequest();
            var errors = ShopValidationRules.ValidateAll(request);

            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    ShopDto? createdShop = null;

                    if (EditVm.IsEdit)
                    {
                        await _shopService.UpdateAsync(request, uiToken);
                    }
                    else
                    {
                        createdShop = await _shopService.CreateAsync(request, uiToken);
                        await RunOnUiThreadAsync(() => EditVm.ShopId = createdShop.Id);
                    }

                    var savedShopId = createdShop?.Id ?? request.Id;

                    _databaseChangeNotifier.NotifyDatabaseChanged("Shop.Save");

                    await LoadShopsAsync(uiToken, selectId: savedShopId);

                    if (CancelTarget == ShopSection.Profile)
                    {
                        var profileId = _openedProfileShopId ?? savedShopId;

                        if (profileId > 0)
                        {
                            var latest = await _shopService.GetAsync(profileId, uiToken)
                                         ?? createdShop
                                         ?? ListVm.SelectedItem;

                            if (latest != null)
                            {
                                await RunOnUiThreadAsync(() =>
                                {
                                    ProfileVm.SetProfile(latest);
                                    ListVm.SelectedItem = latest;
                                });
                            }
                        }

                        await SwitchToProfileAsync();
                    }
                    else
                    {
                        await SwitchToListAsync();
                    }
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            // 1) Визначаємо currentId (із профілю або зі списку).
            var currentId = GetCurrentShopId();
            if (currentId <= 0)
                return;

            // 2) Для confirm беремо ім’я:
            //    - якщо у Profile — там вже є Name
            //    - якщо у List — беремо зі SelectedItem
            var currentName = Mode == ShopSection.Profile
                ? ProfileVm.Name
                : ShopDisplayHelper.NameOrEmpty(ListVm.SelectedItem);

            // 3) Confirm.
            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                    ? "Delete shop?"
                    : $"Delete {currentName}?"))
            {
                return;
            }

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await _shopService.DeleteAsync(currentId, uiToken);

                    _databaseChangeNotifier.NotifyDatabaseChanged("Shop.Delete");
                    await LoadShopsAsync(uiToken, selectId: null);
                    await SwitchToListAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var latest = await _shopService.GetAsync(selected.Id, uiToken) ?? selected;

                    await RunOnUiThreadAsync(() =>
                    {
                        _openedProfileShopId = latest.Id;
                        ProfileVm.SetProfile(latest);
                        ListVm.SelectedItem = latest;
                        CancelTarget = ShopSection.List;
                    });

                    await SwitchToProfileAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        internal Task CancelAsync()
        {
            // 1) При Cancel чистимо помилки Edit.
            EditVm.ClearValidationErrors();

            // 2) Навігація залежить від Mode та CancelTarget.
            return Mode switch
            {
                ShopSection.Edit => CancelTarget == ShopSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),

                _ => SwitchToListAsync()
            };
        }

        // =========================================================
        // Load + navigation helpers
        // =========================================================

        private async Task LoadShopsAsync(CancellationToken ct, int? selectId)
        {
            var list = (await _shopService.GetAllAsync(ct)).ToList();

            await RunOnUiThreadAsync(() =>
            {
                ListVm.SetItems(list);

                if (selectId.HasValue)
                    ListVm.SelectedItem = list.FirstOrDefault(s => s.Id == selectId.Value);
            });
        }

        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = ShopSection.List;
            return Task.CompletedTask;
        }

        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = ShopSection.Edit;
            return Task.CompletedTask;
        }

        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = ShopSection.Profile;
            return Task.CompletedTask;
        }

        private int GetCurrentShopId()
        {
            // Якщо ми в профілі — беремо ShopId з ProfileVm.
            if (Mode == ShopSection.Profile)
                return ProfileVm.ShopId;

            // Інакше — зі списку.
            return ListVm.SelectedItem?.Id ?? 0;
        }


        private void OnDatabaseChanged(object? sender, DatabaseChangedEventArgs e)
        {
            _ = ReloadAfterDatabaseChangeAsync(e.Source);
        }

        private async Task ReloadAfterDatabaseChangeAsync(string source)
        {
            if (!_initialized)
                return;

            if (Interlocked.Exchange(ref _databaseReloadInProgress, 1) == 1)
                return;

            try
            {
                if (Mode == ShopSection.Edit)
                {
                                        return;
                }

                var selectedId = Mode == ShopSection.Profile ? ProfileVm.ShopId : ListVm.SelectedItem?.Id;
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadShopsAsync(CancellationToken.None, selectedId);

                    if (Mode == ShopSection.Profile && selectedId.HasValue)
                    {
                        var latest = await _shopService.GetAsync(selectedId.Value, CancellationToken.None);
                        if (latest != null)
                        {
                            ProfileVm.SetProfile(latest);
                            ListVm.SelectedItem = latest;
                        }
                        else
                        {
                            await SwitchToListAsync();
                        }
                    }
                }).Task.Unwrap();

                            }
            catch (Exception ex)
            {
                            }
            finally
            {
                Interlocked.Exchange(ref _databaseReloadInProgress, 0);
            }
        }


        private Task WaitForUiIdleAsync()
            => Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;

        internal Task RunOnUiThreadAsync(Action action)
        {
            var d = Application.Current?.Dispatcher;
            if (d is null || d.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return d.InvokeAsync(action).Task;
        }

        // =========================================================
        // UI messaging (як у твоєму проєкті)
        // =========================================================

        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        internal void ShowError(Exception ex)
        {
            var (summary, details) = ExceptionMessageBuilder.Build(ex);
            CustomMessageBox.Show("Error", summary, CustomMessageBoxIcon.Error, okText: "OK", details: details);
        }

        private bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");
    }
}
