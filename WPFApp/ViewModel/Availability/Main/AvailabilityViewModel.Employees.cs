/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewModel.Employees у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;

namespace WPFApp.ViewModel.Availability.Main
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        
        private readonly List<EmployeeModel> _allEmployees = new();

        
        private readonly Dictionary<int, string> _employeeNames = new();

        
        private string? _lastEmployeeFilter;

        internal void ApplyEmployeeFilter(string? raw)
        {
            
            var term = raw?.Trim() ?? string.Empty;

            
            if (string.Equals(_lastEmployeeFilter, term, StringComparison.OrdinalIgnoreCase))
                return;

            
            _lastEmployeeFilter = term;

            
            if (string.IsNullOrWhiteSpace(term))
            {
                EditVm.SetEmployees(_allEmployees, _employeeNames);
                return;
            }

            
            var filtered = new List<EmployeeModel>();

            for (int i = 0; i < _allEmployees.Count; i++)
            {
                var e = _allEmployees[i];

                
                if (ContainsIgnoreCase(e.FirstName, term) || ContainsIgnoreCase(e.LastName, term))
                    filtered.Add(e);
            }

            
            EditVm.SetEmployees(filtered, _employeeNames);
        }

        private void ResetEmployeeSearch()
        {
            
            EditVm.EmployeeSearchText = string.Empty;

            
            _lastEmployeeFilter = null;

            
            EditVm.SetEmployees(_allEmployees, _employeeNames);
        }

        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
