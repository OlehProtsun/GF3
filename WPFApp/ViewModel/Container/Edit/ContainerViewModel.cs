/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Applications.Export;
using WPFApp.Applications.Notifications;
using WPFApp.MVVM.Core;
using WPFApp.UI.Dialogs;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Container.List;
using WPFApp.ViewModel.Container.Profile;
using WPFApp.ViewModel.Container.ScheduleEdit;
using WPFApp.ViewModel.Container.ScheduleProfile;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel : ViewModelBase
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private readonly IContainerService _containerService;
        private readonly IScheduleService _scheduleService;
        private readonly IAvailabilityGroupService _availabilityGroupService;
        private readonly IShopService _shopService;
        private readonly IEmployeeService _employeeService;
        private readonly IScheduleGenerator _generator;
        private readonly IColorPickerService _colorPickerService;
        private readonly IScheduleExportService _scheduleExportService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
                private readonly ConcurrentDictionary<int, Lazy<Task<ScheduleModel?>>> _scheduleDetailsCache = new();

        
        
        

        
        
        
        
        
        private bool _initialized;

        
        
        
        
        
        private int? _openedProfileContainerId;

        
        
        
        
        private const int MaxOpenedSchedules = 20;

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

        private Task WaitForUiIdleAsync()
            => System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle).Task;

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

        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerListViewModel ListVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerListViewModel ListVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public ContainerEditViewModel EditVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerEditViewModel EditVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public ContainerProfileViewModel ProfileVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerProfileViewModel ProfileVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleEditViewModel ScheduleEditVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleEditViewModel ScheduleEditVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleProfileViewModel ScheduleProfileVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleProfileViewModel ScheduleProfileVm { get; }

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerViewModel(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerViewModel(
            IContainerService containerService,
            IScheduleService scheduleService,
            IAvailabilityGroupService availabilityGroupService,
            IShopService shopService,
            IEmployeeService employeeService,
            IScheduleGenerator generator,
            IColorPickerService colorPickerService,
            IScheduleExportService scheduleExportService,
            IDatabaseChangeNotifier databaseChangeNotifier
            )
        {
            
            
            
            ArgumentNullException.ThrowIfNull(containerService);
            ArgumentNullException.ThrowIfNull(scheduleService);
            ArgumentNullException.ThrowIfNull(availabilityGroupService);
            ArgumentNullException.ThrowIfNull(shopService);
            ArgumentNullException.ThrowIfNull(employeeService);
            ArgumentNullException.ThrowIfNull(generator);
            ArgumentNullException.ThrowIfNull(colorPickerService);
            ArgumentNullException.ThrowIfNull(scheduleExportService);
            ArgumentNullException.ThrowIfNull(databaseChangeNotifier);

            _containerService = containerService;
            _scheduleService = scheduleService;
            _availabilityGroupService = availabilityGroupService;
            _shopService = shopService;
            _employeeService = employeeService;
            _generator = generator;
            _colorPickerService = colorPickerService;
            _scheduleExportService = scheduleExportService;
            _databaseChangeNotifier = databaseChangeNotifier;

            
            
            
            
            
            ListVm = new ContainerListViewModel(this);
            EditVm = new ContainerEditViewModel(this);
            ProfileVm = new ContainerProfileViewModel(this);
            ScheduleEditVm = new ContainerScheduleEditViewModel(
                this,
                _availabilityGroupService,
                _employeeService);
            ScheduleProfileVm = new ContainerScheduleProfileViewModel(this);

            ProfileVm.EmployeesLoader = LoadScheduleEmployeesAsync;
            ProfileVm.SlotsLoader = LoadScheduleSlotsAsync;

            _databaseChangeNotifier.DatabaseChanged += OnDatabaseChanged;

            
            
            CurrentSection = ListVm;
        }

        private async Task<IReadOnlyList<ScheduleEmployeeModel>> LoadScheduleEmployeesAsync(int scheduleId, CancellationToken ct)
        {
            var detailed = await GetScheduleDetailsCachedAsync(scheduleId, ct).ConfigureAwait(false);
            return detailed?.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
        }

        private async Task<IReadOnlyList<ScheduleSlotModel>> LoadScheduleSlotsAsync(int scheduleId, CancellationToken ct)
        {
            var detailed = await GetScheduleDetailsCachedAsync(scheduleId, ct).ConfigureAwait(false);
            return detailed?.Slots?.ToList() ?? new List<ScheduleSlotModel>();
        }

        private async Task<ScheduleModel?> GetScheduleDetailsCachedAsync(int scheduleId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (scheduleId <= 0)
                return null;

            var lazyTask = _scheduleDetailsCache.GetOrAdd(
                scheduleId,
                id => new Lazy<Task<ScheduleModel?>>(() => _scheduleService.GetDetailedAsync(id, CancellationToken.None)));

            var detailed = await lazyTask.Value.ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return detailed;
        }

        private void ClearScheduleDetailsCache()
            => _scheduleDetailsCache.Clear();
    }
}
