using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Models
{
    public class BindModel
    {
        public int Id { get; set; }

        // що вставляємо в AvailabilityDays.Value: "+", "-", "HH:mm - HH:mm"
        public string Value { get; set; } = string.Empty;

        // хоткей у форматі WinForms KeysConverter: "F1", "Ctrl+1", "Ctrl+Shift+A"
        public string Key { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
