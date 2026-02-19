using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.Validation;
using WPFApp.Service;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Dialogs;
using WPFApp.ViewModel.Employee.Helpers;

namespace WPFApp.ViewModel.Employee
{
    public enum EmployeeSection
    {
        List,
        Edit,
        Profile
    }

    /// <summary>
    /// EmployeeViewModel — owner/coordinator модуля Employee.
    ///
    /// Відповідальність:
    /// - тримати 3 під-VM (List/Edit/Profile)
    /// - керувати навігацією (CurrentSection + Mode + CancelTarget)
    /// - виконувати CRUD через IEmployeeService
    /// - показувати повідомлення користувачу
    ///
    /// Покращення порівняно з початковим варіантом:
    /// 1) EnsureInitializedAsync:
    ///    - не виставляємо _initialized=true ДО успішного завершення
    ///    - конкурентні виклики чекають один task (без дублювання Load)
    /// 2) Валідація: використовуємо EmployeeValidationRules (без залежності від EditVM.Regex).
    /// 3) Менше дублювання форматування імен: EmployeeDisplayHelper.
    /// </summary>
    public sealed class EmployeeViewModel : ViewModelBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
        private readonly ILoggerService _logger;
        private int _databaseReloadInProgress;

        // ----------------------------
        // Initialization (safe)
        // ----------------------------

        private bool _initialized;
        private Task? _initializeTask;
        private readonly object _initLock = new();

        // Пам’ятаємо id відкритого профілю (щоб після Save повернутись і refreshнути).
        private int? _openedProfileEmployeeId;

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

        private EmployeeSection _mode = EmployeeSection.List;
        public EmployeeSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        public EmployeeSection CancelTarget { get; private set; } = EmployeeSection.List;

        // ----------------------------
        // Child VMs
        // ----------------------------

        public EmployeeListViewModel ListVm { get; }
        public EmployeeEditViewModel EditVm { get; }
        public EmployeeProfileViewModel ProfileVm { get; }

        public EmployeeViewModel(
            IEmployeeService employeeService,
            IDatabaseChangeNotifier databaseChangeNotifier,
            ILoggerService logger)
        {
            _employeeService = employeeService;
            _databaseChangeNotifier = databaseChangeNotifier;
            _logger = logger;

            ListVm = new EmployeeListViewModel(this);
            EditVm = new EmployeeEditViewModel(this);
            ProfileVm = new EmployeeProfileViewModel(this);

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

            // 2) Якщо task вже існує — повертаємо його (усі чекатимуть одне).
            if (_initializeTask != null)
                return _initializeTask;

            // 3) Під lock створюємо init-task один раз.
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
                // 1) Завантажуємо список працівників.
                await LoadEmployeesAsync(ct, selectId: null);

                // 2) Фіксуємо успішну ініціалізацію.
                _initialized = true;
            }
            catch
            {
                // Якщо впали — дозволяємо повторити init.
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
            var term = ListVm.SearchText;

            var list = string.IsNullOrWhiteSpace(term)
                ? await _employeeService.GetAllAsync(ct)
                : await _employeeService.GetByValueAsync(term, ct);

            ListVm.SetItems(list);
        }

        internal async Task StartAddAsync(CancellationToken ct = default)
        {
            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await RunOnUiThreadAsync(() =>
                {
                    EditVm.ResetForNew();
                    CancelTarget = EmployeeSection.List;
                });

                await SwitchToEditAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 700);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                var latest = await _employeeService.GetAsync(selected.Id, uiToken) ?? selected;

                await RunOnUiThreadAsync(() =>
                {
                    EditVm.SetEmployee(latest);

                    CancelTarget = Mode == EmployeeSection.Profile
                        ? EmployeeSection.Profile
                        : EmployeeSection.List;
                });

