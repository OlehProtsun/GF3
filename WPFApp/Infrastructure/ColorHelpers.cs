using System;
using System.Globalization;
using System.Windows.Media;

namespace WPFApp.Infrastructure
{
    public static class ColorHelpers
    {
        public static int ToArgb(Color color)
            => (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

        public static Color FromArgb(int argb)
        {
            var a = (byte)((argb >> 24) & 0xFF);
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)(argb & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }

        public static SolidColorBrush ToBrush(int argb)
        {
            var brush = new SolidColorBrush(FromArgb(argb));
            brush.Freeze();
            return brush;
        }

        public static bool TryParseHexColor(string? hex, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(hex))
                return false;

            var text = hex.Trim();
            if (text.StartsWith("#", StringComparison.Ordinal))
                text = text.Substring(1);

            if (text.Length == 6)
                text = $"FF{text}";

            if (text.Length != 8)
                return false;

            if (!int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
                return false;

            color = FromArgb(argb);
            return true;
        }

        public static string ToHex(Color color)
            => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
