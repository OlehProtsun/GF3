using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WPFApp.View.Availability.DesignTime
{
    public sealed class AvailabilityProfileViewDesignVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int AvailabilityId { get; set; } = 101;
        public string AvailabilityName { get; set; } = "Default Availability";
        public string AvailabilityMonthYear { get; set; } = "January 2026";

        public ObservableCollection<AvailabilityProfileRow> ProfileAvailabilityMonths { get; set; }

        private AvailabilityProfileRow? _selectedProfileMonth;
        public AvailabilityProfileRow? SelectedProfileMonth
        {
            get => _selectedProfileMonth;
            set { _selectedProfileMonth = value; OnPropertyChanged(); }
        }

        // Команди (щоб дизайнер не лаявся на bindings)
        public ICommand BackCommand { get; } = new DesignCommand();
        public ICommand CancelProfileCommand { get; } = new DesignCommand();
        public ICommand DeleteCommand { get; } = new DesignCommand();
        public ICommand CancelTableCommand { get; } = new DesignCommand();
        public ICommand EditCommand { get; } = new DesignCommand();

        public AvailabilityProfileViewDesignVM()
        {
            ProfileAvailabilityMonths = new ObservableCollection<AvailabilityProfileRow>
            {
                new AvailabilityProfileRow { Month = "January",  Week1="✔", Week2="✔", Week3="—", Week4="✔", Notes="Good" },
                new AvailabilityProfileRow { Month = "February", Week1="—", Week2="✔", Week3="✔", Week4="—", Notes="Busy" },
                new AvailabilityProfileRow { Month = "March",    Week1="✔", Week2="—", Week3="—", Week4="✔", Notes="OK" },
                new AvailabilityProfileRow { Month = "April",    Week1="✔", Week2="✔", Week3="✔", Week4="✔", Notes="Free" },
            };

            SelectedProfileMonth = ProfileAvailabilityMonths[0];
        }

        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public sealed class AvailabilityProfileRow
    {
        public string Month { get; set; } = "";
        public string Week1 { get; set; } = "";
        public string Week2 { get; set; } = "";
        public string Week3 { get; set; } = "";
        public string Week4 { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    internal sealed class DesignCommand : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
        public event EventHandler? CanExecuteChanged;
    }
}
