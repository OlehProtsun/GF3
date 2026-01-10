using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;

namespace GF3.Presentation.Wpf.ViewModels
{
    public class EmployeePageViewModel : ValidatableViewModelBase
    {
        private readonly IEmployeeService _employeeService;
        private EmployeeModel? _selectedEmployee;
        private string _searchText = string.Empty;
        private EntityViewMode _mode;

        public EmployeePageViewModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            AddCommand = new RelayCommand(BeginAdd);
            EditCommand = new RelayCommand(BeginEdit, () => SelectedEmployee is not null);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedEmployee is not null);
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => Mode == EntityViewMode.Edit);
            CancelCommand = new RelayCommand(CancelEdit);

            _ = LoadAsync();
        }

        public ObservableCollection<EmployeeModel> Employees { get; } = new();

        public EmployeeModel? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                RaisePropertyChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                RaisePropertyChanged();
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
        public AsyncRelayCommand SearchCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private async Task LoadAsync()
        {
            var employees = await _employeeService.GetAllAsync();
            Employees.Clear();
            foreach (var employee in employees)
            {
                Employees.Add(employee);
            }
        }

        private async Task SearchAsync()
        {
            var employees = await _employeeService.GetAllAsync();
            var filtered = employees
                .Where(x => string.IsNullOrWhiteSpace(SearchText)
                    || x.FirstName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || x.LastName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Employees.Clear();
            foreach (var employee in filtered)
            {
                Employees.Add(employee);
            }
        }

        private void BeginAdd()
        {
            SelectedEmployee = new EmployeeModel();
            Mode = EntityViewMode.Edit;
        }

        private void BeginEdit()
        {
            if (SelectedEmployee is null)
            {
                return;
            }

            Mode = EntityViewMode.Edit;
        }

        private async Task DeleteAsync()
        {
            if (SelectedEmployee is null)
            {
                return;
            }

            await _employeeService.DeleteAsync(SelectedEmployee.Id);
            await LoadAsync();
        }

        private async Task SaveAsync()
        {
            if (SelectedEmployee is null)
            {
                return;
            }

            if (!ValidateEmployee(SelectedEmployee))
            {
                return;
            }

            if (SelectedEmployee.Id == 0)
            {
                await _employeeService.AddAsync(SelectedEmployee);
            }
            else
            {
                await _employeeService.UpdateAsync(SelectedEmployee);
            }

            Mode = EntityViewMode.List;
            await LoadAsync();
        }

        private void CancelEdit()
        {
            Mode = EntityViewMode.List;
            ClearErrors(nameof(SelectedEmployee));
        }

        private bool ValidateEmployee(EmployeeModel model)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                errors.Add("First name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                errors.Add("Last name is required.");
            }

            if (errors.Count > 0)
            {
                SetErrors(nameof(SelectedEmployee), errors.ToArray());
                return false;
            }

            ClearErrors(nameof(SelectedEmployee));
            return true;
        }
    }
}
