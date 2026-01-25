using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer.Models;

namespace WPFApp.ViewModel.Container.ScheduleEdit.Helpers
{
    /// <summary>
    /// ScheduleCellStyleStore — швидкий кеш/індекс стилів клітинок матриці.
    ///
    /// ПРОБЛЕМА, яку вирішує:
    /// - Стилі клітинок зберігаються як список/колекція:
    ///     SelectedBlock.CellStyles (ObservableCollection<ScheduleCellStyleModel>)
    /// - Щоб знайти стиль для конкретної клітинки (day+employee),
    ///   без кешу довелося б робити пошук по списку кожного разу (O(n)).
    ///
    /// РІШЕННЯ:
    /// - Ми будуємо словник:
    ///     _map[(day, employeeId)] = style
    /// - Отримання стилю стає O(1).
    ///
    /// Важливо:
    /// - Store НЕ є “джерелом правди”.
    /// - Джерело правди — storage (наприклад SelectedBlock.CellStyles).
    /// - Store лише пришвидшує доступ і допомагає з CRUD-операціями.
    /// </summary>
    public sealed class ScheduleCellStyleStore
    {
        /// <summary>
        /// Ключ: (day, employeeId)
        /// Значення: ScheduleCellStyleModel (вміщує BackgroundColorArgb, TextColorArgb тощо)
        /// </summary>
        private readonly Dictionary<(int day, int employeeId), ScheduleCellStyleModel> _map = new();

        /// <summary>
        /// Повністю перезавантажити store з існуючих стилів.
        ///
        /// Коли викликати:
        /// - при зміні SelectedBlock (завантажили новий блок -> його стилі)
        /// - після масових змін, коли простіше “перезібрати” індекс заново
        ///
        /// Що робить:
        /// - очищає словник
        /// - проходить по styles
        /// - додає/перезаписує ключ (day,employee)
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
        /// Спробувати отримати стиль клітинки.
        ///
        /// Повертає:
        /// - true, якщо стиль знайдено
        /// - false, якщо стилю нема (клітинка ще не форматувалась)
        /// </summary>
        public bool TryGetStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _map.TryGetValue((cellRef.DayOfMonth, cellRef.EmployeeId), out style!);

        /// <summary>
        /// Отримати існуючий стиль або створити новий.
        ///
        /// Параметри:
        /// - cellRef: яка клітинка
        /// - factory: як створити новий ScheduleCellStyleModel (зазвичай з ScheduleId, DayOfMonth, EmployeeId)
        /// - storage: колекція, де реально зберігаються стилі (наприклад SelectedBlock.CellStyles)
        ///
        /// Логіка:
        /// 1) якщо є у _map — повертаємо
        /// 2) якщо нема:
        ///    - створюємо style = factory()
        ///    - додаємо style в storage (джерело правди)
        ///    - додаємо style в _map (індекс)
        ///    - повертаємо style
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
        /// Видалити стилі для набору клітинок.
        ///
        /// Вхід:
        /// - cellRefs: список клітинок (може містити дублікати)
        /// - storage: джерело правди, звідки треба видалити реальні об'єкти style
        ///
        /// Повертає:
        /// - скільки стилів було реально видалено
        ///
        /// Важливо:
        /// - .Distinct() прибирає дублікати, щоб не намагатися видалити одне і те саме двічі.
        /// </summary>
        public int RemoveStyles(IEnumerable<ScheduleMatrixCellRef> cellRefs, ICollection<ScheduleCellStyleModel> storage)
        {
            var removed = 0;

            foreach (var cellRef in cellRefs.Distinct())
            {
                if (!_map.TryGetValue((cellRef.DayOfMonth, cellRef.EmployeeId), out var style))
                    continue;

                // 1) видаляємо з джерела правди
                storage.Remove(style);

                // 2) видаляємо з індексу
                _map.Remove((cellRef.DayOfMonth, cellRef.EmployeeId));

                removed++;
            }

            return removed;
        }

        /// <summary>
        /// Очистити всі стилі (повне скидання форматування).
        ///
        /// Повертає:
        /// - кількість стилів, які були видалені (до очищення)
        /// </summary>
        public int RemoveAll(ICollection<ScheduleCellStyleModel> storage)
        {
            var count = storage.Count;
            storage.Clear();
            _map.Clear();
            return count;
        }

        /// <summary>
        /// Видалити всі стилі, що належать певному працівнику.
        ///
        /// Використання:
        /// - якщо працівника видалили зі schedule — його стилі більше не потрібні
        ///
        /// Повертає:
        /// - скільки стилів видалено
        /// </summary>
        public int RemoveByEmployee(int employeeId, ICollection<ScheduleCellStyleModel> storage)
        {
            // 1) Спочатку формуємо список стилів на видалення,
            //    бо під час модифікації _map не можна ітерувати його напряму.
            var toRemove = _map
                .Where(pair => pair.Key.employeeId == employeeId)
                .Select(pair => pair.Value)
                .ToList();

            // 2) Видаляємо зі storage і з мапи
            foreach (var style in toRemove)
            {
                storage.Remove(style);
                _map.Remove((style.DayOfMonth, style.EmployeeId));
            }

            return toRemove.Count;
        }
    }
}
