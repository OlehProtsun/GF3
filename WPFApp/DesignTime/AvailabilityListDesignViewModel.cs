/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityListDesignViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Collections.ObjectModel;

namespace WPFApp.DesignTime
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityListDesignRow` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityListDesignRow
    {
        /// <summary>
        /// Визначає публічний елемент `public string Name { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityListDesignViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityListDesignViewModel
    {
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<AvailabilityListDesignRow> Items { get; } =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<AvailabilityListDesignRow> Items { get; } =
            new ObservableCollection<AvailabilityListDesignRow>
            {
                new AvailabilityListDesignRow { Name = "Availability January 2026" },
                new AvailabilityListDesignRow { Name = "Availability February 2026" },
                new AvailabilityListDesignRow { Name = "Availability March 2026" },
            };

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityListDesignRow? SelectedItem { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityListDesignRow? SelectedItem { get; set; }

        /// <summary>
        /// Визначає публічний елемент `public string SearchText { get; set; } = "jan";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string SearchText { get; set; } = "jan";
    }
}
