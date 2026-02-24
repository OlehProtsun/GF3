using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Media;

namespace WPFApp.UI.Helpers
{
    /// <summary>
    /// ColorHelpers — утиліти для роботи з Color/ARGB/Hex і кешування SolidColorBrush.
    ///
    /// Основна ідея:
    /// - Багато місць у UI можуть просити Brush для одного і того ж кольору.
    /// - Створювати новий SolidColorBrush кожного разу — зайва алокація + навантаження на GC.
    /// - Ми кешуємо brush по ключу ARGB (int).
    ///
    /// Важливо:
    /// - Ми викликаємо Freeze() для brush, щоб зробити його thread-safe і дешевшим для WPF.
    /// - Кеш НЕ має ліміту. Якщо ти генеруєш тисячі випадкових кольорів — кеш ростиме.
    ///   Для такого сценарію доданий ClearBrushCache().
    /// </summary>
    public static class ColorHelpers
    {
        // ConcurrentDictionary:
        // - прибирає потребу в lock
        // - GetOrAdd дає атомарне створення при конкурентному доступі
        private static readonly ConcurrentDictionary<int, SolidColorBrush> _brushCache = new();

        /// <summary>
        /// ToArgb — упакувати Color у int формату 0xAARRGGBB.
        /// </summary>
        public static int ToArgb(Color color)
            // (A << 24) | (R << 16) | (G << 8) | B — стандартна упаковка.
            => (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

        /// <summary>
        /// FromArgb — розпакувати int 0xAARRGGBB у WPF Color.
        /// </summary>
        public static Color FromArgb(int argb)
        {
            // Витягуємо байти шляхом бітових зсувів і масок.
            var a = (byte)((argb >> 24) & 0xFF);
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)(argb & 0xFF);

            // Створюємо WPF Color з компонент.
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// ToBrush — отримати Frozen SolidColorBrush для ARGB int.
        /// Кеш: один ARGB -> один brush.
        /// </summary>
        public static SolidColorBrush ToBrush(int argb)
        {
            // GetOrAdd:
            // - якщо brush вже є — поверне існуючий
            // - якщо нема — викличе фабрику і збереже результат
            return _brushCache.GetOrAdd(argb, static key =>
            {
                // Створюємо brush з Color.
                var brush = new SolidColorBrush(FromArgb(key));

                // Freeze робить Freezable immutable, швидшим і безпечним для використання між потоками.
                brush.Freeze();

                return brush;
            });
        }

        /// <summary>
        /// ToBrush — зручний overload: Color -> brush.
        /// </summary>
        public static SolidColorBrush ToBrush(Color color)
            => ToBrush(ToArgb(color));

        /// <summary>
        /// ClearBrushCache — очистити кеш brush.
        /// Корисно:
        /// - при зміні теми
        /// - при “reset” UI
        /// - якщо були експерименти з великою кількістю кольорів
        /// </summary>
        public static void ClearBrushCache()
            => _brushCache.Clear();

        /// <summary>
        /// TryParseHexColor — розпарсити hex-колір:
        /// Підтримуються формати:
        /// - #RRGGBB        (alpha = FF)
        /// - #AARRGGBB
        /// - #RGB           (розширюється до #FFRRGGBB)
        /// - #ARGB          (розширюється до #AARRGGBB)
        ///
        /// Повертає true/false, без exception (UI-friendly).
        /// </summary>
        public static bool TryParseHexColor(string? hex, out Color color)
        {
            // За замовчуванням повертаємо default (прозорий чорний).
            color = default;

            // Якщо вхід пустий — неуспіх.
            if (string.IsNullOrWhiteSpace(hex))
                return false;

            // Беремо span, щоб мінімізувати алокації на Trim/Substring.
            ReadOnlySpan<char> span = hex.AsSpan().Trim();

            // Дозволяємо, але не вимагаємо '#'.
            if (span.Length > 0 && span[0] == '#')
                span = span.Slice(1);

            // Далі нам треба привести до рівно 8 hex-символів (AARRGGBB).
            // Використовуємо stackalloc буфер на 8 символів: нуль алокацій у heap.
            Span<char> buffer = stackalloc char[8];

            // Формат #RGB (3 символи): кожен дублюємо, alpha = FF.
            if (span.Length == 3)
            {
                buffer[0] = 'F';
                buffer[1] = 'F';
                buffer[2] = span[0]; buffer[3] = span[0]; // RR
                buffer[4] = span[1]; buffer[5] = span[1]; // GG
                buffer[6] = span[2]; buffer[7] = span[2]; // BB
            }
            // Формат #ARGB (4 символи): кожен дублюємо.
            else if (span.Length == 4)
            {
                buffer[0] = span[0]; buffer[1] = span[0]; // AA
                buffer[2] = span[1]; buffer[3] = span[1]; // RR
                buffer[4] = span[2]; buffer[5] = span[2]; // GG
                buffer[6] = span[3]; buffer[7] = span[3]; // BB
            }
            // Формат #RRGGBB (6 символів): alpha = FF + копія.
            else if (span.Length == 6)
            {
                buffer[0] = 'F';
                buffer[1] = 'F';
                span.CopyTo(buffer.Slice(2));
            }
            // Формат #AARRGGBB (8 символів): копія як є.
            else if (span.Length == 8)
            {
                span.CopyTo(buffer);
            }
            else
            {
                // Будь-яка інша довжина — неуспіх.
                return false;
            }

            // Парсимо 8 hex символів у uint.
            if (!uint.TryParse(buffer, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
                return false;

            // Перетворюємо на int (unchecked дозволяє коректно зберегти бітовий патерн).
            color = FromArgb(unchecked((int)argb));

            return true;
        }

        /// <summary>
        /// ToHex — серіалізувати Color у hex.
        /// За замовчуванням повертаємо #AARRGGBB (бо це однозначно).
        /// </summary>
        public static string ToHex(Color color, bool includeAlpha = true)
        {
            // Якщо альфа не потрібна — повертаємо #RRGGBB.
            if (!includeAlpha)
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            // Стандартний #AARRGGBB.
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
