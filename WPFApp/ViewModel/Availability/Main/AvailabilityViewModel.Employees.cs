using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Employees — локальний кеш працівників і фільтрація списку в EditVm.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        // Повний список працівників (кеш).
        private readonly List<EmployeeModel> _allEmployees = new();

        // Lookup employeeId -> displayName.
        private readonly Dictionary<int, string> _employeeNames = new();

        // Кеш останнього застосованого фільтра (щоб не робити ту саму роботу двічі).
        private string? _lastEmployeeFilter;

        internal void ApplyEmployeeFilter(string? raw)
        {
            // 1) Нормалізуємо term.
            var term = raw?.Trim() ?? string.Empty;

            // 2) Якщо term не змінився (OrdinalIgnoreCase) — нічого не робимо.
            if (string.Equals(_lastEmployeeFilter, term, StringComparison.OrdinalIgnoreCase))
                return;

            // 3) Запам’ятовуємо term як “останній застосований”.
            _lastEmployeeFilter = term;

            // 4) Якщо term порожній — повертаємо повний список.
            if (string.IsNullOrWhiteSpace(term))
            {
                EditVm.SetEmployees(_allEmployees, _employeeNames);
                return;
            }

            // 5) Фільтруємо вручну (швидше і без LINQ ToList зайвих алокацій).
            var filtered = new List<EmployeeModel>();

            for (int i = 0; i < _allEmployees.Count; i++)
            {
                var e = _allEmployees[i];

                // 6) Умова: term входить у FirstName або LastName.
                if (ContainsIgnoreCase(e.FirstName, term) || ContainsIgnoreCase(e.LastName, term))
                    filtered.Add(e);
            }

            // 7) Віддаємо результат у EditVm.
            EditVm.SetEmployees(filtered, _employeeNames);
        }

        private void ResetEmployeeSearch()
        {
            // 1) Скидаємо текст.
            EditVm.EmployeeSearchText = string.Empty;

            // 2) Скидаємо кеш терму.
            _lastEmployeeFilter = null;

            // 3) Віддаємо повний список.
            EditVm.SetEmployees(_allEmployees, _employeeNames);
        }

        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
