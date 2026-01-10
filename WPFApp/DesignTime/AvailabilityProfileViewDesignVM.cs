using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WPFApp.View.Availability.DesignTime
{
    public sealed class AvailabilityProfileMonthItem
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string Name { get; set; } = "";
        public int DaysCount { get; set; }
        public int AvailableDays { get; set; }

        // щоб в AutoGenerateColumns були приємні значення
        public string MonthYear => $"{Month:D2}/{Year}";
    }

    public sealed class AvailabilityProfileViewDesignVM : INotifyPropertyChanged
    {
        // --- Profile fields ---
        private int _availabilityId;
        public int AvailabilityId
        {
            get => _availabilityId;
            set { _availabilityId = value; OnPropertyChanged(); }
        }

        private string _availabilityName = "";
        public string AvailabilityName
        {
            get => _availabilityName;
            set { _availabilityName = value; OnPropertyChanged(); }
        }

        private string _availabilityMonthYear = "";
        public string AvailabilityMonthYear
        {
            get => _availabilityMonthYear;
            set { _availabilityMonthYear = value; OnPropertyChanged(); }
        }

        // --- Table ---
        public ObservableCollection<AvailabilityProfileMonthItem> ProfileAvailabilityMonths { get; } = new();

        private AvailabilityProfileMonthItem? _selectedProfileMonth;
        public AvailabilityProfileMonthItem? SelectedProfileMonth
        {
            get => _selectedProfileMonth;
            set { _selectedProfileMonth = value; OnPropertyChanged(); }
        }

        // --- Commands (design-time stubs) ---
        public ICommand BackCommand { get; } = new DesignCommand();
        public ICommand AddNewCommand { get; } = new DesignCommand();

        public ICommand CancelProfileCommand { get; } = new DesignCommand();
        public ICommand DeleteCommand { get; } = new DesignCommand();

        public ICommand CancelTableCommand { get; } = new DesignCommand();
        public ICommand EditCommand { get; } = new DesignCommand();

        public AvailabilityProfileViewDesignVM()
        {
            // sample data for designer
            AvailabilityId = 12;
            AvailabilityName = "January Availability - Main Team";
            AvailabilityMonthYear = "01-2026";

            ProfileAvailabilityMonths.Add(new AvailabilityProfileMonthItem
            {
                Month = 1,
                Year = 2026,
                Name = "January Availability - Main Team",
                DaysCount = 31,
                AvailableDays = 22
            });

            ProfileAvailabilityMonths.Add(new AvailabilityProfileMonthItem
            {
                Month = 12,
                Year = 2025,
                Name = "December Availability - Main Team",
                DaysCount = 31,
                AvailableDays = 19
            });

            ProfileAvailabilityMonths.Add(new AvailabilityProfileMonthItem
            {
                Month = 11,
                Year = 2025,
                Name = "November Availability - Main Team",
                DaysCount = 30,
                AvailableDays = 20
            });

            SelectedProfileMonth = ProfileAvailabilityMonths[0];
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
