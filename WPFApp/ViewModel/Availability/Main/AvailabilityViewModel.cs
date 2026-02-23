using BusinessLogicLayer.Services.Abstractions;
using WPFApp.Infrastructure;
using WPFApp.ViewModel.Availability.Edit;
using WPFApp.ViewModel.Availability.List;
using WPFApp.ViewModel.Availability.Profile;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// AvailabilitySection — “режим екрану” (яку секцію показує UI).
    /// </summary>
    public enum AvailabilitySection
    {
        List,
        Edit,
        Profile
    }

    /// <summary>
    /// AvailabilityViewModel — “owner/coordinator” модуля Availability.
    ///
    /// Відповідальність:
    /// - тримати 3 під-VM: List/Edit/Profile
    /// - керувати навігацією (CurrentSection + Mode)
    /// - виконувати CRUD/завантаження через сервіси
    /// - бути “gateway” для EditVm (save/cancel/binds/filter)
    ///
    /// Архітектурно це відповідає патерну, який у тебе вже є в ContainerViewModel.* partials.
    /// </summary>
    public sealed partial class AvailabilityViewModel : ViewModelBase
    {
        // 1) Сервіси бізнес-логіки.
        private readonly IAvailabilityGroupService _availabilityService;
        private readonly IEmployeeService _employeeService;
        private readonly IBindService _bindService;
        private readonly Service.IDatabaseChangeNotifier _databaseChangeNotifier;
        private readonly Service.ILoggerService _logger;

        // 2) Під-VM (UI секції).
        public AvailabilityListViewModel ListVm { get; }
        public AvailabilityEditViewModel EditVm { get; }
        public AvailabilityProfileViewModel ProfileVm { get; }

        // 3) Команди навігації (можуть використовуватися кнопками/меню).
        public AsyncRelayCommand ShowListCommand { get; }
        public AsyncRelayCommand ShowEditCommand { get; }
        public AsyncRelayCommand ShowProfileCommand { get; }

        public AvailabilityViewModel(
            IAvailabilityGroupService availabilityService,
            IEmployeeService employeeService,
            IBindService bindService,
            Service.IDatabaseChangeNotifier databaseChangeNotifier,
            Service.ILoggerService logger)
        {
            // 1) Інʼєкції.
            _availabilityService = availabilityService;
            _employeeService = employeeService;
            _bindService = bindService;
            _databaseChangeNotifier = databaseChangeNotifier;
            _logger = logger;

            // 2) Створюємо під-VM і передаємо owner (this).
            ListVm = new AvailabilityListViewModel(this);
            EditVm = new AvailabilityEditViewModel(this);
            ProfileVm = new AvailabilityProfileViewModel(this);

            // 3) Навігаційні команди.
            ShowListCommand = new AsyncRelayCommand(() => SwitchToListAsync());
            ShowEditCommand = new AsyncRelayCommand(() => SwitchToEditAsync());
            ShowProfileCommand = new AsyncRelayCommand(() => SwitchToProfileAsync());

            _databaseChangeNotifier.DatabaseChanged += OnDatabaseChanged;

            // 4) Початково показуємо список.
            CurrentSection = ListVm;
        }
    }
}
