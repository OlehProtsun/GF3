using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// ContainerScheduleProfileViewModel — ViewModel екрану “Schedule Profile” (read-only/preview).
    ///
    /// VM показує:
    /// 1) Базові поля Schedule (Id/Name/MonthYear/Shop/Note + додаткові статистичні поля).
    /// 2) Матрицю розкладу (DataView) у DataGrid.
    /// 3) Totals: TotalEmployees / TotalHoursText + tooltips по кожній колонці employee.
    /// 4) Стилі клітинок матриці через IScheduleMatrixStyleProvider.
    /// 5) “Excel-like Summary” таблицю знизу:
    ///    - Динамічні заголовки днів (Monday(01.01.2026), ...)
    ///    - По рядках: Employee | Sum | (From/To/Hours по кожному дню)
    /// 6) ДОДАНО: Mini-table “Employee | Work Day | Free Day” під summary:
    ///    - EmployeeWorkFreeStats: рахується з SummaryRows (кількість робочих/вільних днів).
    ///
    /// Важливо по потоках:
    /// - Будь-які зміни ObservableCollection / властивостей, що біндяться в UI, робимо ТІЛЬКИ на UI-thread.
    /// - Важку логіку (побудова DataTable, totals, summary) робимо на background thread.
    /// </summary>
    public sealed class ContainerScheduleProfileViewModel : ViewModelBase, IScheduleMatrixStyleProvider
    {
        // =========================================================
        // 0) STATIC / PERF HELPERS
        // =========================================================

        /// <summary>
        /// Компілюємо Regex 1 раз на весь процес:
        /// - Це істотно швидше, ніж Regex.Matches(...) без Compiled у циклі employee * day.
        /// - CultureInvariant: не залежить від локалі.
        /// </summary>
        private static readonly Regex TimeRegex =
            new(@"\b([01]?\d|2[0-3]):[0-5]\d\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Для розбору hours з summary (строки типу: "0", "6", "6h 30m", "12h 0m").
        /// </summary>
        private static readonly Regex SummaryHoursRegex =
            new(@"^\s*(?:(\d+)\s*h)?\s*(?:(\d+)\s*m)?\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // =========================================================
        // 1) ЗАЛЕЖНОСТІ (OWNER) ТА ВНУТРІШНІ СТРУКТУРИ
        // =========================================================

        /// <summary>
        /// Owner (ContainerViewModel) — керує навігацією/діалогами/запитами.
        /// Тут VM тільки делегує команди назад в owner.
        /// </summary>
        private readonly ContainerViewModel _owner;

        /// <summary>
        /// Map: columnName -> employeeId.
        /// Повертається з ScheduleMatrixEngine.BuildScheduleTable(...).
        /// Використовується:
        /// - tooltip по колонці,
        /// - визначення employeeId в TryBuildCellReference.
        /// </summary>
        private readonly Dictionary<string, int> _colNameToEmpId = new();

        /// <summary>
        /// Store стилів клітинок: (day, employeeId) -> style.
        /// </summary>
        private readonly ScheduleCellStyleStore _cellStyleStore = new();

        /// <summary>
        /// Текст totals для кожного працівника (tooltip).
        /// Оновлюється ТІЛЬКИ на UI-thread.
        /// </summary>
        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        /// <summary>
        /// Кеш brushes по ARGB, щоб не створювати new SolidColorBrush на кожен render.
        /// </summary>
        private readonly Dictionary<int, Brush> _brushCache = new();

        /// <summary>
        /// CTS для побудови матриці/summary.
        /// Якщо користувач швидко перемикає профілі — попередній build скасовуємо.
        /// </summary>
        private CancellationTokenSource? _matrixCts;

        /// <summary>
        /// Версія білду для stale-guard:
        /// якщо старий Task завершиться після нового — ігноруємо старий результат.
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

        // ---- Додаткові поля для "Schedule Statistic" ----

        private int _scheduleMonth;
        public int ScheduleMonth
        {
            get => _scheduleMonth;
            private set => SetProperty(ref _scheduleMonth, value);
        }

        private int _scheduleYear;
        public int ScheduleYear
        {
            get => _scheduleYear;
            private set => SetProperty(ref _scheduleYear, value);
        }

        private string _shopAddress = string.Empty;
        public string ShopAddress
        {
            get => _shopAddress;
            private set => SetProperty(ref _shopAddress, value);
        }

        private int _totalDays;
        public int TotalDays
        {
            get => _totalDays;
            private set => SetProperty(ref _totalDays, value);
        }

        private string _shift1 = string.Empty;
        public string Shift1
        {
            get => _shift1;
            private set => SetProperty(ref _shift1, value);
        }

        private string _shift2 = string.Empty;
        public string Shift2
        {
            get => _shift2;
            private set => SetProperty(ref _shift2, value);
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

        /// <summary>
        /// Ревізія стилів — простий “індикатор зміни”.
        /// Часто UI тригериться на зміну числа, щоб перевизначити стилі DataGrid.
        /// </summary>
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

        private string _totalEmployeesListText = string.Empty;
        public string TotalEmployeesListText
        {
            get => _totalEmployeesListText;
            private set => SetProperty(ref _totalEmployeesListText, value);
        }

        /// <summary>
        /// Employees — працівники в цьому schedule (може біндитися в UI).
        /// </summary>
        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();

        // =========================================================
        // 5) SUMMARY TABLE (Excel-like)
        // =========================================================

        /// <summary>
        /// Заголовки днів: Monday(01.01.2026), ...
        /// ItemsControl в XAML будує по ним групові колонки.
        /// </summary>
        public ObservableCollection<SummaryDayHeader> SummaryDayHeaders { get; } = new();

        /// <summary>
        /// Рядки summary: Employee | Sum | Days[] (From/To/Hours).
        /// </summary>
        public ObservableCollection<SummaryEmployeeRow> SummaryRows { get; } = new();

        // =========================================================
        // 5.1) ДОДАНО: MINI TABLE Employee | Work Day | Free Day
        // =========================================================

        /// <summary>
        /// Один рядок для mini-table під summary:
        /// Employee | WorkDays | FreeDays.
        /// </summary>
        public sealed class EmployeeWorkFreeStatRow
        {
            public string Employee { get; }
            public int WorkDays { get; }
            public int FreeDays { get; }

            public EmployeeWorkFreeStatRow(string employee, int workDays, int freeDays)
            {
                Employee = employee ?? string.Empty;
                WorkDays = workDays;
                FreeDays = freeDays;
            }
        }

        /// <summary>
        /// Колекція для рендеру mini-table в XAML.
        /// Оновлюється після побудови SummaryRows.
        /// </summary>
        public ObservableCollection<EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; } = new();

        // =========================================================
        // 6) КОМАНДИ (навігація/дії)
        // =========================================================

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        /// <summary>
        /// MatrixChanged — подія для View (коли треба перебудувати DataGrid columns, refresh tooltips, etc).
        /// </summary>
        public event EventHandler? MatrixChanged;

        public ContainerScheduleProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            BackCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync());
        }

        // =========================================================
        // 7) ГОЛОВНИЙ МЕТОД: ЗАВАНТАЖИТИ ПРОФІЛЬ + ПОБУДУВАТИ МАТРИЦЮ + SUMMARY
        // =========================================================

        /// <summary>
        /// SetProfileAsync — показати профіль schedule.
        ///
        /// Алгоритм:
        /// 1) Скасувати попередній build.
        /// 2) На UI-thread одразу показати базові поля та очистити старі дані матриці/totals/summary.
        /// 3) На background:
        ///    - побудувати DataTable матриці,
        ///    - порахувати totals,
        ///    - сформувати per-employee tooltip тексти,
        ///    - сформувати summary (headers + rows) з матриці.
        /// 4) На UI-thread застосувати всі результати разом.
        /// 5) ДОДАНО: на UI-thread після SummaryRows — перебудувати EmployeeWorkFreeStats.
        /// </summary>
        public async Task SetProfileAsync(
            ScheduleModel schedule,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel> cellStyles,
            CancellationToken ct = default)
        {
            // ---------- 1) Cancel попереднього білду ----------
            _matrixCts?.Cancel();
            _matrixCts?.Dispose();

            var localCts = _matrixCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = localCts.Token;

            // Унікальна версія білду для stale-guard
            var version = Interlocked.Increment(ref _matrixVersion);

            // ---------- 2) Швидке оновлення базових полів на UI-thread ----------
            await _owner.RunOnUiThreadAsync(() =>
            {
                // Базові поля профілю
                ScheduleId = schedule.Id;
                ScheduleName = schedule.Name ?? string.Empty;
                ScheduleMonthYear = $"{schedule.Month:D2}.{schedule.Year}";
                ShopName = schedule.Shop?.Name ?? string.Empty;
                Note = schedule.Note ?? string.Empty;

                // Додаткові "Schedule Statistic"
                ScheduleMonth = schedule.Month;
                ScheduleYear = schedule.Year;
                TotalDays = SafeDaysInMonth(schedule.Year, schedule.Month);
                Shift1 = GetScheduleString(schedule, "Shift1Time", "Shift1");
                Shift2 = GetScheduleString(schedule, "Shift2Time", "Shift2");
                ShopAddress = GetShopAddress(schedule.Shop);

                // Employees (використовуємо в summary і потенційно в UI)
                Employees.Clear();
                foreach (var emp in employees)
                    Employees.Add(emp);

                // Очищаємо старі дані (щоб не миготіли чужі значення)
                ScheduleMatrix = new DataView();
                TotalEmployees = 0;
                TotalHoursText = "0h 0m";
                TotalEmployeesListText = string.Empty;
                _employeeTotalHoursText.Clear();

                SummaryDayHeaders.Clear();
                SummaryRows.Clear();

                // ДОДАНО: очищаємо mini-table
                EmployeeWorkFreeStats.Clear();

                MatrixChanged?.Invoke(this, EventArgs.Empty);
            }).ConfigureAwait(false);

            try
            {
                // ---------- 3) Snapshot (захист від зміни колекцій ззовні під час Task.Run) ----------
                var year = schedule.Year;
                var month = schedule.Month;

                var slotsSnapshot = slots.ToList();
                var employeesSnapshot = employees.ToList();
                var stylesSnapshot = cellStyles.ToList();

                // ---------- 4) Важка побудова на background thread ----------
                var built = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    // 4.1) Build matrix (DataTable)
                    var table = ScheduleMatrixEngine.BuildScheduleTable(
                        year,
                        month,
                        slotsSnapshot,
                        employeesSnapshot,
                        out var colMap,
                        token);

                    token.ThrowIfCancellationRequested();

                    // 4.2) Totals (engine)
                    var totals = ScheduleTotalsCalculator.Calculate(employeesSnapshot, slotsSnapshot);

                    // 4.3) Tooltip totals по кожному employee
                    var perEmployeeText = new Dictionary<int, string>(capacity: Math.Max(8, totals.TotalEmployees));
                    foreach (var emp in employeesSnapshot)
                    {
                        var empId = emp.EmployeeId;
                        totals.PerEmployeeDuration.TryGetValue(empId, out var empTotal);

                        perEmployeeText[empId] =
                            $"Total hours: {ScheduleTotalsCalculator.FormatHoursMinutes(empTotal)}";
                    }

                    var totalHoursText = ScheduleTotalsCalculator.FormatHoursMinutes(totals.TotalDuration);

                    var employeeNames = employeesSnapshot
                        .Select(GetEmployeeDisplayName)
                        .Select(name => (name ?? string.Empty).Trim())
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                    // 4.4) Summary з матриці (ВАЖЛИВО: саме тут table/colMap вже існують)
                    var summary = BuildSummaryFromMatrix(table, colMap, employeesSnapshot, year, month);

                    return new BuildResult(
                        view: table.DefaultView,
                        colMap: colMap,
                        styles: stylesSnapshot,
                        totalEmployees: totals.TotalEmployees,
                        totalHoursText: totalHoursText,
                        totalEmployeesListText: BuildPreviewList(employeeNames),
                        perEmployeeText: perEmployeeText,
                        summaryHeaders: summary.Headers,
                        summaryRows: summary.Rows);

                }, token).ConfigureAwait(false);

                // Якщо цей build застарів або скасований — виходимо
                if (token.IsCancellationRequested || version != _matrixVersion)
                    return;

                // ---------- 5) Застосування результатів на UI-thread ----------
                await _owner.RunOnUiThreadAsync(() =>
                {
                    // stale guard ще раз (між await і UI могло пройти трохи часу)
                    if (token.IsCancellationRequested || version != _matrixVersion)
                        return;

                    // 5.1) Матриця
                    ScheduleMatrix = built.View;

                    // 5.2) Summary
                    SummaryDayHeaders.Clear();
                    foreach (var h in built.SummaryHeaders)
                        SummaryDayHeaders.Add(h);

                    SummaryRows.Clear();
                    foreach (var r in built.SummaryRows)
                        SummaryRows.Add(r);

                    // 5.3) Мапи стилів та колонок
                    RebuildStyleMaps(built.ColMap, built.Styles);

                    // 5.4) Totals
                    TotalEmployees = built.TotalEmployees;
                    TotalHoursText = built.TotalHoursText;
                    TotalEmployeesListText = built.TotalEmployeesListText;

                    // 5.5) Tooltips
                    _employeeTotalHoursText.Clear();
                    foreach (var kv in built.PerEmployeeText)
                        _employeeTotalHoursText[kv.Key] = kv.Value;

                    // 5.6) ДОДАНО: mini-table Work/Free days
                    RebuildEmployeeWorkFreeStats();

                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Норма: користувач переключив профіль, білд скасований
            }
        }

        /// <summary>
        /// Викликати коли виходиш з екрану, щоб зупинити background роботу.
        /// </summary>
        internal void CancelBackgroundWork()
        {
            _matrixCts?.Cancel();
            _matrixCts?.Dispose();
            _matrixCts = null;
        }

        // =========================================================
        // 8) ДОДАНО: ОБЧИСЛЕННЯ WORK/FREE DAYS З SummaryRows
        // =========================================================

        /// <summary>
        /// Перебудувати EmployeeWorkFreeStats на основі SummaryRows.
        ///
        /// Rules:
        /// - Робочий день = якщо в SummaryDayCell є годинник/часи (Hours > 0) або заповнений From/To.
        /// - FreeDays = TotalDays - WorkDays (мінімум 0).
        ///
        /// Викликаємо ТІЛЬКИ на UI-thread.
        /// </summary>
        private void RebuildEmployeeWorkFreeStats()
        {
            EmployeeWorkFreeStats.Clear();

            if (SummaryRows.Count == 0 || TotalDays <= 0)
                return;

            foreach (var row in SummaryRows)
            {
                int workDays = 0;

                // Days — ObservableCollection<SummaryDayCell>
                if (row.Days != null)
                {
                    foreach (var d in row.Days)
                    {
                        // 1) Якщо є часи From/To — вже вважаємо день робочим
                        if (!string.IsNullOrWhiteSpace(d.From) || !string.IsNullOrWhiteSpace(d.To))
                        {
                            workDays++;
                            continue;
                        }

                        // 2) Інакше пробуємо розпарсити Hours ("0", "6", "6h 30m")
                        if (TryParseSummaryHoursToMinutes(d.Hours, out var minutes) && minutes > 0)
                        {
                            workDays++;
                            continue;
                        }

                        // 3) Інакше — не робочий
                    }
                }

                int freeDays = Math.Max(0, TotalDays - workDays);

                EmployeeWorkFreeStats.Add(new EmployeeWorkFreeStatRow(
                    employee: row.Employee,
                    workDays: workDays,
                    freeDays: freeDays));
            }
        }

        /// <summary>
        /// Парсить summary Hours в хвилини.
        /// Підтримка:
        /// - "0"
        /// - "6"  (години)
        /// - "6h 30m"
        /// - "12h"
        /// - "30m"
        /// </summary>
        private static bool TryParseSummaryHoursToMinutes(string? text, out int minutes)
        {
            minutes = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var s = text.Trim();

            // найчастіший формат у тебе: "0" або "6"
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hoursOnly))
            {
                minutes = Math.Max(0, hoursOnly) * 60;
                return true;
            }

            // формат "6h 30m"
            var m = SummaryHoursRegex.Match(s);
            if (!m.Success)
                return false;

            int h = 0;
            int mm = 0;

            if (m.Groups[1].Success)
                int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out h);

            if (m.Groups[2].Success)
                int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out mm);

            minutes = Math.Max(0, h) * 60 + Math.Max(0, mm);
            return true;
        }

        // =========================================================
        // 9) TOOLTIP TOTAL HOURS ПО КОЛОНЦІ ПРАЦІВНИКА
        // =========================================================

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
        // 10) IScheduleMatrixStyleProvider
        // =========================================================

        private static bool IsTechnicalMatrixColumn(string columnName)
        {
            // Технічні колонки в матриці (не employee)
            return columnName == ScheduleMatrixConstants.DayColumnName
                || columnName == ScheduleMatrixConstants.ConflictColumnName
                || columnName == ScheduleMatrixConstants.WeekendColumnName;
        }

        public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
        {
            cellRef = default;

            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            if (IsTechnicalMatrixColumn(columnName))
                return false;

            if (rowData is not DataRowView rowView)
                return false;

            // DayOfMonth — ключ до стилю (day, employeeId)
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

        public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)
        {
            if (!TryGetCellStyle(cellRef, out var style))
                return null;

            if (style.BackgroundColorArgb is not int argb || argb == 0)
                return null;

            return ToBrushCached(argb);
        }

        public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)
        {
            if (!TryGetCellStyle(cellRef, out var style))
                return null;

            if (style.TextColorArgb is not int argb || argb == 0)
                return null;

            return ToBrushCached(argb);
        }

        public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _cellStyleStore.TryGetStyle(cellRef, out style);

        // =========================================================
        // 11) ВНУТРІШНІ HELPERS: стиль/brush кеш
        // =========================================================

        private void RebuildStyleMaps(Dictionary<string, int> colMap, IList<ScheduleCellStyleModel> cellStyles)
        {
            _colNameToEmpId.Clear();
            foreach (var pair in colMap)
                _colNameToEmpId[pair.Key] = pair.Value;

            _cellStyleStore.Load(cellStyles);

            // Тригер для UI (оновити стилі)
            CellStyleRevision++;
        }

        private Brush ToBrushCached(int argb)
        {
            if (_brushCache.TryGetValue(argb, out var b))
                return b;

            b = ColorHelpers.ToBrush(argb);

            // Freeze = immutable + швидше для WPF рендера
            if (b is Freezable f && f.CanFreeze)
                f.Freeze();

            _brushCache[argb] = b;
            return b;
        }

        // =========================================================
        // 12) SCHEDULE-INFO HELPERS (Month days, shifts, address)
        // =========================================================

        private static int SafeDaysInMonth(int year, int month)
        {
            try
            {
                if (year <= 0 || month < 1 || month > 12)
                    return 0;

                return DateTime.DaysInMonth(year, month);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Безпечно дістає строкове значення з ScheduleModel по можливих назвах властивостей.
        /// (бо в моделях інколи “Shift1Time”/”Shift1” тощо).
        /// </summary>
        private static string GetScheduleString(ScheduleModel schedule, params string[] propertyNames)
        {
            if (schedule is null) return string.Empty;

            var t = schedule.GetType();

            foreach (var name in propertyNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;

                var prop = t.GetProperty(name);
                if (prop is null) continue;

                var val = prop.GetValue(schedule);
                var s = val?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }

            return string.Empty;
        }

        /// <summary>
        /// Дістає адресу магазину, підтримуючи різні варіанти назв полів у ShopModel.
        /// </summary>
        private static string GetShopAddress(ShopModel? shop)
        {
            if (shop is null) return string.Empty;

            var t = shop.GetType();

            foreach (var name in new[] { "Address", "Adress", "ShopAddress", "FullAddress" })
            {
                var prop = t.GetProperty(name);
                if (prop is null) continue;

                var val = prop.GetValue(shop);
                var s = val?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }

            // варіант: shop.Location.Address
            var locProp = t.GetProperty("Location");
            if (locProp?.GetValue(shop) is object locObj)
            {
                var lt = locObj.GetType();
                var addrProp = lt.GetProperty("Address") ?? lt.GetProperty("Adress");
                var val = addrProp?.GetValue(locObj);
                var s = val?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }

            return string.Empty;
        }

        // =========================================================
        // 13) SUMMARY MODELS + BUILDER
        // =========================================================

        /// <summary>
        /// Header для одного дня (для групового заголовка "Monday(01.01.2026)").
        /// </summary>
        public sealed class SummaryDayHeader
        {
            public int Day { get; }
            public string Text { get; }

            public SummaryDayHeader(int day, string text)
            {
                Day = day;
                Text = text;
            }
        }

        /// <summary>
        /// Одна клітинка дня: From/To/Hours.
        /// </summary>
        public sealed class SummaryDayCell
        {
            public string From { get; }
            public string To { get; }
            public string Hours { get; }

            public SummaryDayCell(string from = "", string to = "", string hours = "")
            {
                From = from;
                To = to;
                Hours = hours;
            }
        }

        /// <summary>
        /// Рядок summary для одного employee: Employee | Sum | Days[].
        /// </summary>
        public sealed class SummaryEmployeeRow
        {
            public string Employee { get; }
            public string Sum { get; }
            public ObservableCollection<SummaryDayCell> Days { get; }

            public SummaryEmployeeRow(string employee, string sum, IList<SummaryDayCell> days)
            {
                Employee = employee;
                Sum = sum;
                Days = new ObservableCollection<SummaryDayCell>(days);
            }
        }

        /// <summary>
        /// Формує:
        /// - Headers на кожен день місяця
        /// - Rows по кожному employee
        ///
        /// Джерело правди: DataTable матриці (те, що показує DataGrid зверху).
        /// Тобто summary 1-в-1 відображає “графік зверху”.
        /// </summary>
        private static (List<SummaryDayHeader> Headers, List<SummaryEmployeeRow> Rows)
            BuildSummaryFromMatrix(
                DataTable table,
                Dictionary<string, int> colMap,
                IList<ScheduleEmployeeModel> employees,
                int year,
                int month)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // Day -> DataRow
            // У DataTable матриці кожен ряд — окремий день.
            var rowsByDay = table.Rows.Cast<DataRow>()
                .ToDictionary(
                    r => Convert.ToInt32(r[ScheduleMatrixConstants.DayColumnName], CultureInfo.InvariantCulture),
                    r => r);

            // Headers: Monday(01.01.2026)...
            // InvariantCulture, щоб завжди було "Monday" як на твоєму скріні.
            var headers = new List<SummaryDayHeader>(daysInMonth);
            for (var d = 1; d <= daysInMonth; d++)
            {
                var dt = new DateTime(year, month, d);
                var headerText = dt.ToString("dddd(dd.MM.yyyy)", CultureInfo.InvariantCulture);
                headers.Add(new SummaryDayHeader(d, headerText));
            }

            // colMap: columnName -> employeeId
            // Робимо швидкий доступ: employeeId -> columnName
            var colByEmpId = colMap.ToDictionary(kv => kv.Value, kv => kv.Key);

            var resultRows = new List<SummaryEmployeeRow>(employees.Count);

            foreach (var emp in employees)
            {
                // employeeId — ключ для пошуку колонки employee у DataTable
                var empId = emp.EmployeeId;

                if (!colByEmpId.TryGetValue(empId, out var colName))
                    continue; // якщо раптом employee не потрапив у матрицю

                var displayName = GetEmployeeDisplayName(emp);

                var dayCells = new List<SummaryDayCell>(daysInMonth);
                var sum = TimeSpan.Zero;

                for (var d = 1; d <= daysInMonth; d++)
                {
                    if (!rowsByDay.TryGetValue(d, out var dr))
                    {
                        dayCells.Add(new SummaryDayCell());
                        continue;
                    }

                    var obj = dr[colName];
                    var raw = obj == null || obj == DBNull.Value ? "" : (obj.ToString() ?? "");

                    // EmptyMark — стандарт “порожньо” у матриці
                    if (string.IsNullOrWhiteSpace(raw) || raw == ScheduleMatrixConstants.EmptyMark)
                    {
                        dayCells.Add(new SummaryDayCell());
                        continue;
                    }

                    // Оптимізація: якщо в тексті немає двокрапки — це не час,
                    // тож не ганяємо regex.
                    if (raw.IndexOf(':') < 0)
                    {
                        // Наприклад OFF / Conflict / інший текст
                        dayCells.Add(new SummaryDayCell(raw, "", ""));
                        continue;
                    }

                    if (TryParseTimeRanges(raw, out var from, out var to, out var dur))
                    {
                        sum += dur;
                        dayCells.Add(new SummaryDayCell(from, to, FormatHoursCell(dur)));
                    }
                    else
                    {
                        // fallback: якщо часи не розпарсились
                        dayCells.Add(new SummaryDayCell(raw, "", ""));
                    }
                }

                resultRows.Add(new SummaryEmployeeRow(displayName, FormatHoursCell(sum), dayCells));
            }

            return (headers, resultRows);
        }

        /// <summary>
        /// Парсер часу з клітинки матриці.
        /// Витягує всі HH:mm і:
        /// - From = перший
        /// - To   = останній
        /// - Duration = сумуємо парами (0-1) + (2-3) + ...
        ///
        /// Підтримує формат:
        /// - "09:00 - 15:00"
        /// - "09:00-12:00; 13:00-15:00"
        /// - багаторядковий текст з кількома інтервалами
        /// </summary>
        private static bool TryParseTimeRanges(string text, out string from, out string to, out TimeSpan duration)
        {
            from = "";
            to = "";
            duration = TimeSpan.Zero;

            var matches = TimeRegex.Matches(text);
            if (matches.Count < 2)
                return false;

            // швидко перетворюємо matches у TimeSpan
            var times = new List<TimeSpan>(matches.Count);

            foreach (Match m in matches)
            {
                // WPF матриця майже завжди дає hh:mm або h:mm
                if (TimeSpan.TryParseExact(
                        m.Value,
                        new[] { @"h\:mm", @"hh\:mm" },
                        CultureInfo.InvariantCulture,
                        out var ts))
                {
                    times.Add(ts);
                }
            }

            if (times.Count < 2)
                return false;

            from = matches[0].Value;
            to = matches[matches.Count - 1].Value;

            // сумуємо парами: (0-1), (2-3), ...
            for (var i = 0; i + 1 < times.Count; i += 2)
            {
                var delta = times[i + 1] - times[i];
                if (delta > TimeSpan.Zero)
                    duration += delta;
            }

            // Якщо з якихось причин не вийшло парами — fallback first-last
            if (duration == TimeSpan.Zero)
            {
                var delta = times[^1] - times[0];
                if (delta > TimeSpan.Zero)
                    duration = delta;
            }

            return true;
        }

        /// <summary>
        /// Формат “години” для summary:
        /// - якщо хвилин 0 -> "6"
        /// - інакше -> "6h 30m"
        /// </summary>
        private static string FormatHoursCell(TimeSpan ts)
        {
            var totalMinutes = (int)Math.Round(ts.TotalMinutes);
            if (totalMinutes <= 0) return "0";

            var h = totalMinutes / 60;
            var m = totalMinutes % 60;

            return m == 0
                ? h.ToString(CultureInfo.InvariantCulture)
                : $"{h}h {m}m";
        }

        private static string BuildPreviewList(IReadOnlyList<string> items, int previewCount = 8)
        {
            if (items == null || items.Count == 0)
                return "—";

            var trimmed = items
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();

            if (trimmed.Count == 0)
                return "—";

            var shown = trimmed.Take(previewCount).ToList();
            var remaining = trimmed.Count - shown.Count;
            var text = string.Join(", ", shown);

            return remaining > 0
                ? $"{text}, +{remaining} more"
                : text;
        }

        /// <summary>
        /// Дістає FirstName/LastName (або варіанти назв) з будь-якого об’єкта.
        /// Повертає "FirstName LastName" або "FirstName"/"LastName" якщо є тільки одне.
        /// </summary>
        private static bool TryGetFirstLast(object obj, out string fullName)
        {
            fullName = string.Empty;

            var first = TryGetString(obj, "FirstName")
                     ?? TryGetString(obj, "Firstname")
                     ?? TryGetString(obj, "GivenName");

            var last = TryGetString(obj, "LastName")
                     ?? TryGetString(obj, "Lastname")
                     ?? TryGetString(obj, "Surname")
                     ?? TryGetString(obj, "FamilyName");

            var combined = $"{first} {last}".Trim();

            if (!string.IsNullOrWhiteSpace(combined))
            {
                fullName = combined;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Безпечно дістає строкове значення властивості з об’єкта.
        /// </summary>
        private static string? TryGetString(object obj, string propertyName)
        {
            var p = obj.GetType().GetProperty(propertyName);
            if (p?.GetValue(obj) is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();

            return null;
        }

            /// <summary>
            /// Дістаємо ім’я працівника максимально “толерантно” до різних моделей.
            /// (Якщо в майбутньому модель зміниться — UI не впаде.)
            /// </summary>
        private static string GetEmployeeDisplayName(ScheduleEmployeeModel emp)
        {
            if (emp is null)
                return string.Empty;

            // 1) Найкращий варіант: FirstName + LastName на самому ScheduleEmployeeModel
            if (TryGetFirstLast(emp, out var fullName))
                return fullName;

            // 2) Часто дані лежать у вкладеній властивості Employee
            var empProp = emp.GetType().GetProperty("Employee");
            if (empProp?.GetValue(emp) is object empObj)
            {
                if (TryGetFirstLast(empObj, out fullName))
                    return fullName;

                // 3) Інші можливі “готові” поля на вкладеній Employee-моделі
                var nested = TryGetString(empObj, "FullName")
                          ?? TryGetString(empObj, "EmployeeName")
                          ?? TryGetString(empObj, "DisplayName")
                          ?? TryGetString(empObj, "Name");

                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }

            // 4) Інші можливі “готові” поля на ScheduleEmployeeModel
            var direct = TryGetString(emp, "FullName")
                      ?? TryGetString(emp, "EmployeeName")
                      ?? TryGetString(emp, "DisplayName")
                      ?? TryGetString(emp, "Name");

            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            // 5) Fallback
            return $"Employee {emp.EmployeeId}";
        }

        // =========================================================
        // 13) RESULT DTO (щоб не повертати 8-10 елементів tuple-ом)
        // =========================================================

        /// <summary>
        /// Невеликий DTO, який повертаємо з background task одним об’єктом.
        /// Це читабельніше, ніж гігантський tuple.
        /// </summary>
        private sealed class BuildResult
        {
            public DataView View { get; }
            public Dictionary<string, int> ColMap { get; }
            public IList<ScheduleCellStyleModel> Styles { get; }
            public int TotalEmployees { get; }
            public string TotalHoursText { get; }
            public string TotalEmployeesListText { get; }
            public Dictionary<int, string> PerEmployeeText { get; }
            public List<SummaryDayHeader> SummaryHeaders { get; }
            public List<SummaryEmployeeRow> SummaryRows { get; }

            public BuildResult(
                DataView view,
                Dictionary<string, int> colMap,
                IList<ScheduleCellStyleModel> styles,
                int totalEmployees,
                string totalHoursText,
                string totalEmployeesListText,
                Dictionary<int, string> perEmployeeText,
                List<SummaryDayHeader> summaryHeaders,
                List<SummaryEmployeeRow> summaryRows)
            {
                View = view;
                ColMap = colMap;
                Styles = styles;
                TotalEmployees = totalEmployees;
                TotalHoursText = totalHoursText;
                TotalEmployeesListText = totalEmployeesListText;
                PerEmployeeText = perEmployeeText;
                SummaryHeaders = summaryHeaders;
                SummaryRows = summaryRows;
            }
        }
    }
}
