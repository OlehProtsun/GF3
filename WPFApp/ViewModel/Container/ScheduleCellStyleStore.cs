using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer.Models;

namespace WPFApp.ViewModel.Container
{
    public sealed class ScheduleCellStyleStore
    {
        private readonly Dictionary<(int day, int employeeId), ScheduleCellStyleModel> _map = new();

        public void Load(IEnumerable<ScheduleCellStyleModel> styles)
        {
            _map.Clear();
            foreach (var style in styles)
            {
                _map[(style.DayOfMonth, style.EmployeeId)] = style;
            }
        }

        public bool TryGetStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _map.TryGetValue((cellRef.DayOfMonth, cellRef.EmployeeId), out style!);

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

        public int RemoveAll(ICollection<ScheduleCellStyleModel> storage)
        {
            var count = storage.Count;
            storage.Clear();
            _map.Clear();
            return count;
        }

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
