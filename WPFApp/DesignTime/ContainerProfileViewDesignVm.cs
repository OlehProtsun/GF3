using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace WPFApp.DesignTime
{
    // Кореневий design-time VM для цього View
    public class ContainerProfileViewDesignVm
    {
        public string ContainerId { get; set; } = "CNT-2026-00042";
        public string Name { get; set; } = "Container A-12 (Frozen goods)";
        public string Note { get; set; } = "Design-time note: here you can see container details preview.";

        public ContainerScheduleListDesignVm ScheduleListVm { get; set; } = new();

        // Якщо у тебе на рівні root є BackCommand і т.п. — не треба, дизайнеру достатньо даних.
        // Команди можна не оголошувати (Binding просто буде null і це ок для дизайнера).
    }

    // Design-time VM для блоку schedules
    public class ContainerScheduleListDesignVm
    {
        public bool IsMultiOpenEnabled { get; set; } = true;
        public string SearchText { get; set; } = "Jan";

        public ObservableCollection<ScheduleItemDesignVm> Items { get; set; } = new()
        {
            new ScheduleItemDesignVm { IsChecked = true,  Name = "January shipment", Year = 2026, Month = 1, Shop = new ShopDesignVm { Name = "Biedronka" } },
            new ScheduleItemDesignVm { IsChecked = false, Name = "February shipment", Year = 2026, Month = 2, Shop = new ShopDesignVm { Name = "Lidl" } },
            new ScheduleItemDesignVm { IsChecked = true,  Name = "March shipment", Year = 2026, Month = 3, Shop = new ShopDesignVm { Name = "Auchan" } },
            new ScheduleItemDesignVm { IsChecked = false, Name = "April shipment", Year = 2026, Month = 4, Shop = new ShopDesignVm { Name = "Carrefour" } },
        };

        public ScheduleItemDesignVm SelectedItem { get; set; }
            = new ScheduleItemDesignVm { IsChecked = false, Name = "February shipment", Year = 2026, Month = 2, Shop = new ShopDesignVm { Name = "Lidl" } };
    }

    public class ScheduleItemDesignVm
    {
        public bool IsChecked { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public ShopDesignVm Shop { get; set; }
        // Якщо в реальних item є IsSelected / інші — можна додати, але для твого XAML цього достатньо.
    }

    public class ShopDesignVm
    {
        public string Name { get; set; }
    }
}
