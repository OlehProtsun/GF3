using System;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace WPFApp.Infrastructure.Hotkeys
{
    /// <summary>
    /// KeyGestureTextHelper — єдиний helper для:
    /// 1) форматування Key+Modifiers у строку (для відображення)
    /// 2) нормалізації строкового вводу користувача (для збереження/порівняння)
    ///
    /// Чому це винесено в Infrastructure:
    /// - логіка не належить конкретному ViewModel
    /// - у майбутньому її захочеться використовувати в Schedule/Container/будь-де
    /// - це зменшує дублювання коду і помилок “у різних місцях по-різному”
    /// </summary>
    public static class KeyGestureTextHelper
    {
        /// <summary>
        /// Спроба сформувати “людський” текст для KeyGesture (напр. "Ctrl+M").
        ///
        /// Повертає null якщо:
        /// - користувач натиснув тільки модифікатор (Ctrl/Shift/Alt/Win)
        /// - або виникла помилка при побудові KeyGesture
        /// </summary>
        public static string? FormatKeyGesture(Key key, ModifierKeys modifiers, CultureInfo? culture = null)
        {
            // 1) Culture для відображення:
            //    InvariantCulture дає стабільні строки (важливо для порівняння/збереження).
            culture ??= CultureInfo.InvariantCulture;

            // 2) Якщо натиснуто лише “службову” клавішу модифікатора — не вважаємо це hotkey.
            if (key is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt
                or Key.LWin or Key.RWin)
            {
                return null;
            }

            try
            {
                // 3) Створюємо KeyGesture.
                var gesture = new KeyGesture(key, modifiers);

                // 4) Отримуємо строку в стабільному форматі.
                return gesture.GetDisplayStringForCulture(culture);
            }
            catch
            {
                // 5) Якщо WPF викинув виняток на комбінації — повертаємо null.
                return null;
            }
        }

        /// <summary>
        /// Нормалізувати ввід користувача у стабільний формат (для ключа bind-а).
        ///
        /// Приклади:
        /// - "m"    => "M"
        /// - " 1 "  => "1"
        /// - "Ctrl + m" => "Ctrl+M" (через KeyGestureConverter)
        ///
        /// Правила:
        /// - Якщо рядок не містить '+' — приймаємо як “одна клавіша” і нормалізуємо регістр (UpperInvariant).
        /// - Якщо містить '+' — намагаємось прочитати як KeyGesture (через KeyGestureConverter).
        /// </summary>
        public static bool TryNormalizeKey(string raw, out string normalized, CultureInfo? culture = null)
        {
            // 1) Ініціалізація out-параметра.
            normalized = string.Empty;

            // 2) Перевірка на null/whitespace.
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            // 3) Culture для стабільного формату.
            culture ??= CultureInfo.InvariantCulture;

            // 4) Тримаємо trim (прибираємо пробіли по краях).
            raw = raw.Trim();

            // 5) Якщо це НЕ комбінація (немає '+'):
            //    - нормалізуємо в один регістр
            //    - повертаємо true
            if (!raw.Contains('+'))
            {
                normalized = raw.ToUpperInvariant();
                return true;
            }

            // 6) Якщо '+' є — підготуємо “очищений” варіант:
            //    - розділити по '+'
            //    - trim кожної частини
            //    - прибрати пусті
            //    - зібрати назад як "Ctrl+M"
            var cleaned = string.Join("+",
                raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Where(p => !string.IsNullOrWhiteSpace(p)));

            // 7) Якщо після чистки фактично не вийшла комбінація — вважаємо невалідним.
            //    (напр. "Ctrl+" або "+" або "Ctrl++M")
            if (!cleaned.Contains('+'))
                return false;

            // 8) Стандартний конвертер WPF для строк -> KeyGesture.
            var converter = new KeyGestureConverter();

            try
            {
                // 9) Спочатку пробуємо cleaned, бо він стабільніший.
                if (converter.ConvertFromString(cleaned) is KeyGesture gesture1)
                {
                    normalized = gesture1.GetDisplayStringForCulture(culture);
                    return true;
                }

                // 10) На всяк випадок пробуємо raw (інколи converter може бути більш поблажливий до оригіналу).
                if (converter.ConvertFromString(raw) is KeyGesture gesture2)
                {
                    normalized = gesture2.GetDisplayStringForCulture(culture);
                    return true;
                }
            }
            catch
            {
                // 11) Будь-який виняток — вважаємо формат невалідним.
                return false;
            }

            // 12) Якщо converter не зміг — ключ невалідний.
            return false;
        }
    }
}
