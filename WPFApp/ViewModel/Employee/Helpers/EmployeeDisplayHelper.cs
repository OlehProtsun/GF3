using BusinessLogicLayer.Contracts.Employees;
using System;

namespace WPFApp.ViewModel.Employee.Helpers
{
    /// <summary>
    /// EmployeeDisplayHelper — маленький helper для “людського” форматування Employee.
    ///
    /// Навіщо:
    /// - FullName збирається у кількох місцях (ProfileVM, Confirm delete, тощо)
    /// - Email/Phone у профілі показуються як "—" якщо пусто
    /// - краще мати 1 центр правди, ніж копіювати Trim/— логіку
    /// </summary>
    public static class EmployeeDisplayHelper
    {
        /// <summary>
        /// Побудувати повне ім’я "First Last" з trim.
        /// Повертає "" якщо обидві частини порожні.
        /// </summary>
        public static string GetFullName(EmployeeDto? model)
        {
            // 1) Null-safe.
            if (model is null)
                return string.Empty;

            // 2) Trim частин.
            var first = (model.FirstName ?? string.Empty).Trim();
            var last = (model.LastName ?? string.Empty).Trim();

            // 3) Склейка з пробілом тільки там, де треба.
            //    (Якщо одна частина пусто — пробіли не “висять”.)
            var full = $"{first} {last}".Trim();

            // 4) Повертаємо.
            return full;
        }

        /// <summary>
        /// Повернути "—" якщо значення пусте, інакше — Trim.
        /// </summary>
        public static string TextOrDash(string? value)
        {
            // 1) Null/whitespace — "—".
            if (string.IsNullOrWhiteSpace(value))
                return "—";

            // 2) Інакше повертаємо Trim.
            return value.Trim();
        }
    }
}
