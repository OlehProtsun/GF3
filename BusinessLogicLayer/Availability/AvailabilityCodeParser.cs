using DataAccessLayer.Models.Enums;
using System.Globalization;

namespace BusinessLogicLayer.Availability
{
    public static class AvailabilityCodeParser
    {
        public static bool TryParse(string code, out AvailabilityKind kind, out string? intervalStr)
        {
            code = (code ?? string.Empty).Trim();
            intervalStr = null;

            if (string.IsNullOrEmpty(code) || code == "-")
            {
                kind = AvailabilityKind.NONE;
                return true;
            }

            if (code == "+")
            {
                kind = AvailabilityKind.ANY;
                return true;
            }

            if (!TryNormalizeInterval(code, out var normalized))
            {
                kind = default;
                return false;
            }

            kind = AvailabilityKind.INT;
            intervalStr = normalized;
            return true;
        }

        // Приймає: "8:00-17:30", "08:00 - 17:30", "08:00-17:30"
        public static bool TryNormalizeInterval(string input, out string normalized)
        {
            normalized = string.Empty;

            var parts = input.Split('-', StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;

            if (!TryParseTime(parts[0], out var start)) return false;
            if (!TryParseTime(parts[1], out var end)) return false;

            if (end <= start) return false;

            normalized = $"{start:hh\\:mm} - {end:hh\\:mm}";
            return true;
        }

        private static bool TryParseTime(string s, out TimeSpan ts)
        {
            // стабільно для "H:mm" та "HH:mm"
            return TimeSpan.TryParseExact(s, new[] { "h\\:mm", "hh\\:mm" }, CultureInfo.InvariantCulture, out ts);
        }
    }
}
