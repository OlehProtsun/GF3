/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerProfileViewDesignVm у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace WPFApp.DesignTime
{
    
    /// <summary>
    /// Визначає публічний елемент `public class ContainerProfileViewDesignVm` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class ContainerProfileViewDesignVm
    {
        /// <summary>
        /// Визначає публічний елемент `public string ContainerId { get; set; } = "CNT-2026-00042";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ContainerId { get; set; } = "CNT-2026-00042";
        /// <summary>
        /// Визначає публічний елемент `public string Name { get; set; } = "Container A-12 (Frozen goods)";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name { get; set; } = "Container A-12 (Frozen goods)";
        /// <summary>
        /// Визначає публічний елемент `public string Note { get; set; } = "Design-time note: here you can see container details preview.";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Note { get; set; } = "Design-time note: here you can see container details preview.";

        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleListDesignVm ScheduleListVm { get; set; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleListDesignVm ScheduleListVm { get; set; } = new();

        
        
    }

    
    /// <summary>
    /// Визначає публічний елемент `public class ContainerScheduleListDesignVm` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class ContainerScheduleListDesignVm
    {
        /// <summary>
        /// Визначає публічний елемент `public bool IsMultiOpenEnabled { get; set; } = true;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsMultiOpenEnabled { get; set; } = true;
        /// <summary>
        /// Визначає публічний елемент `public string SearchText { get; set; } = "Jan";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string SearchText { get; set; } = "Jan";

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleItemDesignVm> Items { get; set; } = new()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleItemDesignVm> Items { get; set; } = new()
        {
            new ScheduleItemDesignVm { IsChecked = true,  Name = "January shipment", Year = 2026, Month = 1, Shop = new ShopDesignVm { Name = "Biedronka" } },
            new ScheduleItemDesignVm { IsChecked = false, Name = "February shipment", Year = 2026, Month = 2, Shop = new ShopDesignVm { Name = "Lidl" } },
            new ScheduleItemDesignVm { IsChecked = true,  Name = "March shipment", Year = 2026, Month = 3, Shop = new ShopDesignVm { Name = "Auchan" } },
            new ScheduleItemDesignVm { IsChecked = false, Name = "April shipment", Year = 2026, Month = 4, Shop = new ShopDesignVm { Name = "Carrefour" } },
        };

        /// <summary>
        /// Визначає публічний елемент `public ScheduleItemDesignVm SelectedItem { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleItemDesignVm SelectedItem { get; set; }
            = new ScheduleItemDesignVm { IsChecked = false, Name = "February shipment", Year = 2026, Month = 2, Shop = new ShopDesignVm { Name = "Lidl" } };
    }

    /// <summary>
    /// Визначає публічний елемент `public class ScheduleItemDesignVm` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class ScheduleItemDesignVm
    {
        /// <summary>
        /// Визначає публічний елемент `public bool IsChecked { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsChecked { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string Name { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public int Year { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Year { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public int Month { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Month { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public ShopDesignVm Shop { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopDesignVm Shop { get; set; }
        
    }

    /// <summary>
    /// Визначає публічний елемент `public class ShopDesignVm` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class ShopDesignVm
    {
        /// <summary>
        /// Визначає публічний елемент `public string Name { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name { get; set; }
    }
}
