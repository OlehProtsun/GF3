using System.Collections.ObjectModel;

namespace WPFApp.DesignTime
{
    public sealed class AvailabilityListDesignRow
    {
        public string Name { get; set; } = "";
    }

    public sealed class AvailabilityListDesignViewModel
    {
        public ObservableCollection<AvailabilityListDesignRow> Items { get; } =
            new ObservableCollection<AvailabilityListDesignRow>
            {
                new AvailabilityListDesignRow { Name = "Availability January 2026" },
                new AvailabilityListDesignRow { Name = "Availability February 2026" },
                new AvailabilityListDesignRow { Name = "Availability March 2026" },
            };

        public AvailabilityListDesignRow? SelectedItem { get; set; }

        public string SearchText { get; set; } = "jan";
    }
}
