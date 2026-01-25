using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WPFApp.Infrastructure;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Частина ViewModel, яка відповідає ТІЛЬКИ за:
    /// - стилі клітинок матриці (фон/текст)
    /// - “режим фарбування” (PaintMode)
    /// - кеш Brushes (щоб WPF не створював 1000 разів однакові пензлі)
    ///
    /// Навіщо винесено:
    /// - код про UI-стилі великий
    /// - він не має змішуватись з матрицею/валидацією/довідниками
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        // =========================
        // 1) Отримання brush для клітинок
        // =========================

        /// <summary>
        /// Повертає Brush для фону конкретної клітинки.
        ///
        /// Логіка:
        /// 1) Знаходимо стиль клітинки (ScheduleCellStyleModel) у _cellStyleStore
        /// 2) Якщо в ньому є BackgroundColorArgb != 0 => конвертуємо ARGB у Brush
        /// 3) Повертаємо null, якщо стилю немає або колір не заданий
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
        /// Повертає Brush для тексту (foreground) конкретної клітинки.
        /// Аналогічно до GetCellBackgroundBrush, тільки для TextColorArgb.
        /// </summary>
        public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)
        {
            return TryGetCellStyle(cellRef, out var style)
                   && style.TextColorArgb is int argb
                   && argb != 0
                ? ToBrushCached(argb)
                : null;
        }

        /// <summary>
        /// Конвертація ARGB(int) => Brush з кешуванням.
        ///
        /// Чому кеш важливий:
        /// - WPF може часто запитувати Brush для клітинок (скрол, перерендер).
        /// - Якщо кожен раз створювати новий SolidColorBrush — буде багато алокацій і лаги.
        ///
        /// Як працює:
        /// - _brushCache[argb] => повертаємо готовий Brush
        /// - якщо нема => створюємо, Freeze() (для швидкості WPF), кладемо в кеш
        /// </summary>
        private Brush ToBrushCached(int argb)
        {
            if (_brushCache.TryGetValue(argb, out var b))
                return b;

            b = ColorHelpers.ToBrush(argb);

            // Freeze — WPF perf trick: “заморожений” brush можна шарити безпеечно і швидко
            if (b is Freezable f && f.CanFreeze) f.Freeze();

            _brushCache[argb] = b;
            return b;
        }


        // =========================
        // 2) Робота зі store стилів (читання/оновлення)
        // =========================

        /// <summary>
        /// Спробувати отримати стиль клітинки зі store.
        /// Store тримає map: (day, employee) => style.
        /// </summary>
        public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _cellStyleStore.TryGetStyle(cellRef, out style);

        /// <summary>
        /// Коли стилі в SelectedBlock.CellStyles змінюються, ми “перезаливаємо” store
        /// і піднімаємо ревізію, щоб UI оновився.
        /// </summary>
        private void RefreshCellStyleMap()
        {
            _cellStyleStore.Load(SelectedBlock?.CellStyles?.ToArray() ?? Array.Empty<ScheduleCellStyleModel>());
            CellStyleRevision++;
        }

        /// <summary>
        /// Видалити всі стилі, які стосуються конкретного працівника.
        /// Використовується, коли працівника прибрали зі schedule.
        /// </summary>
        internal void RemoveCellStylesForEmployee(int employeeId)
        {
            if (SelectedBlock is null)
                return;

            if (_cellStyleStore.RemoveByEmployee(employeeId, SelectedBlock.CellStyles) > 0)
                RefreshCellStyleMap();
        }


        // =========================
        // 3) “Paint mode”: застосування останнього вибраного кольору
        // =========================

        /// <summary>
        /// Викликається, коли користувач “малює” по клітинках (наприклад клікнув клітинку).
        ///
        /// Ідея:
        /// - ActivePaintMode каже, що саме ми “фарбуємо”: фон чи текст.
        /// - LastFillColorArgb / LastTextColorArgb — останній обраний колір.
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


        // =========================
        // 4) Вибір кольору (через color picker) і застосування
        // =========================

        /// <summary>
        /// Встановити фон клітинки(клітинок).
        ///
        /// Логіка:
        /// 1) Визначаємо initial колір для color picker:
        ///    - якщо є LastFillColorArgb => беремо його
        ///    - якщо конкретна клітинка має власний background => краще показати її колір
        /// 2) Викликаємо owner.TryPickScheduleCellColor(...) — відкриває діалог
        /// 3) Якщо користувач вибрав колір:
        ///    - зберігаємо його як LastFillColorArgb
        ///    - ставимо ActivePaintMode = Background
        ///    - застосовуємо до цільових клітинок (або SelectedCellRefs, або fallback)
        /// </summary>
        private void SetCellBackgroundColor(ScheduleMatrixCellRef? cellRef)
        {
            if (SelectedBlock is null)
                return;

            // 1) initial
            var initial = LastFillColorArgb.HasValue
                ? ColorHelpers.FromArgb(LastFillColorArgb.Value)
                : (Color?)null;

            // якщо клітинка задана і вона має стиль — initial = її колір
            if (cellRef.HasValue)
            {
                var existing = GetOrCreateCellStyle(cellRef.Value);
                if (existing.BackgroundColorArgb.HasValue)
                    initial = ColorHelpers.FromArgb(existing.BackgroundColorArgb.Value);
            }

            // 2) picker
            if (!_owner.TryPickScheduleCellColor(initial, out var selected))
                return;

            // 3) apply
            LastFillColorArgb = ColorHelpers.ToArgb(selected);
            ActivePaintMode = SchedulePaintMode.Background;

            ApplyCellBackgroundColor(GetTargetCells(cellRef), LastFillColorArgb.Value);
        }

        /// <summary>
        /// Аналогічно SetCellBackgroundColor, тільки для кольору тексту.
        /// </summary>
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

        /// <summary>
        /// Застосувати “останній колір фону” без відкриття picker’а.
        /// </summary>
        private void ApplyLastFillColor(ScheduleMatrixCellRef? cellRef)
        {
            if (!LastFillColorArgb.HasValue)
                return;

            ActivePaintMode = SchedulePaintMode.Background;
            ApplyCellBackgroundColor(GetTargetCells(cellRef), LastFillColorArgb.Value);
        }

        /// <summary>
        /// Застосувати “останній колір тексту” без відкриття picker’а.
        /// </summary>
        private void ApplyLastTextColor(ScheduleMatrixCellRef? cellRef)
        {
            if (!LastTextColorArgb.HasValue)
                return;

            ActivePaintMode = SchedulePaintMode.Foreground;
            ApplyCellTextColor(GetTargetCells(cellRef), LastTextColorArgb.Value);
        }

        private void PickFillColor() => SetCellBackgroundColor(null);
        private void PickTextColor() => SetCellTextColor(null);


        // =========================
        // 5) Очищення форматування
        // =========================

        private void ClearCellFormatting(ScheduleMatrixCellRef? cellRef)
            => ClearSelectedCellStyles(cellRef);

        /// <summary>
        /// Очистити стилі для:
        /// - або SelectedCellRefs (якщо користувач виділив багато клітинок)
        /// - або fallback клітинки (якщо виділення пусте)
        /// </summary>
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

        /// <summary>
        /// Очистити ВСІ стилі в поточному блоці.
        /// </summary>
        private void ClearAllScheduleStyles()
        {
            if (SelectedBlock is null)
                return;

            if (_cellStyleStore.RemoveAll(SelectedBlock.CellStyles) > 0)
                CellStyleRevision++;
        }


        // =========================
        // 6) Створення/отримання style-об’єкта + застосування кольорів
        // =========================

        /// <summary>
        /// Отримати або створити ScheduleCellStyleModel для конкретної клітинки.
        ///
        /// Де зберігаються стилі:
        /// - у SelectedBlock.CellStyles (це колекція, яка зберігається разом зі schedule)
        /// - _cellStyleStore — це кеш/індекс для швидкого пошуку
        ///
        /// Якщо стилю ще немає — створюємо новий об’єкт, додаємо в CellStyles,
        /// і store почне його “бачити”.
        /// </summary>
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

        /// <summary>
        /// Застосувати фон (BackgroundColorArgb) до набору клітинок.
        /// </summary>
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

        /// <summary>
        /// Застосувати колір тексту (TextColorArgb) до набору клітинок.
        /// </summary>
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


        // =========================
        // 7) Визначення “цільових клітинок” для команд
        // =========================

        /// <summary>
        /// Правило вибору цільових клітинок:
        /// 1) Якщо користувач виділив декілька клітинок (SelectedCellRefs.Count > 0)
        ///    => застосовуємо до них.
        /// 2) Інакше:
        ///    - якщо є fallback клітинка (клікнули конкретну)
        ///      => застосовуємо до неї
        ///    - якщо нема => порожній список
        ///
        /// Це зручно, бо всі команди фарбування/очистки використовують одну логіку.
        /// </summary>
        private IReadOnlyCollection<ScheduleMatrixCellRef> GetTargetCells(ScheduleMatrixCellRef? fallback)
        {
            if (SelectedCellRefs.Count > 0)
                return SelectedCellRefs; // без алокацій

            return fallback.HasValue
                ? new[] { fallback.Value }
                : Array.Empty<ScheduleMatrixCellRef>();
        }

        /// <summary>
        /// Оновити SelectedCellRefs (виділення клітинок) з зовнішнього коду (UI).
        /// Distinct() прибирає дублікати, якщо вони прилетіли.
        /// </summary>
        public void UpdateSelectedCellRefs(IEnumerable<ScheduleMatrixCellRef> cellRefs)
        {
            SelectedCellRefs.Clear();
            foreach (var cellRef in cellRefs.Distinct())
                SelectedCellRefs.Add(cellRef);
        }
    }
}
