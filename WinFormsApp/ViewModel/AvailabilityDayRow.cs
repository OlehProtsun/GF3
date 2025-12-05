using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.ViewModel
{
    public class AvailabilityDayRow
    {
        public int DayOfMonth { get; set; }
        /// <summary>
        /// "+", "-", "09:00 - 15:00" і т.п.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
