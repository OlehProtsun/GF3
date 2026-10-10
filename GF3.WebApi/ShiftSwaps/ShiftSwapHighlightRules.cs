using System.Globalization;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;

namespace WebApi.ShiftSwaps;

internal static partial class ShiftSwapHighlightRules
{
    public const string DefaultColor = "#BBF7D0";

    public static string NormalizeColor(string? value, string fieldName)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (normalized is null || !HexColorRegex().IsMatch(normalized))
        {
            throw ValidationException.ForField(fieldName, "Choose a valid color in #RRGGBB format.");
        }

        return normalized;
    }

    public static int ToArgb(string? value)
    {
        var normalized = value is not null && HexColorRegex().IsMatch(value)
            ? value
            : DefaultColor;
        return unchecked((int)(0xFF000000u | uint.Parse(normalized.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();
}
