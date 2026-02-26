/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewModel.Matrix у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.ViewModel.Availability.Helpers;

namespace WPFApp.ViewModel.Availability.Edit
{
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        /// <summary>
        /// Визначає публічний елемент `public void SetEmployees(IEnumerable<EmployeeModel> employees, IReadOnlyDictionary<int, string> nameLookup)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetEmployees(IEnumerable<EmployeeModel> employees, IReadOnlyDictionary<int, string> nameLookup)
        {
            
            Employees.Clear();

            
            _employeeNames.Clear();

            
            foreach (var employee in employees)
            {
                
                var name = nameLookup.TryGetValue(employee.Id, out var fullName)
                    ? fullName
                    
                    : $"{employee.FirstName} {employee.LastName}";

                
                Employees.Add(new EmployeeListItem { Id = employee.Id, FullName = name });

                
                _employeeNames[employee.Id] = name;
            }

            
            bool captionsChanged = false;

            foreach (var kv in _employeeIdToColumn)
            {
                
                if (!_employeeNames.TryGetValue(kv.Key, out var displayName))
                    continue;

                
                if (_groupTable.Columns.Contains(kv.Value))
                {
                    var col = _groupTable.Columns[kv.Value];

                    
                    if (!string.Equals(col.Caption, displayName, StringComparison.Ordinal))
                    {
                        col.Caption = displayName;
                        captionsChanged = true;
                    }
                }
            }

            
            if (captionsChanged)
                NotifyMatrixChanged();
        }

        /// <summary>
        /// Визначає публічний елемент `public void ResetForNew()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ResetForNew()
        {
            
            using var _ = EnterMatrixUpdate();

            
            using var __ = EnterDateSync();

            
            AvailabilityId = 0;
            AvailabilityName = string.Empty;

            
            AvailabilityMonth = DateTime.Today.Month;
            AvailabilityYear = DateTime.Today.Year;

            
            ClearValidationErrors();

            
            ResetGroupMatrixCore(regenerateDays: true);
        }

        /// <summary>
        /// Визначає публічний елемент `public void LoadGroup(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void LoadGroup(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days,
            IReadOnlyDictionary<int, string> nameLookup)
        {
            
            using var _ = EnterMatrixUpdate();

            
            using var __ = EnterDateSync();

            
            AvailabilityId = group.Id;
            AvailabilityName = group.Name ?? string.Empty;
            AvailabilityMonth = group.Month;
            AvailabilityYear = group.Year;

            
            ClearValidationErrors();

            
            ResetGroupMatrixCore(regenerateDays: true);

            
            foreach (var m in members)
            {
                
                var header = m.Employee is null
                    ? (nameLookup.TryGetValue(m.EmployeeId, out var n) ? n : $"Employee #{m.EmployeeId}")
                    
                    : $"{m.Employee.FirstName} {m.Employee.LastName}";

                
                TryAddEmployeeColumn(m.EmployeeId, header);
            }

            
            int dim = DateTime.DaysInMonth(group.Year, group.Month);

            
            
            var dayLookup = days
                .GroupBy(d => (d.AvailabilityGroupMemberId, d.DayOfMonth))
                .ToDictionary(g => g.Key, g => g.Last());

            
            foreach (var mb in members)
            {
                
                var codes = new (int day, string code)[dim];

                
                for (int day = 1; day <= dim; day++)
                {
                    
                    if (!dayLookup.TryGetValue((mb.Id, day), out var d))
                    {
                        codes[day - 1] = (day, AvailabilityCellCodeParser.NoneMark);
                        continue;
                    }

                    
                    var code = d.Kind switch
                    {
                        AvailabilityKind.ANY => AvailabilityCellCodeParser.AnyMark,
                        AvailabilityKind.NONE => AvailabilityCellCodeParser.NoneMark,
                        AvailabilityKind.INT => d.IntervalStr ?? string.Empty,
                        _ => AvailabilityCellCodeParser.NoneMark
                    };

                    
                    codes[day - 1] = (day, code);
                }

                
                SetEmployeeCodes(mb.EmployeeId, codes);
            }

            
            
            
            NormalizeAndValidateAllMatrixCells();
        }

        /// <summary>
        /// Визначає публічний елемент `public void ResetGroupMatrix()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ResetGroupMatrix()
        {
            
            using var _ = EnterMatrixUpdate();

            
            ResetGroupMatrixCore(regenerateDays: true);
        }

        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<int> GetSelectedEmployeeIds()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<int> GetSelectedEmployeeIds()
            
            => _employeeIdToColumn.Keys.ToList();

        /// <summary>
        /// Визначає публічний елемент `public IList<(int employeeId, IList<(int dayOfMonth, string code)> codes)> ReadGroupCodes()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IList<(int employeeId, IList<(int dayOfMonth, string code)> codes)> ReadGroupCodes()
        {
            
            var result = new List<(int employeeId, IList<(int dayOfMonth, string code)> codes)>(
                capacity: _employeeIdToColumn.Count);

            
            foreach (var kv in _employeeIdToColumn)
            {
                
                var list = AvailabilityMatrixEngine.ReadEmployeeCodes(_groupTable, kv.Value);

                
                result.Add((kv.Key, list));
            }

            return result;
        }

        /// <summary>
        /// Визначає публічний елемент `public void SetEmployeeCodes(int employeeId, IEnumerable<(int dayOfMonth, string code)> codes)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetEmployeeCodes(int employeeId, IEnumerable<(int dayOfMonth, string code)> codes)
        {
            
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return;

            
            AvailabilityMatrixEngine.SetEmployeeCodes(_groupTable, colName, codes);
        }

        private void RegenerateGroupDays()
        {
            
            int year = AvailabilityYear;
            int month = AvailabilityMonth;

            
            AvailabilityMatrixEngine.EnsureDayRowsForMonth(_groupTable, year, month);

            
            NotifyMatrixChanged();
        }

        private bool TryAddEmployeeColumn(int employeeId, string header)
        {
            
            if (_employeeIdToColumn.ContainsKey(employeeId))
                return false;

            
            if (!AvailabilityMatrixEngine.TryAddEmployeeColumn(_groupTable, employeeId, header, out var colName))
                return false;

            
            _employeeIdToColumn[employeeId] = colName;

            
            NotifyMatrixChanged();

            return true;
        }

        private bool RemoveEmployeeColumn(int employeeId)
        {
            
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return false;

            
            _employeeIdToColumn.Remove(employeeId);

            
            AvailabilityMatrixEngine.RemoveEmployeeColumn(_groupTable, colName);

            
            NotifyMatrixChanged();

            return true;
        }

        private void ResetGroupMatrixCore(bool regenerateDays)
        {
            
            _employeeIdToColumn.Clear();

            
            
            
            
            
            AvailabilityMatrixEngine.Reset(_groupTable, regenerateDays, AvailabilityYear, AvailabilityMonth);

            
            NotifyMatrixChanged();
        }
    }
}
