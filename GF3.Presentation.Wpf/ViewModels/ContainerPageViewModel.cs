using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;

namespace GF3.Presentation.Wpf.ViewModels
{
    public class ContainerPageViewModel : ValidatableViewModelBase
    {
        private readonly IContainerService _containerService;
        private readonly IScheduleService _scheduleService;
        private readonly IAvailabilityGroupService _availabilityGroupService;
        private readonly IShopService _shopService;
        private readonly IEmployeeService _employeeService;
        private readonly IScheduleGenerator _scheduleGenerator;

        private ContainerModel? _selectedContainer;
        private ScheduleModel? _selectedSchedule;
        private ContainerViewMode _mode;
        private ScheduleViewMode _scheduleMode;

        public ContainerPageViewModel(
            IContainerService containerService,
            IScheduleService scheduleService,
            IAvailabilityGroupService availabilityGroupService,
            IShopService shopService,
            IEmployeeService employeeService,
            IScheduleGenerator scheduleGenerator)
        {
            _containerService = containerService ?? throw new ArgumentNullException(nameof(containerService));
            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
            _availabilityGroupService = availabilityGroupService ?? throw new ArgumentNullException(nameof(availabilityGroupService));
            _shopService = shopService ?? throw new ArgumentNullException(nameof(shopService));
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _scheduleGenerator = scheduleGenerator ?? throw new ArgumentNullException(nameof(scheduleGenerator));

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AddCommand = new RelayCommand(BeginAdd);
            EditCommand = new RelayCommand(BeginEdit, () => SelectedContainer is not null);
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => Mode == ContainerViewMode.Edit);
            CancelCommand = new RelayCommand(CancelEdit);

            LoadScheduleCommand = new AsyncRelayCommand(LoadSchedulesAsync);

            _ = LoadAsync();
        }

        public ObservableCollection<ContainerModel> Containers { get; } = new();
        public ObservableCollection<ScheduleModel> Schedules { get; } = new();
        public ObservableCollection<AvailabilityGroupModel> AvailabilityGroups { get; } = new();
        public ObservableCollection<ShopModel> Shops { get; } = new();
        public ObservableCollection<EmployeeModel> Employees { get; } = new();
        public ObservableCollection<ScheduleSlotModel> ScheduleSlots { get; } = new();
        public ObservableCollection<ScheduleEmployeeModel> ScheduleEmployees { get; } = new();

        public ContainerModel? SelectedContainer
        {
            get => _selectedContainer;
            set
            {
                _selectedContainer = value;
                RaisePropertyChanged();
                EditCommand.RaiseCanExecuteChanged();
            }
        }

        public ScheduleModel? SelectedSchedule
        {
            get => _selectedSchedule;
            set
            {
                _selectedSchedule = value;
                RaisePropertyChanged();
            }
        }

        public ContainerViewMode Mode
        {
            get => _mode;
            private set
            {
                _mode = value;
                RaisePropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public ScheduleViewMode ScheduleMode
        {
            get => _scheduleMode;
            private set
            {
                _scheduleMode = value;
                RaisePropertyChanged();
            }
        }

        public AsyncRelayCommand LoadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public AsyncRelayCommand LoadScheduleCommand { get; }

        private async Task LoadAsync()
        {
            var containers = await _containerService.GetAllAsync();
            Containers.Clear();
            foreach (var container in containers)
            {
                Containers.Add(container);
            }

            await LoadSchedulesAsync();
            await LoadLookupsAsync();
        }

        private async Task LoadSchedulesAsync()
        {
            var schedules = await _scheduleService.GetAllAsync();
            Schedules.Clear();
            foreach (var schedule in schedules)
            {
                Schedules.Add(schedule);
            }
        }

        private async Task LoadLookupsAsync()
        {
            var groups = await _availabilityGroupService.GetAllAsync();
            AvailabilityGroups.Clear();
            foreach (var group in groups)
            {
                AvailabilityGroups.Add(group);
            }

            var shops = await _shopService.GetAllAsync();
            Shops.Clear();
            foreach (var shop in shops)
            {
                Shops.Add(shop);
            }

            var employees = await _employeeService.GetAllAsync();
            Employees.Clear();
            foreach (var employee in employees)
            {
                Employees.Add(employee);
            }
        }

        private void BeginAdd()
        {
            SelectedContainer = new ContainerModel();
            Mode = ContainerViewMode.Edit;
        }

        private void BeginEdit()
        {
            if (SelectedContainer is null)
            {
                return;
            }

            Mode = ContainerViewMode.Edit;
        }

        private async Task SaveAsync()
        {
            if (SelectedContainer is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedContainer.Name))
            {
                SetErrors(nameof(SelectedContainer), "Container name is required.");
                return;
            }

            ClearErrors(nameof(SelectedContainer));

            if (SelectedContainer.Id == 0)
            {
                await _containerService.AddAsync(SelectedContainer);
            }
            else
            {
                await _containerService.UpdateAsync(SelectedContainer);
            }

            Mode = ContainerViewMode.List;
            await LoadAsync();
        }

        private void CancelEdit()
        {
            Mode = ContainerViewMode.List;
            ClearErrors(nameof(SelectedContainer));
        }
    }
}
