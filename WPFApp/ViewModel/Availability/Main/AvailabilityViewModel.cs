/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Services.Abstractions;
using WPFApp.Applications.Notifications;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Availability.Edit;
using WPFApp.ViewModel.Availability.List;
using WPFApp.ViewModel.Availability.Profile;

namespace WPFApp.ViewModel.Availability.Main
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public enum AvailabilitySection` та контракт його використання у шарі WPFApp.
    /// </summary>
    public enum AvailabilitySection
    {
        List,
        Edit,
        Profile
    }

    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityViewModel : ViewModelBase
    {
        
        private readonly IAvailabilityGroupService _availabilityService;
        private readonly IEmployeeService _employeeService;
        private readonly IBindService _bindService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
        
        
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityListViewModel ListVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityListViewModel ListVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityEditViewModel EditVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityEditViewModel EditVm { get; }
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityProfileViewModel ProfileVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityProfileViewModel ProfileVm { get; }

        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowListCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowListCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowEditCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowEditCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowProfileCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowProfileCommand { get; }

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityViewModel(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityViewModel(
            IAvailabilityGroupService availabilityService,
            IEmployeeService employeeService,
            IBindService bindService,
            IDatabaseChangeNotifier databaseChangeNotifier
            )
        {
            
            _availabilityService = availabilityService;
            _employeeService = employeeService;
            _bindService = bindService;
            _databaseChangeNotifier = databaseChangeNotifier;

            
            ListVm = new AvailabilityListViewModel(this);
            EditVm = new AvailabilityEditViewModel(this);
            ProfileVm = new AvailabilityProfileViewModel(this);

            
            ShowListCommand = new AsyncRelayCommand(() => SwitchToListAsync());
            ShowEditCommand = new AsyncRelayCommand(() => SwitchToEditAsync());
            ShowProfileCommand = new AsyncRelayCommand(() => SwitchToProfileAsync());

            _databaseChangeNotifier.DatabaseChanged += OnDatabaseChanged;

            
            CurrentSection = ListVm;
        }
    }
}
