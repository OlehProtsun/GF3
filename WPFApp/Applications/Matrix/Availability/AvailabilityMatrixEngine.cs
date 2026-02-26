/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityMatrixEngine у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Data;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.Applications.Matrix.Availability
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class AvailabilityMatrixEngine` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class AvailabilityMatrixEngine
    {
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static readonly string DayColumnName = ScheduleMatrixConstants.DayColumnName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly string DayColumnName = ScheduleMatrixConstants.DayColumnName;

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string GetEmployeeColumnName(int employeeId)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string GetEmployeeColumnName(int employeeId)
        {
            
            
            
            
            
            return $"emp_{employeeId}";
        }

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void EnsureDayColumn(DataTable table)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void EnsureDayColumn(DataTable table)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            if (table.Columns.Contains(DayColumnName))
                return;

            
            
            
            var dayColumn = new DataColumn(DayColumnName, typeof(int))
            {
                Caption = "Day",     
                ReadOnly = true      
            };

            
            table.Columns.Add(dayColumn);

            
            
            table.PrimaryKey = new[] { dayColumn };
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void EnsureDayRowsForMonth(DataTable table, int year, int month)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void EnsureDayRowsForMonth(DataTable table, int year, int month)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            
            
            if (year <= 0 || month < 1 || month > 12)
                return;

            
            EnsureDayColumn(table);

            
            int desiredRowCount = DateTime.DaysInMonth(year, month);

            
            int currentRowCount = table.Rows.Count;

            
            if (currentRowCount < desiredRowCount)
            {
                
                for (int day = currentRowCount + 1; day <= desiredRowCount; day++)
                {
                    
                    var row = table.NewRow();

                    
                    row[DayColumnName] = day;

                    
                    
                    foreach (DataColumn col in table.Columns)
                    {
                        
                        if (col.ColumnName == DayColumnName)
                            continue;

                        
                        row[col.ColumnName] = string.Empty;
                    }

                    
                    table.Rows.Add(row);
                }
            }
            
            else if (currentRowCount > desiredRowCount)
            {
                
                for (int i = currentRowCount - 1; i >= desiredRowCount; i--)
                    table.Rows.RemoveAt(i);
            }

            
        }

        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool TryAddEmployeeColumn(DataTable table, int employeeId, string header, out string columnName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool TryAddEmployeeColumn(DataTable table, int employeeId, string header, out string columnName)
        {
            
            columnName = string.Empty;

            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            if (employeeId <= 0)
                return false;

            
            EnsureDayColumn(table);

            
            columnName = GetEmployeeColumnName(employeeId);

            
            if (table.Columns.Contains(columnName))
                return false;

            
            
            
            
            var col = new DataColumn(columnName, typeof(string))
            {
                Caption = header ?? string.Empty,
                DefaultValue = string.Empty
            };

            
            table.Columns.Add(col);

            
            
            foreach (DataRow r in table.Rows)
                r[columnName] = string.Empty;

            
            return true;
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool RemoveEmployeeColumn(DataTable table, string columnName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool RemoveEmployeeColumn(DataTable table, string columnName)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            
            if (columnName == DayColumnName)
                return false;

            
            if (!table.Columns.Contains(columnName))
                return false;

            
            table.Columns.Remove(columnName);

            
            return true;
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void RemoveAllEmployeeColumns(DataTable table)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void RemoveAllEmployeeColumns(DataTable table)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            EnsureDayColumn(table);

            
            
            var toRemove = new List<string>();

            foreach (DataColumn c in table.Columns)
            {
                
                if (c.ColumnName == DayColumnName)
                    continue;

                
                toRemove.Add(c.ColumnName);
            }

            
            for (int i = 0; i < toRemove.Count; i++)
                table.Columns.Remove(toRemove[i]);
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void Reset(DataTable table, bool regenerateDays, int year, int month)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void Reset(DataTable table, bool regenerateDays, int year, int month)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            EnsureDayColumn(table);

            
            RemoveAllEmployeeColumns(table);

            
            table.Rows.Clear();

            
            if (regenerateDays)
                EnsureDayRowsForMonth(table, year, month);
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void SetEmployeeCodes(DataTable table, string employeeColumnName, IEnumerable<(int dayOfMonth, string code` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void SetEmployeeCodes(DataTable table, string employeeColumnName, IEnumerable<(int dayOfMonth, string code)> codes)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            if (string.IsNullOrWhiteSpace(employeeColumnName))
                return;

            
            if (!table.Columns.Contains(employeeColumnName))
                return;

            
            foreach (var (day, raw) in codes)
            {
                
                
                if (day <= 0 || day > table.Rows.Count)
                    continue;

                
                var row = table.Rows[day - 1];

                
                row[employeeColumnName] = raw ?? string.Empty;
            }
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static List<(int dayOfMonth, string code)> ReadEmployeeCodes(DataTable table, string employeeColumnName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static List<(int dayOfMonth, string code)> ReadEmployeeCodes(DataTable table, string employeeColumnName)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            var result = new List<(int dayOfMonth, string code)>();

            
            if (string.IsNullOrWhiteSpace(employeeColumnName) || !table.Columns.Contains(employeeColumnName))
                return result;

            
            int rowCount = table.Rows.Count;

            
            result = new List<(int dayOfMonth, string code)>(capacity: rowCount);

            
            for (int i = 0; i < rowCount; i++)
            {
                
                int day = i + 1;

                
                var code = Convert.ToString(table.Rows[i][employeeColumnName]) ?? string.Empty;

                
                result.Add((day, code));
            }

            return result;
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool TryNormalizeCell(string? raw, out string normalized, out string? error)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool TryNormalizeCell(string? raw, out string normalized, out string? error)
        {
            
            
            
            return AvailabilityCellCodeParser.TryNormalize(raw, out normalized, out error);
        }

        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void NormalizeAndValidateAllCells(DataTable table)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void NormalizeAndValidateAllCells(DataTable table)
        {
            
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            
            foreach (DataRow row in table.Rows)
            {
                
                foreach (DataColumn col in table.Columns)
                {
                    
                    if (col.ColumnName == DayColumnName)
                        continue;

                    
                    var raw = Convert.ToString(row[col]) ?? string.Empty;

                    
                    if (!TryNormalizeCell(raw, out var normalized, out var error))
                    {
                        
                        row.SetColumnError(col, error ?? "Invalid value.");
                        continue;
                    }

                    
                    row.SetColumnError(col, string.Empty);

                    
                    
                    if (!string.Equals(raw, normalized, StringComparison.Ordinal))
                        row[col] = normalized;
                }
            }
        }
    }
}
