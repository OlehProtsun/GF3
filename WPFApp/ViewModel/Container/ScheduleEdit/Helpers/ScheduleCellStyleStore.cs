/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleCellStyleStore у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Contracts.Models;

namespace WPFApp.ViewModel.Container.ScheduleEdit.Helpers
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleCellStyleStore` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleCellStyleStore
    {
        
        
        
        
        private readonly Dictionary<(int day, int employeeId), ScheduleCellStyleModel> _map = new();

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void Load(IEnumerable<ScheduleCellStyleModel> styles)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Load(IEnumerable<ScheduleCellStyleModel> styles)
        {
            _map.Clear();

            foreach (var style in styles)
            {
                _map[(style.DayOfMonth, style.EmployeeId)] = style;
            }
        }

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool TryGetStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool TryGetStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _map.TryGetValue((cellRef.DayOfMonth, cellRef.EmployeeId), out style!);

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ScheduleCellStyleModel GetOrCreate(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleCellStyleModel GetOrCreate(
            ScheduleMatrixCellRef cellRef,
            Func<ScheduleCellStyleModel> factory,
            ICollection<ScheduleCellStyleModel> storage)
        {
            if (_map.TryGetValue((cellRef.DayOfMonth, cellRef.EmployeeId), out var existing))
                return existing;

            var style = factory();
            storage.Add(style);
            _map[(cellRef.DayOfMonth, cellRef.EmployeeId)] = style;
            return style;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public int RemoveStyles(IEnumerable<ScheduleMatrixCellRef> cellRefs, ICollection<ScheduleCellStyleModel> storage)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int RemoveStyles(IEnumerable<ScheduleMatrixCellRef> cellRefs, ICollection<ScheduleCellStyleModel> storage)
        {
            var removed = 0;

            foreach (var cellRef in cellRefs.Distinct())
            {
                if (!_map.TryGetValue((cellRef.DayOfMonth, cellRef.EmployeeId), out var style))
                    continue;

                
                storage.Remove(style);

                
                _map.Remove((cellRef.DayOfMonth, cellRef.EmployeeId));

                removed++;
            }

            return removed;
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public int RemoveAll(ICollection<ScheduleCellStyleModel> storage)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int RemoveAll(ICollection<ScheduleCellStyleModel> storage)
        {
            var count = storage.Count;
            storage.Clear();
            _map.Clear();
            return count;
        }

        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public int RemoveByEmployee(int employeeId, ICollection<ScheduleCellStyleModel> storage)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int RemoveByEmployee(int employeeId, ICollection<ScheduleCellStyleModel> storage)
        {
            
            
            var toRemove = _map
                .Where(pair => pair.Key.employeeId == employeeId)
                .Select(pair => pair.Value)
                .ToList();

            
            foreach (var style in toRemove)
            {
                storage.Remove(style);
                _map.Remove((style.DayOfMonth, style.EmployeeId));
            }

            return toRemove.Count;
        }
    }
}
