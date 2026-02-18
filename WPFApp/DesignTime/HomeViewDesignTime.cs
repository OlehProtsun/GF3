using System;
using System.Collections.ObjectModel;

namespace WPFApp.DesignTime
{
    // Design-time VM (тільки для preview у дизайнері)
    public class HomeViewDesignTime
    {
        public string CurrentTimeText { get; set; }

        public ObservableCollection<WhoWorksTodayRow> WhoWorksTodayItems { get; set; }

        public int MonthSchedulesCount { get; set; }
        public int TotalContainersCount { get; set; }
        public int TodayAssignmentsCount { get; set; }
        public int ActiveShopsCount { get; set; }

        public string StatusText { get; set; }

        public ObservableCollection<ActiveScheduleCard> ActiveSchedules { get; set; }

        // ОБОВʼЯЗКОВО: public ctor без параметрів (щоб дизайнер міг створити інстанс)
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

    // Рядок для "Who Work Today?"
    public class WhoWorksTodayRow
    {
        public string Employee { get; set; }
        public string Shift { get; set; }
        public string Shop { get; set; }
    }

    // Карточка для "Active schedules this month"
    public class ActiveScheduleCard
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public ObservableCollection<ActiveScheduleItemRow> Items { get; set; }
    }

    // Рядок всередині active schedule grid
    public class ActiveScheduleItemRow
    {
        public string Day { get; set; }
        public string Employee { get; set; }
        public string Shift { get; set; }
    }
}
