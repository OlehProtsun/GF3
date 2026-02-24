using System;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.Applications.Matrix.Availability
{
    /// <summary>
    /// AvailabilityCellCodeParser — маленький helper для коду клітинки availability-матриці.
    ///
    /// Дозволені значення:
    /// - ""  (порожньо)        -> “нема запису / не задано” (залишаємо як є, бо так було в твоєму VM)
    /// - "+"                  -> AvailabilityKind.ANY
    /// - "-"                  -> AvailabilityKind.NONE
    /// - "HH:mm-HH:mm"        -> AvailabilityKind.INT (інтервал)
    ///   Також приймаємо пробіли: "HH:mm - HH:mm"
    ///
    /// Також робимо нормалізацію:
    /// - "9:00 - 18:00" => "09:00-18:00"
    /// </summary>
    public static class AvailabilityCellCodeParser
    {
        public const string AnyMark = "+";
        public const string NoneMark = "-";

        /// <summary>
        /// Спробувати:
        /// 1) перевірити значення
        /// 2) повернути канонічний формат (normalized)
        /// 3) повернути error, якщо невалідно
        ///
        /// allowOvernight:
        /// - якщо true, дозволяє "22:00-06:00" (перехід через 00:00)
        /// - якщо false (дефолт), вимагає end > start
        /// </summary>
        public static bool TryNormalize(string? raw, out string normalized, out string? error, bool allowOvernight = false)
        {
            normalized = string.Empty;
            error = null;

            raw = (raw ?? string.Empty).Trim();

            // Порожнє значення в тебе допустиме — залишаємо (це важливо для сценарію "нема запису")
            if (raw.Length == 0)
                return true;

            // Швидкі маркери
            if (raw == AnyMark || raw == NoneMark)
            {
                normalized = raw;
                return true;
            }

            // Інтервал: очікуємо "from-to"
            var parts = raw.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "Allowed: +, -, HH:mm-HH:mm or HH:mm - HH:mm (e.g., 09:00-18:00).";
                return false;
            }

            if (!ScheduleMatrixEngine.TryParseTime(parts[0], out var from) ||
                !ScheduleMatrixEngine.TryParseTime(parts[1], out var to))
            {
                error = "Time must be in HH:mm format (e.g., 09:00).";
                return false;
            }

            // Якщо не дозволяємо “нічні” інтервали — кінець має бути строго пізніше початку
            if (!allowOvernight && to <= from)
            {
                error = "End time must be later than start time.";
                return false;
            }

            // Канонічний формат для збереження/читання
            normalized = $"{from:hh\\:mm}-{to:hh\\:mm}";
            return true;
        }
    }
}
