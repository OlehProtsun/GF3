/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WPFApp.Applications.Diagnostics;
using WPFApp.Applications.Notifications;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation.Rules;
using WPFApp.UI.Dialogs;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Dialogs;
using WPFApp.ViewModel.Employee.Helpers;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Employee
{
    /// <summary>
    /// Визначає публічний елемент `public enum EmployeeSection` та контракт його використання у шарі WPFApp.
    /// </summary>
    public enum EmployeeSection
    {
        List,
        Edit,
        Profile
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class EmployeeViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class EmployeeViewModel : ViewModelBase
    {
        private readonly IEmployeeFacade _employeeService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
                private int _databaseReloadInProgress;

        
        
        

        private bool _initialized;
        private Task? _initializeTask;
        private readonly object _initLock = new();

        
        private int? _openedProfileEmployeeId;

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

        private EmployeeSection _mode = EmployeeSection.List;
        /// <summary>
        /// Визначає публічний елемент `public EmployeeSection Mode` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public EmployeeSection CancelTarget { get; private set; } = EmployeeSection.List;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeSection CancelTarget { get; private set; } = EmployeeSection.List;

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public EmployeeListViewModel ListVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeListViewModel ListVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public EmployeeEditViewModel EditVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeEditViewModel EditVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public EmployeeProfileViewModel ProfileVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeProfileViewModel ProfileVm { get; }

        /// <summary>
        /// Визначає публічний елемент `public EmployeeViewModel(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeViewModel(
            IEmployeeFacade employeeService,
            IDatabaseChangeNotifier databaseChangeNotifier
            )
        {
            _employeeService = employeeService;
            _databaseChangeNotifier = databaseChangeNotifier;

            ListVm = new EmployeeListViewModel(this);
            EditVm = new EmployeeEditViewModel(this);
            ProfileVm = new EmployeeProfileViewModel(this);

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
                
                await LoadEmployeesAsync(ct, selectId: null);

                
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
                ? await _employeeService.GetAllAsync(ct)
                : await _employeeService.GetByValueAsync(term, ct);

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
                        CancelTarget = EmployeeSection.List;
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
                    var latest = await _employeeService.GetAsync(selected.Id, uiToken) ?? selected;

                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.SetEmployee(latest);

                        CancelTarget = Mode == EmployeeSection.Profile
                            ? EmployeeSection.Profile
                            : EmployeeSection.List;
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
            var request = EditVm.ToRequest();
            var raw = EmployeeValidationRules.ValidateAll(request);

            if (raw.Count > 0)
            {
                var errors = ValidationDictionaryHelper.RemapFirstErrors(raw, EmployeeEditViewModel.MapValidationKeyToVm);
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
                    var savedEmployeeId = request.Id;
                    EmployeeDto? createdEmployee = null;

                    if (EditVm.IsEdit)
                    {
                        await _employeeService.UpdateAsync(request, uiToken);
                    }
                    else
                    {
                        createdEmployee = await _employeeService.CreateAsync(request, uiToken);
                        savedEmployeeId = createdEmployee.Id;

                        await RunOnUiThreadAsync(() => EditVm.EmployeeId = savedEmployeeId);
                    }

                    _databaseChangeNotifier.NotifyDatabaseChanged("Employee.Save");

                    await LoadEmployeesAsync(uiToken, selectId: savedEmployeeId);

                    if (CancelTarget == EmployeeSection.Profile)
                    {
                        var profileId = _openedProfileEmployeeId ?? savedEmployeeId;

                        if (profileId > 0)
                        {
                            var latest = await _employeeService.GetAsync(profileId, uiToken) ?? createdEmployee;

                            if (latest is not null)
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
            
            var currentId = GetCurrentEmployeeId();
            if (currentId <= 0)
                return;

            
            var currentName = GetCurrentEmployeeName();

            
            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                ? "Delete employee?"
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
                    await _employeeService.DeleteAsync(currentId, uiToken);

                    _databaseChangeNotifier.NotifyDatabaseChanged("Employee.Delete");
                    await LoadEmployeesAsync(uiToken, selectId: null);
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
                    var latest = await _employeeService.GetAsync(selected.Id, uiToken) ?? selected;

                    await RunOnUiThreadAsync(() =>
                    {
                        _openedProfileEmployeeId = latest.Id;
                        ProfileVm.SetProfile(latest);
                        ListVm.SelectedItem = latest;
                        CancelTarget = EmployeeSection.List;
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
                EmployeeSection.Edit => CancelTarget == EmployeeSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),

                _ => SwitchToListAsync()
            };
        }

        
        
        

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
            
            if (Mode == EmployeeSection.Profile)
                return ProfileVm.FullName;

            
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
