/*
  Опис файлу: цей модуль містить реалізацію компонента ShopViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
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
    /// <summary>
    /// Визначає публічний елемент `public enum ShopSection` та контракт його використання у шарі WPFApp.
    /// </summary>
    public enum ShopSection
    {
        List,
        Edit,
        Profile
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ShopViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ShopViewModel : ViewModelBase
    {
        private readonly IShopFacade _shopService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
                private int _databaseReloadInProgress;

        
        
        

        private bool _initialized;
        private Task? _initializeTask;
        private readonly object _initLock = new();

        
        private int? _openedProfileShopId;

        private bool _isNavStatusVisible;
        /// <summary>
        /// Визначає публічний елемент `public bool IsNavStatusVisible` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsNavStatusVisible
        {
            get => _isNavStatusVisible;
            private set => SetProperty(ref _isNavStatusVisible, value);
        }

        private UIStatusKind _navStatus = UIStatusKind.Success;
        /// <summary>
        /// Визначає публічний елемент `public UIStatusKind NavStatus` та контракт його використання у шарі WPFApp.
        /// </summary>
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

        
        
        

        private object _currentSection = null!;
        /// <summary>
        /// Визначає публічний елемент `public object CurrentSection` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        private ShopSection _mode = ShopSection.List;
        /// <summary>
        /// Визначає публічний елемент `public ShopSection Mode` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ShopSection CancelTarget { get; private set; } = ShopSection.List;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopSection CancelTarget { get; private set; } = ShopSection.List;

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public ShopListViewModel ListVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopListViewModel ListVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public ShopEditViewModel EditVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopEditViewModel EditVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public ShopProfileViewModel ProfileVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopProfileViewModel ProfileVm { get; }

        /// <summary>
        /// Визначає публічний елемент `public ShopViewModel(` та контракт його використання у шарі WPFApp.
        /// </summary>
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

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public Task EnsureInitializedAsync(CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            
            if (_initialized)
                return Task.CompletedTask;

            
            if (_initializeTask != null)
                return _initializeTask;

            
            lock (_initLock)
            {
                
                if (_initialized)
                    return Task.CompletedTask;

                if (_initializeTask != null)
                    return _initializeTask;

                
                _initializeTask = InitializeCoreAsync(ct);

                return _initializeTask;
            }
        }

        private async Task InitializeCoreAsync(CancellationToken ct)
        {
            try
            {
                
                await LoadShopsAsync(ct, selectId: null);

                
                _initialized = true;
            }
            catch
            {
                
                lock (_initLock)
                {
                    _initializeTask = null;
                    _initialized = false;
                }

                throw;
            }
        }

        
        
        

        internal async Task SearchAsync(CancellationToken ct = default)
        {
            
            var term = ListVm.SearchText;

            
            var list = string.IsNullOrWhiteSpace(term)
                ? await _shopService.GetAllAsync(ct)
                : await _shopService.GetByValueAsync(term, ct);

            
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
            
            var currentId = GetCurrentShopId();
            if (currentId <= 0)
                return;

            
            
            
            var currentName = Mode == ShopSection.Profile
                ? ProfileVm.Name
                : ShopDisplayHelper.NameOrEmpty(ListVm.SelectedItem);

            
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
            
            EditVm.ClearValidationErrors();

            
            return Mode switch
            {
                ShopSection.Edit => CancelTarget == ShopSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),

                _ => SwitchToListAsync()
            };
        }

        
        
        

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
            
            if (Mode == ShopSection.Profile)
                return ProfileVm.ShopId;

            
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
