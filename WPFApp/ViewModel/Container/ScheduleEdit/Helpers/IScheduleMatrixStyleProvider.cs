/*
  Опис файлу: цей модуль містить реалізацію компонента IScheduleMatrixStyleProvider у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System.Windows.Media;

namespace WPFApp.ViewModel.Container.ScheduleEdit.Helpers
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public interface IScheduleMatrixStyleProvider` та контракт його використання у шарі WPFApp.
    /// </summary>
    public interface IScheduleMatrixStyleProvider
    {
        
        
        
        
        
        
        
        int CellStyleRevision { get; }

        
        
        
        
        
        
        
        
        
        
        bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef);

        
        
        
        
        Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef);

        
        
        
        
        Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef);

        
        
        
        
        
        
        
        bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style);
    }
}
