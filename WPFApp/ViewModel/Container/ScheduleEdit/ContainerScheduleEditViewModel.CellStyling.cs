/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.CellStyling у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WPFApp.UI.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        

        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)
        {
            return TryGetCellStyle(cellRef, out var style)
                   && style.BackgroundColorArgb is int argb
                   && argb != 0
                ? ToBrushCached(argb)
                : null;
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)
        {
            return TryGetCellStyle(cellRef, out var style)
                   && style.TextColorArgb is int argb
                   && argb != 0
                ? ToBrushCached(argb)
                : null;
        }

        
        
        
        
        
        
        
        
        
        
        
        private Brush ToBrushCached(int argb)
        {
            if (_brushCache.TryGetValue(argb, out var b))
                return b;

            b = ColorHelpers.ToBrush(argb);

            
            if (b is Freezable f && f.CanFreeze) f.Freeze();

            _brushCache[argb] = b;
            return b;
        }


        
        
        

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _cellStyleStore.TryGetStyle(cellRef, out style);

        
        
        
        
        private void RefreshCellStyleMap()
        {
            _cellStyleStore.Load(SelectedBlock?.CellStyles?.ToArray() ?? Array.Empty<ScheduleCellStyleModel>());
            CellStyleRevision++;
        }

        
        
        
        
        internal void RemoveCellStylesForEmployee(int employeeId)
        {
            if (SelectedBlock is null)
                return;

            if (_cellStyleStore.RemoveByEmployee(employeeId, SelectedBlock.CellStyles) > 0)
                RefreshCellStyleMap();
        }


        
        
        

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void ApplyPaintToCell(ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ApplyPaintToCell(ScheduleMatrixCellRef cellRef)
        {
            if (ActivePaintMode == SchedulePaintMode.Background && LastFillColorArgb.HasValue)
            {
                ApplyCellBackgroundColor(new[] { cellRef }, LastFillColorArgb.Value);
            }
            else if (ActivePaintMode == SchedulePaintMode.Foreground && LastTextColorArgb.HasValue)
            {
                ApplyCellTextColor(new[] { cellRef }, LastTextColorArgb.Value);
            }
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        private void SetCellBackgroundColor(ScheduleMatrixCellRef? cellRef)
        {
            if (SelectedBlock is null)
                return;

            
            var initial = LastFillColorArgb.HasValue
                ? ColorHelpers.FromArgb(LastFillColorArgb.Value)
                : (Color?)null;

            
            if (cellRef.HasValue)
            {
                var existing = GetOrCreateCellStyle(cellRef.Value);
                if (existing.BackgroundColorArgb.HasValue)
                    initial = ColorHelpers.FromArgb(existing.BackgroundColorArgb.Value);
            }

            
            if (!_owner.TryPickScheduleCellColor(initial, out var selected))
                return;

            
            LastFillColorArgb = ColorHelpers.ToArgb(selected);
            ActivePaintMode = SchedulePaintMode.Background;

            ApplyCellBackgroundColor(GetTargetCells(cellRef), LastFillColorArgb.Value);
        }

        
        
        
        private void SetCellTextColor(ScheduleMatrixCellRef? cellRef)
        {
            if (SelectedBlock is null)
                return;

            var initial = LastTextColorArgb.HasValue
                ? ColorHelpers.FromArgb(LastTextColorArgb.Value)
                : (Color?)null;

            if (cellRef.HasValue)
            {
                var existing = GetOrCreateCellStyle(cellRef.Value);
                if (existing.TextColorArgb.HasValue)
                    initial = ColorHelpers.FromArgb(existing.TextColorArgb.Value);
            }

            if (!_owner.TryPickScheduleCellColor(initial, out var selected))
                return;

            LastTextColorArgb = ColorHelpers.ToArgb(selected);
            ActivePaintMode = SchedulePaintMode.Foreground;

            ApplyCellTextColor(GetTargetCells(cellRef), LastTextColorArgb.Value);
        }

        
        
        
        private void ApplyLastFillColor(ScheduleMatrixCellRef? cellRef)
        {
            if (!LastFillColorArgb.HasValue)
                return;

            ActivePaintMode = SchedulePaintMode.Background;
            ApplyCellBackgroundColor(GetTargetCells(cellRef), LastFillColorArgb.Value);
        }

        
        
        
        private void ApplyLastTextColor(ScheduleMatrixCellRef? cellRef)
        {
            if (!LastTextColorArgb.HasValue)
                return;

            ActivePaintMode = SchedulePaintMode.Foreground;
            ApplyCellTextColor(GetTargetCells(cellRef), LastTextColorArgb.Value);
        }

        private void PickFillColor() => SetCellBackgroundColor(null);
        private void PickTextColor() => SetCellTextColor(null);


        
        
        

        private void ClearCellFormatting(ScheduleMatrixCellRef? cellRef)
            => ClearSelectedCellStyles(cellRef);

        
        
        
        
        
        private void ClearSelectedCellStyles(ScheduleMatrixCellRef? cellRef)
        {
            if (SelectedBlock is null)
                return;

            var targets = GetTargetCells(cellRef);
            if (targets.Count == 0)
                return;

            if (_cellStyleStore.RemoveStyles(targets, SelectedBlock.CellStyles) > 0)
                CellStyleRevision++;
        }

        
        
        
        private void ClearAllScheduleStyles()
        {
            if (SelectedBlock is null)
                return;

            if (_cellStyleStore.RemoveAll(SelectedBlock.CellStyles) > 0)
                CellStyleRevision++;
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        private ScheduleCellStyleModel GetOrCreateCellStyle(ScheduleMatrixCellRef cellRef)
        {
            if (SelectedBlock is null)
                throw new InvalidOperationException("No selected schedule block.");

            return _cellStyleStore.GetOrCreate(
                cellRef,
                () => new ScheduleCellStyleModel
                {
                    ScheduleId = SelectedBlock.Model.Id,
                    DayOfMonth = cellRef.DayOfMonth,
                    EmployeeId = cellRef.EmployeeId
                },
                SelectedBlock.CellStyles);
        }

        
        
        
        private void ApplyCellBackgroundColor(IReadOnlyCollection<ScheduleMatrixCellRef> cellRefs, int argb)
        {
            if (SelectedBlock is null || cellRefs.Count == 0)
                return;

            foreach (var cellRef in cellRefs)
            {
                var style = GetOrCreateCellStyle(cellRef);
                style.BackgroundColorArgb = argb;
            }

            CellStyleRevision++;
        }

        
        
        
        private void ApplyCellTextColor(IReadOnlyCollection<ScheduleMatrixCellRef> cellRefs, int argb)
        {
            if (SelectedBlock is null || cellRefs.Count == 0)
                return;

            foreach (var cellRef in cellRefs)
            {
                var style = GetOrCreateCellStyle(cellRef);
                style.TextColorArgb = argb;
            }

            CellStyleRevision++;
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        private IReadOnlyCollection<ScheduleMatrixCellRef> GetTargetCells(ScheduleMatrixCellRef? fallback)
        {
            if (SelectedCellRefs.Count > 0)
                return SelectedCellRefs; 

            return fallback.HasValue
                ? new[] { fallback.Value }
                : Array.Empty<ScheduleMatrixCellRef>();
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void UpdateSelectedCellRefs(IEnumerable<ScheduleMatrixCellRef> cellRefs)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void UpdateSelectedCellRefs(IEnumerable<ScheduleMatrixCellRef> cellRefs)
        {
            SelectedCellRefs.Clear();
            foreach (var cellRef in cellRefs.Distinct())
                SelectedCellRefs.Add(cellRef);
        }
    }
}
