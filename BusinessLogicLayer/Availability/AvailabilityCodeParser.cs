using BusinessLogicLayer.Contracts.Enums;
using System.Globalization;

namespace BusinessLogicLayer.Availability;

/// <summary>
/// Converts short availability codes from the UI or import payloads into normalized domain values.
/// The supported mini-language is intentionally small so every entry point interprets availability in exactly
/// the same way: <c>+</c> means "available for any shift", <c>-</c> or empty means "not available", and any
/// other value must be a valid time interval.
/// </summary>
public static class AvailabilityCodeParser
{
    private static readonly string[] SupportedTimeFormats = ["h\\:mm", "hh\\:mm"];

    public const string AnyMark = "+";
    public const string NoneMark = "-";

    /// <summary>
    /// Attempts to parse a raw code into the domain availability kind and, when relevant, a normalized interval.
    /// </summary>
    public static bool TryParse(string? code, out AvailabilityKind kind, out string? intervalStr)
    {
        code = NormalizeCode(code);
        intervalStr = null;

        if (string.IsNullOrEmpty(code) || code == NoneMark)
        {
            kind = AvailabilityKind.NONE;
            return true;
        }

        if (code == AnyMark)
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

    /// <summary>
    /// Normalizes user-entered intervals to the canonical <c>HH:mm - HH:mm</c> format.
    /// Accepted inputs include compact and spaced forms such as <c>8:00-17:30</c> and <c>08:00 - 17:30</c>.
    /// </summary>
    public static bool TryNormalizeInterval(string input, out string normalized)
    {
        normalized = string.Empty;

        var parts = input.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseTime(parts[0], out var start) || !TryParseTime(parts[1], out var end))
        {
            return false;
        }

        if (end <= start)
        {
            return false;
        }

        normalized = $"{start:hh\\:mm} - {end:hh\\:mm}";
        return true;
    }

    private static string NormalizeCode(string? code) => (code ?? string.Empty).Trim();

    private static bool TryParseTime(string value, out TimeSpan time) =>
        TimeSpan.TryParseExact(value, SupportedTimeFormats, CultureInfo.InvariantCulture, out time);
}
