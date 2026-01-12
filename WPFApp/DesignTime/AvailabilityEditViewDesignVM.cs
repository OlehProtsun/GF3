using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPFApp.ViewModel.Availability; // EmployeeListItem, BindRow

namespace WPFApp.View.Availability.DesignTime
{
    public sealed class AvailabilityEditViewDesignVM : INotifyPropertyChanged
    {
        private const string DayColumn = "DayOfMonth";

        private readonly DataTable _groupTable = new();

        public DataView AvailabilityDays => _groupTable.DefaultView;

        public ObservableCollection<EmployeeListItem> Employees { get; } = new();
        public ObservableCollection<BindRow> Binds { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CancelInformationCommand { get; }
        public ICommand CancelEmployeeCommand { get; }
        public ICommand CancelBindCommand { get; }
        public ICommand AddEmployeeCommand { get; }
        public ICommand RemoveEmployeeCommand { get; }
        public ICommand SearchEmployeeCommand { get; }
        public ICommand AddBindCommand { get; }
        public ICommand DeleteBindCommand { get; }

        private int _availabilityId;
        public int AvailabilityId { get => _availabilityId; set => Set(ref _availabilityId, value); }

        private int _availabilityMonth;
        public int AvailabilityMonth { get => _availabilityMonth; set => Set(ref _availabilityMonth, value); }

        private int _availabilityYear;
        public int AvailabilityYear { get => _availabilityYear; set => Set(ref _availabilityYear, value); }

        private string _availabilityName = "";
        public string AvailabilityName { get => _availabilityName; set => Set(ref _availabilityName, value); }

        private string _employeeSearchText = "";
        public string EmployeeSearchText { get => _employeeSearchText; set => Set(ref _employeeSearchText, value); }

        private EmployeeListItem? _selectedEmployee;
        public EmployeeListItem? SelectedEmployee { get => _selectedEmployee; set => Set(ref _selectedEmployee, value); }

        public string SelectedEmployeeId => SelectedEmployee?.Id > 0 ? SelectedEmployee.Id.ToString() : string.Empty;

        private BindRow? _selectedBind;
        public BindRow? SelectedBind { get => _selectedBind; set => Set(ref _selectedBind, value); }

        private object? _selectedAvailabilityDay;
        public object? SelectedAvailabilityDay { get => _selectedAvailabilityDay; set => Set(ref _selectedAvailabilityDay, value); }

        // Якщо у тебе у View є підписка на MatrixChanged (у code-behind) — залишимо івент
        public event EventHandler? MatrixChanged;

        public AvailabilityEditViewDesignVM()
        {
            // dummy commands (в дизайнері вони не потрібні)
            var cmd = new DummyCommand();
            SaveCommand = cmd;
            CancelCommand = cmd;
            CancelInformationCommand = cmd;
            CancelEmployeeCommand = cmd;
            CancelBindCommand = cmd;
            AddEmployeeCommand = cmd;
            RemoveEmployeeCommand = cmd;
            SearchEmployeeCommand = cmd;
            AddBindCommand = cmd;
            DeleteBindCommand = cmd;

            // demo header fields
            AvailabilityId = 12;
            AvailabilityMonth = DateTime.Today.Month;
            AvailabilityYear = DateTime.Today.Year;
            AvailabilityName = "Availability (Design Preview)";

            // demo employees (ліва карта)
            Employees.Add(new EmployeeListItem { Id = 1, FullName = "Іван Петренко" });
            Employees.Add(new EmployeeListItem { Id = 2, FullName = "Олена Шевченко" });
            Employees.Add(new EmployeeListItem { Id = 3, FullName = "Андрій Коваль" });
            SelectedEmployee = Employees[0];

            // demo binds
            Binds.Add(new BindRow { Id = 1, Key = "CTRL+1", Value = "+", IsActive = true });
            Binds.Add(new BindRow { Id = 2, Key = "CTRL+2", Value = "-", IsActive = true });
            Binds.Add(new BindRow { Id = 3, Key = "CTRL+3", Value = "09:00-17:00", IsActive = false });
            SelectedBind = Binds[0];

            BuildScheduleTable();
        }

        private void BuildScheduleTable()
        {
            _groupTable.Clear();
            _groupTable.Columns.Clear();

            // Day
            _groupTable.Columns.Add(new DataColumn(DayColumn, typeof(int))
            {
                Caption = "Day",
                ReadOnly = true
            });

            // Employee columns (як у твоєму runtime: emp_{id})
            var c1 = new DataColumn("emp_1", typeof(string)) { Caption = "Іван Петренко" };
            var c2 = new DataColumn("emp_2", typeof(string)) { Caption = "Олена Шевченко" };
            _groupTable.Columns.Add(c1);
            _groupTable.Columns.Add(c2);

            int days = 31;
            for (int d = 1; d <= days; d++)
            {
                var row = _groupTable.NewRow();
                row[DayColumn] = d;

                // трохи “живих” прикладів
                row["emp_1"] = (d % 2 == 0) ? "+" : "-";
                row["emp_2"] = (d % 3 == 0) ? "09:00-17:00" : "-";

                _groupTable.Rows.Add(row);
            }

            MatrixChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(AvailabilityDays));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
            if (name == nameof(SelectedEmployee)) OnPropertyChanged(nameof(SelectedEmployeeId));
        }

        private sealed class DummyCommand : ICommand
        {
            public bool CanExecute(object? parameter) => false;
            public void Execute(object? parameter) { }
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