                await SwitchToEditAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 700);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        // =========================================================
        // Save/Delete/Profile flows
        // =========================================================

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var model = EditVm.ToModel();
            var errors = EmployeeValidationRules.ValidateAll(model);

            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                if (EditVm.IsEdit)
                {
                    await _employeeService.UpdateAsync(model, uiToken);
                }
                else
                {
                    var created = await _employeeService.CreateAsync(model, uiToken);

                    await RunOnUiThreadAsync(() => EditVm.EmployeeId = created.Id);

                    model = created;
                }

                _databaseChangeNotifier.NotifyDatabaseChanged("Employee.Save");

                await LoadEmployeesAsync(uiToken, selectId: model.Id);

                if (CancelTarget == EmployeeSection.Profile)
                {
                    var profileId = _openedProfileEmployeeId ?? model.Id;

                    if (profileId > 0)
                    {
                        var latest = await _employeeService.GetAsync(profileId, uiToken) ?? model;

                        await RunOnUiThreadAsync(() =>
                        {
                            ProfileVm.SetProfile(latest);
                            ListVm.SelectedItem = latest;
                        });
                    }

                    await SwitchToProfileAsync();
                }
                else
                {
                    await SwitchToListAsync();
                }

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 900);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            // 1) Визначаємо “поточний id” залежно від режиму (Profile або List).
            var currentId = GetCurrentEmployeeId();
            if (currentId <= 0)
                return;

            // 2) Формуємо “поточне ім’я” для confirm.
            var currentName = GetCurrentEmployeeName();

            // 3) Confirm.
            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                ? "Delete employee?"
                : $"Delete {currentName}?"))
            {
                return;
            }

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await _employeeService.DeleteAsync(currentId, uiToken);

                _databaseChangeNotifier.NotifyDatabaseChanged("Employee.Delete");
                await LoadEmployeesAsync(uiToken, selectId: null);
                await SwitchToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 900);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                var latest = await _employeeService.GetAsync(selected.Id, uiToken) ?? selected;

                await RunOnUiThreadAsync(() =>
                {
                    _openedProfileEmployeeId = latest.Id;
                    ProfileVm.SetProfile(latest);
                    ListVm.SelectedItem = latest;
                    CancelTarget = EmployeeSection.List;
                });

                await SwitchToProfileAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 900);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal Task CancelAsync()
        {
            // 1) При Cancel очищаємо помилки форми (щоб не “прилипали”).
            EditVm.ClearValidationErrors();

            // 2) Навігація залежить від Mode і CancelTarget.
            return Mode switch
            {
                EmployeeSection.Edit => CancelTarget == EmployeeSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),

                _ => SwitchToListAsync()
            };
        }

        // =========================================================
        // Load + navigation helpers
        // =========================================================

        private async Task LoadEmployeesAsync(CancellationToken ct, int? selectId)
        {
            var list = (await _employeeService.GetAllAsync(ct)).ToList();

            await RunOnUiThreadAsync(() =>
            {
                ListVm.SetItems(list);

                if (selectId.HasValue)
                    ListVm.SelectedItem = list.FirstOrDefault(e => e.Id == selectId.Value);
            });
        }

        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = EmployeeSection.List;
            return Task.CompletedTask;
        }

        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = EmployeeSection.Edit;
            return Task.CompletedTask;
        }

        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = EmployeeSection.Profile;
            return Task.CompletedTask;
        }

        private int GetCurrentEmployeeId()
        {
            if (Mode == EmployeeSection.Profile)
                return ProfileVm.EmployeeId;

            return ListVm.SelectedItem?.Id ?? 0;
        }

        private string GetCurrentEmployeeName()
        {
            // У профілі вже є “готовий” FullName.
            if (Mode == EmployeeSection.Profile)
                return ProfileVm.FullName;

            // У списку — беремо з SelectedItem.
            return EmployeeDisplayHelper.GetFullName(ListVm.SelectedItem);
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
                if (Mode == EmployeeSection.Edit)
                {
                    _logger.Log($"[DB-CHANGE] Employee reload skipped in edit mode. Source={source}.");
                    return;
                }

                var selectedId = Mode == EmployeeSection.Profile ? ProfileVm.EmployeeId : ListVm.SelectedItem?.Id;
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadEmployeesAsync(CancellationToken.None, selectedId);

                    if (Mode == EmployeeSection.Profile && selectedId.HasValue)
                    {
                        var latest = await _employeeService.GetAsync(selectedId.Value, CancellationToken.None);
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

                _logger.Log($"[DB-CHANGE] Employee module reloaded. Source={source}.");
            }
            catch (Exception ex)
            {
                _logger.Log($"[DB-CHANGE] Employee reload failed: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _databaseReloadInProgress, 0);
            }
        }

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
        // UI messaging (залишив твою схему)
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
