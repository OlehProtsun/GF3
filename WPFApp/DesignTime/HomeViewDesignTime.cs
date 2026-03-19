/*
  Опис файлу: цей модуль містить реалізацію компонента HomeViewDesignTime у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.ObjectModel;

namespace WPFApp.DesignTime
{
    
    /// <summary>
    /// Визначає публічний елемент `public class HomeViewDesignTime` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class HomeViewDesignTime
    {
        /// <summary>
        /// Визначає публічний елемент `public string CurrentTimeText { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string CurrentTimeText { get; set; }

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<WhoWorksTodayRow> WhoWorksTodayItems { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<WhoWorksTodayRow> WhoWorksTodayItems { get; set; }

        /// <summary>
        /// Визначає публічний елемент `public int MonthSchedulesCount { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int MonthSchedulesCount { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public int TotalContainersCount { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalContainersCount { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public int TodayAssignmentsCount { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TodayAssignmentsCount { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public int ActiveShopsCount { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ActiveShopsCount { get; set; }

        /// <summary>
        /// Визначає публічний елемент `public string StatusText { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string StatusText { get; set; }

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ActiveScheduleCard> ActiveSchedules { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ActiveScheduleCard> ActiveSchedules { get; set; }

        
        /// <summary>
        /// Визначає публічний елемент `public HomeViewDesignTime()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public HomeViewDesignTime()
        {
            CurrentTimeText = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            WhoWorksTodayItems = new ObservableCollection<WhoWorksTodayRow>
            {
                new WhoWorksTodayRow { Employee = "Ivan Petrenko", Shift = "Day",   Shop = "Shop A" },
                new WhoWorksTodayRow { Employee = "Olena Kovalenko", Shift = "Night", Shop = "Shop B" },
                new WhoWorksTodayRow { Employee = "Andrii Melnyk",   Shift = "Day",   Shop = "Shop C" },
            };

            MonthSchedulesCount = 12;
            TotalContainersCount = 248;
            TodayAssignmentsCount = 7;
            ActiveShopsCount = 4;

            StatusText = "Design preview: sample active schedules loaded.";

            ActiveSchedules = new ObservableCollection<ActiveScheduleCard>
            {
                new ActiveScheduleCard
                {
                    Title = "Schedule • Shop A",
                    Subtitle = "February (Week 3) • 6 items",
                    Items = new ObservableCollection<ActiveScheduleItemRow>
                    {
                        new ActiveScheduleItemRow { Day = "Mon", Employee = "Ivan Petrenko",  Shift = "Day" },
                        new ActiveScheduleItemRow { Day = "Tue", Employee = "Olena Kovalenko", Shift = "Night" },
                        new ActiveScheduleItemRow { Day = "Wed", Employee = "Andrii Melnyk",  Shift = "Day" },
                    }
                },
                new ActiveScheduleCard
                {
                    Title = "Schedule • Shop B",
                    Subtitle = "February (Week 3) • 4 items",
                    Items = new ObservableCollection<ActiveScheduleItemRow>
                    {
                        new ActiveScheduleItemRow { Day = "Thu", Employee = "Nazar Hrytsenko", Shift = "Day" },
                        new ActiveScheduleItemRow { Day = "Fri", Employee = "Sofiia Bondar",   Shift = "Night" },
                    }
                }
            };
        }
    }

    
    /// <summary>
    /// Визначає публічний елемент `public class WhoWorksTodayRow` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class WhoWorksTodayRow
    {
        /// <summary>
        /// Визначає публічний елемент `public string Employee { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Employee { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string Shift { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Shift { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string Shop { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Shop { get; set; }
    }

    
    /// <summary>
    /// Визначає публічний елемент `public class ActiveScheduleCard` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class ActiveScheduleCard
    {
        /// <summary>
        /// Визначає публічний елемент `public string Title { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string Subtitle { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Subtitle { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ActiveScheduleItemRow> Items { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ActiveScheduleItemRow> Items { get; set; }
    }

    
    /// <summary>
    /// Визначає публічний елемент `public class ActiveScheduleItemRow` та контракт його використання у шарі WPFApp.
    /// </summary>
    public class ActiveScheduleItemRow
    {
        /// <summary>
        /// Визначає публічний елемент `public string Day { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Day { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string Employee { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Employee { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string Shift { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Shift { get; set; }
    }
}
