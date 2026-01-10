using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WPFApp.View.Availability.DesignTime
{
    // ========== Design-time models ==========
    public sealed class EmployeeItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public override string ToString() => FullName; // щоб ComboBox красиво показував ім'я
    }

    public sealed class BindItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class AvailabilityDayItem
    {
        public string Day { get; set; } = "";
        public string Start { get; set; } = "";
        public string End { get; set; } = "";
        public bool IsAvailable { get; set; }
    }

    // ========== Design-time VM ==========
    public sealed class AvailabilityEditViewDesignVM : INotifyPropertyChanged
    {
        // --- Information ---
        private int _availabilityId;
        public int AvailabilityId
        {
            get => _availabilityId;
            set { _availabilityId = value; OnPropertyChanged(); }
        }

        private int _availabilityMonth;
        public int AvailabilityMonth
        {
            get => _availabilityMonth;
            set { _availabilityMonth = value; OnPropertyChanged(); }
        }

        private int _availabilityYear;
        public int AvailabilityYear
        {
            get => _availabilityYear;
            set { _availabilityYear = value; OnPropertyChanged(); }
        }

        private string _availabilityName = "";
        public string AvailabilityName
        {
            get => _availabilityName;
            set { _availabilityName = value; OnPropertyChanged(); }
        }

        // --- Employee ---
        private string _employeeSearchText = "";
        public string EmployeeSearchText
        {
            get => _employeeSearchText;
            set { _employeeSearchText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EmployeeItem> Employees { get; } = new();

        private EmployeeItem? _selectedEmployee;
        public EmployeeItem? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedEmployeeId));
            }
        }

        public int SelectedEmployeeId => SelectedEmployee?.Id ?? 0;

        // --- Binds ---
        public ObservableCollection<BindItem> Binds { get; } = new();

        private BindItem? _selectedBind;
        public BindItem? SelectedBind
        {
            get => _selectedBind;
            set { _selectedBind = value; OnPropertyChanged(); }
        }

        // --- Availability table ---
        public ObservableCollection<AvailabilityDayItem> AvailabilityDays { get; } = new();

        private AvailabilityDayItem? _selectedAvailabilityDay;
        public AvailabilityDayItem? SelectedAvailabilityDay
        {
            get => _selectedAvailabilityDay;
            set { _selectedAvailabilityDay = value; OnPropertyChanged(); }
        }

        // --- Commands (design-time stubs) ---
        public ICommand CancelInformationCommand { get; } = new DesignCommand();
        public ICommand CancelEmployeeCommand { get; } = new DesignCommand();
        public ICommand RemoveEmployeeCommand { get; } = new DesignCommand();
        public ICommand AddEmployeeCommand { get; } = new DesignCommand();

        public ICommand CancelBindCommand { get; } = new DesignCommand();
        public ICommand DeleteBindCommand { get; } = new DesignCommand();
        public ICommand AddBindCommand { get; } = new DesignCommand();

        public ICommand CancelCommand { get; } = new DesignCommand();
        public ICommand SaveCommand { get; } = new DesignCommand();

        public AvailabilityEditViewDesignVM()
        {
            // Sample values for designer
            AvailabilityId = 12;
            AvailabilityMonth = 1;
            AvailabilityYear = 2026;
            AvailabilityName = "January Availability - Main Team";

            EmployeeSearchText = "an";

            Employees.Add(new EmployeeItem { Id = 101, FullName = "Anna Kowalska" });
            Employees.Add(new EmployeeItem { Id = 102, FullName = "Andrii Shevchenko" });
            Employees.Add(new EmployeeItem { Id = 103, FullName = "Oleh Petrenko" });
            SelectedEmployee = Employees[1];

            Binds.Add(new BindItem { Id = 1, Type = "Project", Value = "Retail App" });
            Binds.Add(new BindItem { Id = 2, Type = "Location", Value = "Warsaw" });
            Binds.Add(new BindItem { Id = 3, Type = "Role", Value = "Support" });
            SelectedBind = Binds[0];

            AvailabilityDays.Add(new AvailabilityDayItem { Day = "Mon", Start = "09:00", End = "17:00", IsAvailable = true });
            AvailabilityDays.Add(new AvailabilityDayItem { Day = "Tue", Start = "10:00", End = "18:00", IsAvailable = true });
            AvailabilityDays.Add(new AvailabilityDayItem { Day = "Wed", Start = "—", End = "—", IsAvailable = false });
            AvailabilityDays.Add(new AvailabilityDayItem { Day = "Thu", Start = "12:00", End = "20:00", IsAvailable = true });
            AvailabilityDays.Add(new AvailabilityDayItem { Day = "Fri", Start = "09:00", End = "15:00", IsAvailable = true });
            AvailabilityDays.Add(new AvailabilityDayItem { Day = "Sat", Start = "—", End = "—", IsAvailable = false });
            AvailabilityDays.Add(new AvailabilityDayItem { Day = "Sun", Start = "—", End = "—", IsAvailable = false });
            SelectedAvailabilityDay = AvailabilityDays[0];
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== Simple design-time ICommand =====
        private sealed class DesignCommand : ICommand
        {
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) { }
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
