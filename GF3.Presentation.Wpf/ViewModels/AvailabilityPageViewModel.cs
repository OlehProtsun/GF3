using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;

namespace GF3.Presentation.Wpf.ViewModels
{
    public class AvailabilityPageViewModel : ValidatableViewModelBase
    {
        private readonly IAvailabilityGroupService _groupService;
        private readonly IEmployeeService _employeeService;
        private readonly IBindService _bindService;
        private AvailabilityGroupModel? _selectedGroup;
        private EntityViewMode _mode;

        public AvailabilityPageViewModel(
            IAvailabilityGroupService groupService,
            IEmployeeService employeeService,
            IBindService bindService)
        {
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _bindService = bindService ?? throw new ArgumentNullException(nameof(bindService));

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AddCommand = new RelayCommand(BeginAdd);
            EditCommand = new RelayCommand(BeginEdit, () => SelectedGroup is not null);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedGroup is not null);
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => Mode == EntityViewMode.Edit);
            CancelCommand = new RelayCommand(CancelEdit);

            _ = LoadAsync();
        }

        public ObservableCollection<AvailabilityGroupModel> Groups { get; } = new();
        public ObservableCollection<EmployeeModel> Employees { get; } = new();
        public ObservableCollection<BindModel> Binds { get; } = new();

        public AvailabilityGroupModel? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                RaisePropertyChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public EntityViewMode Mode
        {
            get => _mode;
            private set
            {
                _mode = value;
                RaisePropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public AsyncRelayCommand LoadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private async Task LoadAsync()
        {
            var groups = await _groupService.GetAllAsync();
            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }

            var employees = await _employeeService.GetAllAsync();
            Employees.Clear();
            foreach (var employee in employees)
            {
                Employees.Add(employee);
            }

            var binds = await _bindService.GetAllAsync();
            Binds.Clear();
            foreach (var bind in binds)
            {
                Binds.Add(bind);
            }
        }

        private void BeginAdd()
        {
            SelectedGroup = new AvailabilityGroupModel();
            Mode = EntityViewMode.Edit;
        }

        private void BeginEdit()
        {
            if (SelectedGroup is null)
            {
                return;
            }

            Mode = EntityViewMode.Edit;
        }

        private async Task DeleteAsync()
        {
            if (SelectedGroup is null)
            {
                return;
            }

            await _groupService.DeleteAsync(SelectedGroup.Id);
            await LoadAsync();
        }

        private async Task SaveAsync()
        {
            if (SelectedGroup is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedGroup.Name))
            {
                SetErrors(nameof(SelectedGroup), "Group name is required.");
                return;
            }

            ClearErrors(nameof(SelectedGroup));

            if (SelectedGroup.Id == 0)
            {
                await _groupService.AddAsync(SelectedGroup);
            }
            else
            {
                await _groupService.UpdateAsync(SelectedGroup);
            }

            Mode = EntityViewMode.List;
            await LoadAsync();
        }

        private void CancelEdit()
        {
            Mode = EntityViewMode.List;
            ClearErrors(nameof(SelectedGroup));
        }
    }
}
