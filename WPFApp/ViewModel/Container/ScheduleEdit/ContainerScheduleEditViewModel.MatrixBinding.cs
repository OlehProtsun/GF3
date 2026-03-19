/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.MatrixBinding у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Data;
using System.Globalization;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        
        
        
        
        
        
        
        
        
        private static bool IsTechnicalMatrixColumn(string columnName)
        {
            return columnName == DayColumnName
                || columnName == ConflictColumnName
                || columnName == WeekendColumnName;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
        {
            
            
            cellRef = default;

            
            if (SelectedBlock is null)
                return false;

            
            
            
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            if (IsTechnicalMatrixColumn(columnName))
                return false;

            
            
            
            if (rowData is not DataRowView rowView)
                return false;

            
            
            var dayObj = rowView[DayColumnName];

            
            if (dayObj is null || dayObj == DBNull.Value)
                return false;

            
            
            int day;
            try
            {
                day = Convert.ToInt32(dayObj, CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }

            
            
            if (day <= 0)
                return false;

            
            
            
            if (!_colNameToEmpId.TryGetValue(columnName, out var employeeId))
                return false;

            
            
            
            
            cellRef = new ScheduleMatrixCellRef(day, employeeId, columnName);

            return true;
        }
    }
}
