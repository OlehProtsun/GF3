using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WPFApp.View.Availability.DesignTime
{
    public sealed class AvailabilityRow
    {
        public string Name { get; set; } = "";
    }

    public sealed class AvailabilityListViewDesignVM
    {
        public string SearchText { get; set; } = "mo";

        public ObservableCollection<AvailabilityRow> Items { get; } = new()
        {
            new AvailabilityRow { Name = "Morning (09:00–12:00)" },
            new AvailabilityRow { Name = "Afternoon (12:00–17:00)" },
            new AvailabilityRow { Name = "Evening (17:00–21:00)" },
        };

        public AvailabilityRow? SelectedItem { get; set; }

        public ICommand SearchCommand { get; } = new DesignCommand();
        public ICommand AddNewCommand { get; } = new DesignCommand();

        private sealed class DesignCommand : ICommand
        {
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) { }
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
