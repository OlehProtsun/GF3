/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.Totals у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Collections.Generic;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        
        
        
        
        
        
        
        
        
        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public string GetEmployeeTotalHoursText(string columnName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string GetEmployeeTotalHoursText(string columnName)
        {
            if (SelectedBlock is null)
                return string.Empty;

            
            if (!_colNameToEmpId.TryGetValue(columnName, out var empId))
                return string.Empty;

            return _employeeTotalHoursText.TryGetValue(empId, out var text)
                ? text
                : "Total hours: 0h 0m";
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private void RecalculateTotals()
        {
            var block = SelectedBlock;

            if (block is null)
            {
                _employeeTotalHoursText.Clear();
                TotalEmployees = 0;
                TotalHoursText = "0h 0m";
                return;
            }

            
            var result = ScheduleTotalsCalculator.Calculate(block.Employees, block.Slots);

            
            var aliveIds = new HashSet<int>();

            foreach (var emp in block.Employees)
            {
                var empId = emp.EmployeeId;
                aliveIds.Add(empId);

                result.PerEmployeeDuration.TryGetValue(empId, out var empTotal);

                _employeeTotalHoursText[empId] =
                    $"Total hours: {ScheduleTotalsCalculator.FormatHoursMinutes(empTotal)}";
            }

            
            if (_employeeTotalHoursText.Count > aliveIds.Count)
            {
                var toRemove = new List<int>();

                foreach (var id in _employeeTotalHoursText.Keys)
                {
                    if (!aliveIds.Contains(id))
                        toRemove.Add(id);
                }

                foreach (var id in toRemove)
                    _employeeTotalHoursText.Remove(id);
            }

            
            TotalEmployees = result.TotalEmployees;
            TotalHoursText = ScheduleTotalsCalculator.FormatHoursMinutes(result.TotalDuration);
        }

    }
}
