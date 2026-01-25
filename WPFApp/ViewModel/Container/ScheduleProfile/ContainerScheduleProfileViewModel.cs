using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.ScheduleMatrix;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.ScheduleProfile
{
    /// <summary>
    /// ContainerScheduleProfileViewModel — ViewModel екрану “профіль розкладу” (read-only/preview).
    ///
    /// Що цей VM робить:
    /// 1) Показує базову інформацію по schedule (Id, Name, Month/Year, ShopName, Note).
    /// 2) Показує матрицю (DataView), збудовану через ScheduleMatrixEngine.
    /// 3) Показує totals:
    ///    - TotalEmployees
    ///    - TotalHoursText
    ///    - tooltip “Total hours …” по кожній колонці працівника.
    /// 4) Дає стилі клітинок (фон/текст) для WPF DataGrid через IScheduleMatrixStyleProvider.
    ///
    /// Чому ми НЕ робимо тут partial-файли:
    /// - файл відносно короткий
    /// - логіка зосереджена навколо одного сценарію (побудувати і показати профіль)
    ///
    /// Але ми приводимо його до “інфраструктурного” стилю:
    /// - константи колонок беремо з ScheduleMatrixConstants
    /// - totals рахуємо через ScheduleTotalsCalculator
    /// - brush робимо з кешем і Freeze() для продуктивності WPF
    /// - не мутимо UI-дані з background thread
    /// </summary>
    public sealed class ContainerScheduleProfileViewModel : ViewModelBase, IScheduleMatrixStyleProvider
    {
        // =========================================================
        // 1) ЗАЛЕЖНОСТІ (OWNER) ТА ВНУТРІШНІ СТРУКТУРИ
        // =========================================================

        /// <summary>
        /// Owner (ContainerViewModel) — керує навігацією/діалогами/запитами.
        /// Тут профільний VM лише викликає:
        /// - RunOnUiThreadAsync(...)
        /// - CancelScheduleAsync()
        /// - EditSelectedScheduleAsync()
        /// - DeleteSelectedScheduleAsync()
        /// </summary>
        private readonly ContainerViewModel _owner;

        /// <summary>
        /// Map: columnName -> employeeId.
        /// Її повертає ScheduleMatrixEngine.BuildScheduleTable.
        ///
        /// Навіщо:
        /// - коли UI каже “користувач навівся на колонку emp_12”,
        ///   ми можемо знайти employeeId=12 і показати totals/tooltip саме для цього працівника.
        /// </summary>
        private readonly Dictionary<string, int> _colNameToEmpId = new();

        /// <summary>
        /// Store стилів клітинок: швидкий доступ (day, employeeId) -> style.
        /// Ми завантажуємо сюди список cellStyles для поточного профілю.
        /// </summary>
        private readonly ScheduleCellStyleStore _cellStyleStore = new();

        /// <summary>
        /// Тексти totals для кожного працівника (EmployeeId -> "Total hours: Xh Ym").
        /// Важливо: цей словник читається UI (tooltip), тому ми оновлюємо його ТІЛЬКИ на UI-thread.
        /// </summary>
        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        /// <summary>
        /// Кеш brushes по ARGB, щоб WPF не створював новий SolidColorBrush кожен раз.
        /// Це важливо, бо DataGrid може часто перерендерити клітинки.
        /// </summary>
        private readonly Dictionary<int, Brush> _brushCache = new();

        /// <summary>
        /// CTS для побудови матриці.
        /// Якщо користувач швидко перемикає профілі — попередній build скасовуємо.
        /// </summary>
        private CancellationTokenSource? _matrixCts;

        /// <summary>
        /// Версія побудови.
        /// Захист від “stale result”: якщо старий Task завершиться після нового,
        /// ми його результат не застосовуємо.
        /// </summary>
        private int _matrixVersion;

        // =========================================================
        // 2) ПРОСТІ ПОЛЯ ПРОФІЛЮ (Показуються в UI)
        // =========================================================

        private int _scheduleId;
        public int ScheduleId
        {
            get => _scheduleId;
            private set => SetProperty(ref _scheduleId, value);
        }

        private string _scheduleName = string.Empty;
        public string ScheduleName
        {
            get => _scheduleName;
            private set => SetProperty(ref _scheduleName, value);
        }

        private string _scheduleMonthYear = string.Empty;
        public string ScheduleMonthYear
        {
            get => _scheduleMonthYear;
            private set => SetProperty(ref _scheduleMonthYear, value);
        }

        private string _shopName = string.Empty;
        public string ShopName
        {
            get => _shopName;
            private set => SetProperty(ref _shopName, value);
        }

        private string _note = string.Empty;
        public string Note
        {
            get => _note;
            private set => SetProperty(ref _note, value);
        }

        // =========================================================
        // 3) МАТРИЦЯ + СТИЛІ (DataGrid)
        // =========================================================

        private DataView _scheduleMatrix = new DataView();
        public DataView ScheduleMatrix
        {
            get => _scheduleMatrix;
            private set => SetProperty(ref _scheduleMatrix, value);
        }

        private int _cellStyleRevision;
        public int CellStyleRevision
        {
            get => _cellStyleRevision;
            private set => SetProperty(ref _cellStyleRevision, value);
        }

        // =========================================================
        // 4) TOTALS (Показуються в UI)
        // =========================================================

        private int _totalEmployees;
        public int TotalEmployees
        {
            get => _totalEmployees;
            private set => SetProperty(ref _totalEmployees, value);
        }

        private string _totalHoursText = "0h 0m";
        public string TotalHoursText
        {
            get => _totalHoursText;
            private set => SetProperty(ref _totalHoursText, value);
        }

        /// <summary>
        /// Employees — список працівників у цьому schedule.
        /// Це може бути використано в UI (наприклад, для списку під матрицею).
        /// </summary>
        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();

        // =========================================================
        // 5) КОМАНДИ (навігація/дії)
        // =========================================================

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        /// <summary>
        /// MatrixChanged — подія, щоб UI/слухачі знали:
        /// “матриця або totals/стилі оновилися”.
        /// </summary>
        public event EventHandler? MatrixChanged;

        public ContainerScheduleProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            // У твоєму UI Back і CancelProfile роблять однакову дію — закрити/повернутись.
            BackCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());

            // Edit/Delete делегуються owner’у (owner знає який schedule зараз selected).
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync());
        }

        // =========================================================
        // 6) ГОЛОВНИЙ МЕТОД: ЗАВАНТАЖИТИ ПРОФІЛЬ + ПОБУДУВАТИ МАТРИЦЮ
        // =========================================================

        /// <summary>
        /// SetProfileAsync — встановлює новий профіль для показу.
        ///
        /// Вхід:
        /// - schedule: модель розкладу (id, name, year, month, shop, note...)
        /// - employees: список працівників для цього schedule
        /// - slots: список слотів
        /// - cellStyles: стилі клітинок (фон/текст)
        ///
        /// Що робимо:
        /// 1) Скасовуємо попередній build (якщо користувач швидко перемикає профілі).
        /// 2) На UI thread швидко оновлюємо прості поля (тексти, список employees).
        /// 3) У background будуємо DataTable матриці (дорога операція).
        /// 4) У background рахуємо totals (так само дорога операція).
        /// 5) На UI thread застосовуємо:
        ///    - ScheduleMatrix
        ///    - _colNameToEmpId
        ///    - _cellStyleStore + CellStyleRevision
        ///    - TotalEmployees/TotalHoursText
        ///    - _employeeTotalHoursText (tooltip по працівниках)
        /// </summary>
        public async Task SetProfileAsync(
            ScheduleModel schedule,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel> cellStyles,
            CancellationToken ct = default)
        {
            // ---------- 1) Скасування попереднього білду ----------
            _matrixCts?.Cancel();
            _matrixCts?.Dispose();

            var localCts = _matrixCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = localCts.Token;

            // “id” цього білду (для stale guard)
            var version = ++_matrixVersion;

            // ---------- 2) Швидке оновлення простих полів на UI thread ----------
            await _owner.RunOnUiThreadAsync(() =>
            {
                ScheduleId = schedule.Id;
                ScheduleName = schedule.Name ?? string.Empty;
                ScheduleMonthYear = $"{schedule.Month:D2}.{schedule.Year}";
                ShopName = schedule.Shop?.Name ?? string.Empty;
                Note = schedule.Note ?? string.Empty;

                Employees.Clear();
                foreach (var emp in employees)
                    Employees.Add(emp);

                // Поки будуємо матрицю — очищаємо її (щоб UI не показував старий профіль)
                ScheduleMatrix = new DataView();

                // Також очищаємо totals, щоб не миготіли старі значення
                TotalEmployees = 0;
                TotalHoursText = "0h 0m";
                _employeeTotalHoursText.Clear();

                MatrixChanged?.Invoke(this, EventArgs.Empty);
            }).ConfigureAwait(false);

            try
            {
                // ---------- 3) Snapshot ----------
                // Snapshot важливий, якщо вхідні списки:
                // - ObservableCollection
                // - EF proxy
                // - або можуть змінитися зовні під час побудови
                var year = schedule.Year;
                var month = schedule.Month;

                var slotsSnapshot = slots.ToList();
                var employeesSnapshot = employees.ToList();
                var stylesSnapshot = cellStyles.ToList();

                // ---------- 4) Важка побудова OFF UI thread ----------
                var built = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    // 4.1) Будуємо DataTable матриці
                    var table = ScheduleMatrixEngine.BuildScheduleTable(
                        year,
                        month,
                        slotsSnapshot,
                        employeesSnapshot,
                        out var colMap,
                        token);

                    token.ThrowIfCancellationRequested();

                    // 4.2) Рахуємо totals через винесений “engine”
                    var totals = ScheduleTotalsCalculator.Calculate(employeesSnapshot, slotsSnapshot);

                    // 4.3) Формуємо тексти totals для tooltip по працівниках
                    // Важливо: тут ми НЕ пишемо у поле _employeeTotalHoursText (бо це буде background thread),
                    // а повертаємо готовий Dictionary і застосуємо його на UI thread.
                    var perEmployeeText = new Dictionary<int, string>(capacity: Math.Max(8, totals.TotalEmployees));

                    foreach (var emp in employeesSnapshot)
                    {
                        var empId = emp.EmployeeId;

                        totals.PerEmployeeDuration.TryGetValue(empId, out var empTotal);
                        perEmployeeText[empId] =
                            $"Total hours: {ScheduleTotalsCalculator.FormatHoursMinutes(empTotal)}";
                    }

                    var totalHoursText = ScheduleTotalsCalculator.FormatHoursMinutes(totals.TotalDuration);

                    return (View: table.DefaultView,
                            ColMap: colMap,
                            Styles: stylesSnapshot,
                            TotalEmployees: totals.TotalEmployees,
                            TotalHoursText: totalHoursText,
                            PerEmployeeText: perEmployeeText);

                }, token).ConfigureAwait(false);

                // stale guard: якщо за цей час уже прийшов новий профіль — цей результат не актуальний
                if (token.IsCancellationRequested || version != _matrixVersion)
                    return;

                // ---------- 5) Застосування результатів на UI thread ----------
                await _owner.RunOnUiThreadAsync(() =>
                {
                    // stale guard ще раз (бо між await і UI могло пройти трохи часу)
                    if (token.IsCancellationRequested || version != _matrixVersion)
                        return;

                    // 5.1) Ставимо матрицю
                    ScheduleMatrix = built.View;

                    // 5.2) Оновлюємо мапи стилів та колонок
                    RebuildStyleMaps(built.ColMap, built.Styles);

                    // 5.3) Totals
                    TotalEmployees = built.TotalEmployees;
                    TotalHoursText = built.TotalHoursText;

                    // 5.4) Tooltip тексти
                    _employeeTotalHoursText.Clear();
                    foreach (var kv in built.PerEmployeeText)
                        _employeeTotalHoursText[kv.Key] = kv.Value;

                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Нормально: користувач переключив профіль, попередній build скасовано
            }
        }

        /// <summary>
        /// Скасувати всі background задачі (коли виходимо з профілю/закриваємо форму).
        /// </summary>
        internal void CancelBackgroundWork()
        {
            _matrixCts?.Cancel();
            _matrixCts?.Dispose();
            _matrixCts = null;
        }

        // =========================================================
        // 7) TOOLTIP TOTAL HOURS ПО КОЛОНЦІ ПРАЦІВНИКА
        // =========================================================

        /// <summary>
        /// Повертає текст “Total hours …” для колонки DataGrid.
        ///
        /// Як працює:
        /// - columnName (наприклад "emp_12") шукаємо в _colNameToEmpId
        /// - якщо знайшли employeeId — беремо готовий текст з _employeeTotalHoursText
        ///
        /// Важливо:
        /// - Для технічних колонок (Day/Conflict/Weekend) мапи не буде => повернемо "".
        /// </summary>
        public string GetEmployeeTotalHoursText(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            if (!_colNameToEmpId.TryGetValue(columnName, out var empId))
                return string.Empty;

            return _employeeTotalHoursText.TryGetValue(empId, out var text)
                ? text
                : "Total hours: 0h 0m";
        }

        // =========================================================
        // 8) IScheduleMatrixStyleProvider: binding/стилі клітинок
        // =========================================================

        /// <summary>
        /// Перевіряє, чи колонка технічна (не працівник).
        /// Це допоміжна функція для TryBuildCellReference.
        /// </summary>
        private static bool IsTechnicalMatrixColumn(string columnName)
        {
            return columnName == ScheduleMatrixConstants.DayColumnName
                || columnName == ScheduleMatrixConstants.ConflictColumnName
                || columnName == ScheduleMatrixConstants.WeekendColumnName;
        }

        /// <summary>
        /// Побудувати ScheduleMatrixCellRef з rowData (DataRowView) + columnName.
        ///
        /// Повертає false, якщо:
        /// - columnName порожній або технічний
        /// - rowData не DataRowView
        /// - у рядку немає валідного DayOfMonth
        /// - columnName не є колонкою працівника (нема в _colNameToEmpId)
        /// </summary>
        public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
        {
            cellRef = default;

            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            if (IsTechnicalMatrixColumn(columnName))
                return false;

            if (rowData is not DataRowView rowView)
                return false;

            // dayObj — значення з колонки DayOfMonth у DataTable
            var dayObj = rowView[ScheduleMatrixConstants.DayColumnName];
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

        /// <summary>
        /// Повертає Brush для фону клітинки (Background).
        /// null => “використовуй стандартний стиль”.
        /// </summary>
        public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)
        {
            if (!TryGetCellStyle(cellRef, out var style))
                return null;

            // 0 або null — означає “не задано”
            if (style.BackgroundColorArgb is not int argb || argb == 0)
                return null;

            return ToBrushCached(argb);
        }

        /// <summary>
        /// Повертає Brush для тексту клітинки (Foreground).
        /// </summary>
        public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)
        {
            if (!TryGetCellStyle(cellRef, out var style))
                return null;

            if (style.TextColorArgb is not int argb || argb == 0)
                return null;

            return ToBrushCached(argb);
        }

        /// <summary>
        /// Дати сирий ScheduleCellStyleModel, якщо він існує у store.
        /// </summary>
        public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _cellStyleStore.TryGetStyle(cellRef, out style);

        // =========================================================
        // 9) ВНУТРІШНІ HELPERS: стиль/brush кеш
        // =========================================================

        /// <summary>
        /// Оновити:
        /// - _colNameToEmpId (колонка -> employeeId)
        /// - _cellStyleStore (day,employee -> style)
        /// - підняти CellStyleRevision, щоб UI знав що стилі змінились
        /// </summary>
        private void RebuildStyleMaps(Dictionary<string, int> colMap, IList<ScheduleCellStyleModel> cellStyles)
        {
            _colNameToEmpId.Clear();
            foreach (var pair in colMap)
                _colNameToEmpId[pair.Key] = pair.Value;

            _cellStyleStore.Load(cellStyles);

            // Ревізія потрібна для WPF (часто стилі оновлюють через тригер на число)
            CellStyleRevision++;
        }

        /// <summary>
        /// Конвертація ARGB(int) -> Brush з кешем і Freeze().
        ///
        /// Чому так:
        /// - DataGrid може дуже часто питати brush при скролі/перерендері.
        /// - Якщо кожного разу робити new SolidColorBrush — буде багато GC і лаги.
        /// - Freeze() робить brush “незмінним” і швидшим для WPF.
        /// </summary>
        private Brush ToBrushCached(int argb)
        {
            if (_brushCache.TryGetValue(argb, out var b))
                return b;

            b = ColorHelpers.ToBrush(argb);

            // Freezable — базовий тип для brush у WPF.
            // Freeze робить об’єкт immutable і дозволяє WPF ефективніше його використовувати.
            if (b is Freezable f && f.CanFreeze)
                f.Freeze();

            _brushCache[argb] = b;
            return b;
        }
    }
}
