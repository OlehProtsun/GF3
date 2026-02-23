using System;
using System.Linq;
using System.Windows.Input;

namespace WPFApp.View.Availability.Helpers
{
    /// <summary>
    /// AvailabilityViewInputHelper — невеликий helper для code-behind:
    /// - перетворити Key у “токен” для bind lookup (особливо цифри)
    /// - перевірка “всі символи — digits” (для numeric TextBox)
    /// </summary>
    public static class AvailabilityViewInputHelper
    {
        /// <summary>
        /// Перетворити Key у стабільний “токен” для пошуку bind-а.
        ///
        /// Приклади:
        /// - Key.A -> "A"
        /// - Key.D1 -> "1"
        /// - Key.NumPad9 -> "9"
        /// - інші (OemMinus, Space, etc.) -> key.ToString()
        ///
        /// Чому це потрібно:
        /// - owner.TryNormalizeKey(...) для одиночної клавіші робить лише UpperInvariant
        /// - тому "D1" не стане "1"
        /// - а користувач майже завжди очікує bind на "1"
        /// </summary>
        public static string KeyToBindToken(Key key)
        {
            // 1) Літери A..Z.
            if (key >= Key.A && key <= Key.Z)
                return key.ToString();

            // 2) Верхній ряд цифр (D0..D9) -> "0".."9".
            if (key >= Key.D0 && key <= Key.D9)
                return ((char)('0' + (key - Key.D0))).ToString();

            // 3) NumPad цифри -> "0".."9".
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return ((char)('0' + (key - Key.NumPad0))).ToString();

            // 4) Інше — як є (користувач може зробити bind на "OemMinus" тощо).
            return key.ToString();
        }

        /// <summary>
        /// Перевірити, що текст складається лише з цифр.
        /// Це швидше і простіше, ніж Regex для такого кейсу.
        /// </summary>
        public static bool IsAllDigits(string? text)
        {
            // 1) Null/empty — не digits (в контексті “number-only”).
            if (string.IsNullOrEmpty(text))
                return false;

            // 2) Всі символи мають бути char.IsDigit.
            return text.All(char.IsDigit);
        }

        /// <summary>
        /// Визначити, чи комбінація є “стандартним” shortcut-ом, який краще НЕ перехоплювати
        /// при роботі з матрицею (щоб Ctrl+C/Ctrl+V/.. працювали як очікує користувач).
        /// </summary>
        public static bool IsCommonEditorShortcut(Key key, ModifierKeys modifiers)
        {
            if (modifiers != ModifierKeys.Control)
                return false;

            return key is Key.C or Key.V or Key.X or Key.Z or Key.Y or Key.A;
        }
    }
}
