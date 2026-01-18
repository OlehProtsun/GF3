using System.Drawing;
using System.Globalization;

namespace WinFormsApp.View.Shared
{
    public static class ColorHexConverter
    {
        public static string ToHex(Color color)
            => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        public static Color? FromHex(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return null;

            var value = hex.Trim();
            if (value.StartsWith("#", StringComparison.Ordinal))
                value = value[1..];

            if (value.Length == 6)
            {
                if (TryParseByte(value.AsSpan(0, 2), out var r) &&
                    TryParseByte(value.AsSpan(2, 2), out var g) &&
                    TryParseByte(value.AsSpan(4, 2), out var b))
                {
                    return Color.FromArgb(r, g, b);
                }

                return null;
            }

            if (value.Length == 8)
            {
                if (TryParseByte(value.AsSpan(0, 2), out var a) &&
                    TryParseByte(value.AsSpan(2, 2), out var r) &&
                    TryParseByte(value.AsSpan(4, 2), out var g) &&
                    TryParseByte(value.AsSpan(6, 2), out var b))
                {
                    return Color.FromArgb(a, r, g, b);
                }
            }

            return null;
        }

        private static bool TryParseByte(ReadOnlySpan<char> value, out byte result)
            => byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }
}
