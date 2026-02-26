/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleProfileViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using Microsoft.Win32;
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
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using WPFApp.View.Dialogs;
using WPFApp.Applications.Export;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Commands;
using WPFApp.UI.Helpers;
using WPFApp.Applications.Matrix.Schedule;


namespace WPFApp.ViewModel.Container.ScheduleProfile
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerScheduleProfileViewModel : ViewModelBase, IScheduleMatrixStyleProvider` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerScheduleProfileViewModel : ViewModelBase, IScheduleMatrixStyleProvider
    {
        
        
        

        
        
        
        
        
        private static readonly Regex TimeRegex =
            new(@"\b([01]?\d|2[0-3]):[0-5]\d\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        
        
        
        private static readonly Regex SummaryHoursRegex =
            new(@"^\s*(?:(\d+)\s*h)?\s*(?:(\d+)\s*m)?\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        
        
        

        
        
        
        
        private readonly ContainerViewModel _owner;

        
        
        
        
        
        
        
        private readonly Dictionary<string, int> _colNameToEmpId = new();

        
        
        
        private readonly ScheduleCellStyleStore _cellStyleStore = new();

        
        
        
        
        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        
        
        
        private readonly Dictionary<int, Brush> _brushCache = new();

        
        
        
        
        private CancellationTokenSource? _matrixCts;


        
        
        
        
        private int _matrixVersion;

        private ScheduleModel? _currentSchedule;
        private IReadOnlyList<ScheduleEmployeeModel> _scheduleEmployees = Array.Empty<ScheduleEmployeeModel>();
        private IReadOnlyList<ScheduleSlotModel> _scheduleSlots = Array.Empty<ScheduleSlotModel>();
        private IReadOnlyList<ScheduleCellStyleModel> _scheduleCellStyles = Array.Empty<ScheduleCellStyleModel>();

        
        
        

        private int _scheduleId;
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleId
        {
            get => _scheduleId;
            private set => SetProperty(ref _scheduleId, value);
        }

        private string _scheduleName = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string ScheduleName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleName
        {
            get => _scheduleName;
            private set => SetProperty(ref _scheduleName, value);
        }

        private string _scheduleMonthYear = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string ScheduleMonthYear` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleMonthYear
        {
            get => _scheduleMonthYear;
            private set => SetProperty(ref _scheduleMonthYear, value);
        }

        private string _shopName = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string ShopName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ShopName
        {
            get => _shopName;
            private set => SetProperty(ref _shopName, value);
        }

        private string _note = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Note` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Note
        {
            get => _note;
            private set => SetProperty(ref _note, value);
        }

        

        private int _scheduleMonth;
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMonth` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMonth
        {
            get => _scheduleMonth;
            private set => SetProperty(ref _scheduleMonth, value);
        }

        private int _scheduleYear;
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleYear` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleYear
        {
            get => _scheduleYear;
            private set => SetProperty(ref _scheduleYear, value);
        }

        private string _shopAddress = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string ShopAddress` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ShopAddress
        {
            get => _shopAddress;
            private set => SetProperty(ref _shopAddress, value);
        }

        private int _totalDays;
        /// <summary>
        /// Визначає публічний елемент `public int TotalDays` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalDays
        {
            get => _totalDays;
            private set => SetProperty(ref _totalDays, value);
        }

        private string _shift1 = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Shift1` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Shift1
        {
            get => _shift1;
            private set => SetProperty(ref _shift1, value);
        }

        private string _shift2 = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Shift2` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Shift2
        {
            get => _shift2;
            private set => SetProperty(ref _shift2, value);
        }

        
        
        

        private DataView _scheduleMatrix = new DataView();
        /// <summary>
        /// Визначає публічний елемент `public DataView ScheduleMatrix` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView ScheduleMatrix
        {
            get => _scheduleMatrix;
            private set => SetProperty(ref _scheduleMatrix, value);
        }

        
        
        
        
        private int _cellStyleRevision;
        /// <summary>
        /// Визначає публічний елемент `public int CellStyleRevision` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int CellStyleRevision
        {
            get => _cellStyleRevision;
            private set => SetProperty(ref _cellStyleRevision, value);
        }

        
        
        

        private int _totalEmployees;
        /// <summary>
        /// Визначає публічний елемент `public int TotalEmployees` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalEmployees
        {
            get => _totalEmployees;
            private set => SetProperty(ref _totalEmployees, value);
        }

        private string _totalHoursText = "0h 0m";
        /// <summary>
        /// Визначає публічний елемент `public string TotalHoursText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalHoursText
        {
            get => _totalHoursText;
            private set => SetProperty(ref _totalHoursText, value);
        }

        private string _totalEmployeesListText = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string TotalEmployeesListText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalEmployeesListText
        {
            get => _totalEmployeesListText;
            private set => SetProperty(ref _totalEmployeesListText, value);
        }

        private bool _isExportStatusVisible;
        /// <summary>
        /// Визначає публічний елемент `public bool IsExportStatusVisible` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsExportStatusVisible
        {
            get => _isExportStatusVisible;
            private set => SetProperty(ref _isExportStatusVisible, value);
        }

        private UIStatusKind _exportStatus = UIStatusKind.Success;
        /// <summary>
        /// Визначає публічний елемент `public UIStatusKind ExportStatus` та контракт його використання у шарі WPFApp.
        /// </summary>
        public UIStatusKind ExportStatus
        {
            get => _exportStatus;
            private set => SetProperty(ref _exportStatus, value);
        }

        private CancellationTokenSource? _exportUiCts;


        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();

        
        
        

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<SummaryDayHeader> SummaryDayHeaders { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<SummaryDayHeader> SummaryDayHeaders { get; } = new();

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<SummaryEmployeeRow> SummaryRows { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<SummaryEmployeeRow> SummaryRows { get; } = new();

        
        
        

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public sealed class EmployeeWorkFreeStatRow` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class EmployeeWorkFreeStatRow
        {
            /// <summary>
            /// Визначає публічний елемент `public string Employee { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string Employee { get; }
            /// <summary>
            /// Визначає публічний елемент `public int WorkDays { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int WorkDays { get; }
            /// <summary>
            /// Визначає публічний елемент `public int FreeDays { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int FreeDays { get; }

            /// <summary>
            /// Визначає публічний елемент `public EmployeeWorkFreeStatRow(string employee, int workDays, int freeDays)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public EmployeeWorkFreeStatRow(string employee, int workDays, int freeDays)
            {
                Employee = employee ?? string.Empty;
                WorkDays = workDays;
                FreeDays = freeDays;
            }
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; } = new();

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand BackCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand BackCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelProfileCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelProfileCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand EditCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand DeleteCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ExportToExcelCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ExportToExcelCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ExportToSQLiteCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ExportToSQLiteCommand { get; }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? MatrixChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? MatrixChanged;

        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleProfileViewModel(ContainerViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            BackCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync());
            ExportToExcelCommand = new AsyncRelayCommand(ExportToExcelAsync);
            ExportToSQLiteCommand = new AsyncRelayCommand(ExportToSQLiteAsync);
        }

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public async Task SetProfileAsync(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public async Task SetProfileAsync(
            ScheduleModel schedule,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel> cellStyles,
            CancellationToken ct = default)
        {
            var employeesSnapshot = employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            var slotsSnapshot = slots?.ToList() ?? new List<ScheduleSlotModel>();
            var stylesSnapshot = cellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

            
            _matrixCts?.Cancel();
            _matrixCts?.Dispose();

            var localCts = _matrixCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = localCts.Token;

            
            var version = Interlocked.Increment(ref _matrixVersion);

                
                await _owner.RunOnUiThreadAsync(() =>
            {
                IsExportStatusVisible = false;
                ExportStatus = UIStatusKind.Success;
                
                _currentSchedule = schedule;
                _scheduleEmployees = employeesSnapshot;
                _scheduleSlots = slotsSnapshot;
                _scheduleCellStyles = stylesSnapshot;

                ScheduleId = schedule.Id;
                ScheduleName = schedule.Name ?? string.Empty;
                ScheduleMonthYear = $"{schedule.Month:D2}.{schedule.Year}";
                ShopName = schedule.Shop?.Name ?? string.Empty;
                Note = schedule.Note ?? string.Empty;

                
                ScheduleMonth = schedule.Month;
                ScheduleYear = schedule.Year;
                TotalDays = SafeDaysInMonth(schedule.Year, schedule.Month);
                Shift1 = GetScheduleString(schedule, "Shift1Time", "Shift1");
                Shift2 = GetScheduleString(schedule, "Shift2Time", "Shift2");
                ShopAddress = GetShopAddress(schedule.Shop);

                
                Employees.Clear();
                foreach (var emp in employeesSnapshot)
                    Employees.Add(emp);

                
                ScheduleMatrix = new DataView();
                TotalEmployees = 0;
                TotalHoursText = "0h 0m";
                TotalEmployeesListText = string.Empty;
                _employeeTotalHoursText.Clear();

                SummaryDayHeaders.Clear();
                SummaryRows.Clear();

                
                EmployeeWorkFreeStats.Clear();

                MatrixChanged?.Invoke(this, EventArgs.Empty);
            }).ConfigureAwait(false);

                try
            {
                
                var year = schedule.Year;
                var month = schedule.Month;
                var peoplePerShift = schedule.PeoplePerShift;
                var shift1Time = schedule.Shift1Time;
                var shift2Time = schedule.Shift2Time;

                
                var built = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    
                    var table = ScheduleMatrixEngine.BuildScheduleTable(
                        year,
                        month,
                        slotsSnapshot,
                        employeesSnapshot,
                        out var colMap,
                        token);

                    token.ThrowIfCancellationRequested();

                    var daysInMonth = DateTime.DaysInMonth(year, month);
                    for (int day = 1; day <= daysInMonth; day++)
                    {
                        var conflict = ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(
                            slotsSnapshot,
                            day,
                            peoplePerShift,
                            shift1Time,
                            shift2Time);

                        table.Rows[day - 1][ScheduleMatrixConstants.ConflictColumnName] = conflict;
                    }


                    
                    var totals = ScheduleTotalsCalculator.Calculate(employeesSnapshot, slotsSnapshot);

                    
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

                
                if (token.IsCancellationRequested || version != _matrixVersion)
                    return;

                
                await _owner.RunOnUiThreadAsync(() =>
                {
                    
                    if (token.IsCancellationRequested || version != _matrixVersion)
                        return;

                    
                    ScheduleMatrix = built.View;

                    
                    SummaryDayHeaders.Clear();
                    foreach (var h in built.SummaryHeaders)
                        SummaryDayHeaders.Add(h);

                    SummaryRows.Clear();
                    foreach (var r in built.SummaryRows)
                        SummaryRows.Add(r);

                    
                    RebuildStyleMaps(built.ColMap, built.Styles);

                    
                    TotalEmployees = built.TotalEmployees;
                    TotalHoursText = built.TotalHoursText;
                    TotalEmployeesListText = built.TotalEmployeesListText;

                    
                    _employeeTotalHoursText.Clear();
                    foreach (var kv in built.PerEmployeeText)
                        _employeeTotalHoursText[kv.Key] = kv.Value;

                    
                    RebuildEmployeeWorkFreeStats();

                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                
            }

        }

        
        
        
        internal void CancelBackgroundWork()
        {
            _matrixCts?.Cancel();
            _matrixCts?.Dispose();
            _matrixCts = null;
        }

        
        
        

        private async Task ExportToExcelAsync(CancellationToken ct)
        {
            if (_currentSchedule is null)
            {
                _owner.ShowError("No schedule is selected for export.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Schedule to Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FileName = ScheduleExportService.SanitizeFileName(
                    $"{ScheduleName}.xlsx",
                    "Schedule.xlsx")
            };

            if (dialog.ShowDialog() != true)
                return;

            var uiToken = ResetExportUiCts(ct);
            await ShowExportWorkingAsync().ConfigureAwait(false);

            var context = new ScheduleExportContext(
                scheduleName: ScheduleName,
                scheduleMonth: ScheduleMonth,
                scheduleYear: ScheduleYear,
                shopName: ShopName,
                shopAddress: ShopAddress,
                totalHoursText: TotalHoursText,
                totalEmployees: TotalEmployees,
                totalDays: TotalDays,
                shift1: Shift1,
                shift2: Shift2,
                totalEmployeesListText: TotalEmployeesListText,
                scheduleMatrix: ScheduleMatrix,
                summaryDayHeaders: SummaryDayHeaders.ToList(),
                summaryRows: SummaryRows.ToList(),
                employeeWorkFreeStats: EmployeeWorkFreeStats.ToList(),
                styleProvider: this);

            try
            {
                await _owner.ExportScheduleToExcelAsync(context, dialog.FileName, uiToken).ConfigureAwait(false);

                
                await ShowExportSuccessThenAutoHideAsync(uiToken, milliseconds: 1400).ConfigureAwait(false);

                
                
            }
            catch (OperationCanceledException)
            {
                await HideExportStatusAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HideExportStatusAsync().ConfigureAwait(false);
                _owner.ShowError(ex);
            }
        }

        private async Task ExportToSQLiteAsync(CancellationToken ct)
        {
            if (_currentSchedule is null)
            {
                _owner.ShowError("No schedule is selected for export.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Schedule to SQLite Script",
                Filter = "SQLite Script (*.sql)|*.sql|All Files (*.*)|*.*",
                FileName = ScheduleExportService.SanitizeFileName(
                    $"{ScheduleName}.sqlite.sql",
                    "Schedule.sqlite.sql")
            };

            if (dialog.ShowDialog() != true)
                return;

            var uiToken = ResetExportUiCts(ct);
            await ShowExportWorkingAsync().ConfigureAwait(false);

            try
            {
                var availabilityData = await _owner
                    .LoadAvailabilityGroupExportDataAsync(_currentSchedule.AvailabilityGroupId, uiToken)
                    .ConfigureAwait(false);

                var context = new ScheduleSqlExportContext(
                    schedule: _currentSchedule,
                    employees: _scheduleEmployees,
                    slots: _scheduleSlots,
                    cellStyles: _scheduleCellStyles,
                    availabilityGroupData: availabilityData);

                await _owner.ExportScheduleToSqlAsync(context, dialog.FileName, uiToken).ConfigureAwait(false);

                await ShowExportSuccessThenAutoHideAsync(uiToken, milliseconds: 1400).ConfigureAwait(false);

                
            }
            catch (OperationCanceledException)
            {
                await HideExportStatusAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HideExportStatusAsync().ConfigureAwait(false);
                _owner.ShowError(ex);
            }
        }

        
        
        

        
        
        
        
        
        
        
        
        
        private void RebuildEmployeeWorkFreeStats()
        {
            EmployeeWorkFreeStats.Clear();

            if (SummaryRows.Count == 0 || TotalDays <= 0)
                return;

            foreach (var row in SummaryRows)
            {
                
                
                EmployeeWorkFreeStats.Add(new EmployeeWorkFreeStatRow(
                    employee: row.Employee,
                    workDays: row.WorkDays,
                    freeDays: row.FreeDays));
            }
        }

        
        
        
        
        
        
        
        
        
        private static bool TryParseSummaryHoursToMinutes(string? text, out int minutes)
        {
            minutes = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var s = text.Trim();

            
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hoursOnly))
            {
                minutes = Math.Max(0, hoursOnly) * 60;
                return true;
            }

            
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

        private static int CountWorkDays(IEnumerable<SummaryDayCell>? days)
        {
            if (days == null) return 0;

            int workDays = 0;

            foreach (var d in days)
            {
                
                if (!string.IsNullOrWhiteSpace(d.From) || !string.IsNullOrWhiteSpace(d.To))
                {
                    workDays++;
                    continue;
                }

                
                if (TryParseSummaryHoursToMinutes(d.Hours, out var minutes) && minutes > 0)
                {
                    workDays++;
                }
            }

            return workDays;
        }


        
        
        

        /// <summary>
        /// Визначає публічний елемент `public string GetEmployeeTotalHoursText(string columnName)` та контракт його використання у шарі WPFApp.
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

        
        
        

        private static bool IsTechnicalMatrixColumn(string columnName)
        {
            
            return columnName == ScheduleMatrixConstants.DayColumnName
                || columnName == ScheduleMatrixConstants.ConflictColumnName
                || columnName == ScheduleMatrixConstants.WeekendColumnName;
        }

        /// <summary>
        /// Визначає публічний елемент `public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
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
        /// Визначає публічний елемент `public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)
        {
            if (!TryGetCellStyle(cellRef, out var style))
                return null;

            if (style.BackgroundColorArgb is not int argb || argb == 0)
                return null;

            return ToBrushCached(argb);
        }

        /// <summary>
        /// Визначає публічний елемент `public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
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
        /// Визначає публічний елемент `public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _cellStyleStore.TryGetStyle(cellRef, out style);

        
        
        

        private void RebuildStyleMaps(Dictionary<string, int> colMap, IList<ScheduleCellStyleModel> cellStyles)
        {
            _colNameToEmpId.Clear();
            foreach (var pair in colMap)
                _colNameToEmpId[pair.Key] = pair.Value;

            _cellStyleStore.Load(cellStyles);

            
            CellStyleRevision++;
        }

        private Brush ToBrushCached(int argb)
        {
            if (_brushCache.TryGetValue(argb, out var b))
                return b;

            b = ColorHelpers.ToBrush(argb);

            
            if (b is Freezable f && f.CanFreeze)
                f.Freeze();

            _brushCache[argb] = b;
            return b;
        }

        
        
        

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

        
        
        

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public sealed class SummaryDayHeader` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class SummaryDayHeader
        {
            /// <summary>
            /// Визначає публічний елемент `public int Day { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int Day { get; }
            /// <summary>
            /// Визначає публічний елемент `public string Text { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string Text { get; }

            /// <summary>
            /// Визначає публічний елемент `public SummaryDayHeader(int day, string text)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public SummaryDayHeader(int day, string text)
            {
                Day = day;
                Text = text;
            }
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public sealed class SummaryDayCell` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class SummaryDayCell
        {
            /// <summary>
            /// Визначає публічний елемент `public string From { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string From { get; }
            /// <summary>
            /// Визначає публічний елемент `public string To { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string To { get; }
            /// <summary>
            /// Визначає публічний елемент `public string Hours { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string Hours { get; }

            /// <summary>
            /// Визначає публічний елемент `public SummaryDayCell(string from = "", string to = "", string hours = "")` та контракт його використання у шарі WPFApp.
            /// </summary>
            public SummaryDayCell(string from = "", string to = "", string hours = "")
            {
                From = from;
                To = to;
                Hours = hours;
            }
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public sealed class SummaryEmployeeRow` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class SummaryEmployeeRow
        {
            /// <summary>
            /// Визначає публічний елемент `public string Employee { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string Employee { get; }
            /// <summary>
            /// Визначає публічний елемент `public int WorkDays { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int WorkDays { get; }
            /// <summary>
            /// Визначає публічний елемент `public int FreeDays { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int FreeDays { get; }
            /// <summary>
            /// Визначає публічний елемент `public string Sum { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string Sum { get; }
            /// <summary>
            /// Визначає публічний елемент `public ObservableCollection<SummaryDayCell> Days { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public ObservableCollection<SummaryDayCell> Days { get; }

            /// <summary>
            /// Визначає публічний елемент `public SummaryEmployeeRow(string employee, int workDays, int freeDays, string sum, IList<SummaryDayCell> days)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public SummaryEmployeeRow(string employee, int workDays, int freeDays, string sum, IList<SummaryDayCell> days)
            {
                Employee = employee ?? string.Empty;
                WorkDays = workDays;
                FreeDays = freeDays;
                Sum = sum ?? string.Empty;
                Days = new ObservableCollection<SummaryDayCell>(days);
            }
        }


        
        
        
        
        
        
        
        
        private static (List<SummaryDayHeader> Headers, List<SummaryEmployeeRow> Rows)
            BuildSummaryFromMatrix(
                DataTable table,
                Dictionary<string, int> colMap,
                IList<ScheduleEmployeeModel> employees,
                int year,
                int month)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);

            
            
            var rowsByDay = table.Rows.Cast<DataRow>()
                .ToDictionary(
                    r => Convert.ToInt32(r[ScheduleMatrixConstants.DayColumnName], CultureInfo.InvariantCulture),
                    r => r);

            
            
            var headers = new List<SummaryDayHeader>(daysInMonth);
            for (var d = 1; d <= daysInMonth; d++)
            {
                var dt = new DateTime(year, month, d);
                var headerText = dt.ToString("dddd(dd.MM.yyyy)", CultureInfo.InvariantCulture);
                headers.Add(new SummaryDayHeader(d, headerText));
            }

            
            
            var colByEmpId = colMap.ToDictionary(kv => kv.Value, kv => kv.Key);

            var resultRows = new List<SummaryEmployeeRow>(employees.Count);

            foreach (var emp in employees)
            {
                
                var empId = emp.EmployeeId;

                if (!colByEmpId.TryGetValue(empId, out var colName))
                    continue; 

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

                    
                    if (string.IsNullOrWhiteSpace(raw) || raw == ScheduleMatrixConstants.EmptyMark)
                    {
                        dayCells.Add(new SummaryDayCell());
                        continue;
                    }

                    
                    
                    if (raw.IndexOf(':') < 0)
                    {
                        
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
                        
                        dayCells.Add(new SummaryDayCell(raw, "", ""));
                    }
                }

                var sumText = FormatTimeSpanToSummary(sum);

                var workDays = CountWorkDays(dayCells);
                var freeDays = Math.Max(0, daysInMonth - workDays);

                resultRows.Add(new SummaryEmployeeRow(
                    employee: displayName,
                    workDays: workDays,
                    freeDays: freeDays,
                    sum: sumText,
                    days: dayCells));

            }

            return (headers, resultRows);
        }

        private static string FormatTimeSpanToSummary(TimeSpan ts)
        {
            var totalMinutes = (int)Math.Round(ts.TotalMinutes);
            if (totalMinutes <= 0) return "0";

            var h = totalMinutes / 60;
            var m = totalMinutes % 60;

            return m == 0
                ? h.ToString(CultureInfo.InvariantCulture)
                : $"{h}h {m}m";
        }

        
        
        
        
        
        
        
        
        
        
        
        
        private static bool TryParseTimeRanges(string text, out string from, out string to, out TimeSpan duration)
        {
            from = "";
            to = "";
            duration = TimeSpan.Zero;

            var matches = TimeRegex.Matches(text);
            if (matches.Count < 2)
                return false;

            
            var times = new List<TimeSpan>(matches.Count);

            foreach (Match m in matches)
            {
                
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

            
            for (var i = 0; i + 1 < times.Count; i += 2)
            {
                var delta = times[i + 1] - times[i];
                if (delta > TimeSpan.Zero)
                    duration += delta;
            }

            
            if (duration == TimeSpan.Zero)
            {
                var delta = times[^1] - times[0];
                if (delta > TimeSpan.Zero)
                    duration = delta;
            }

            return true;
        }

        
        
        
        
        
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

        private CancellationToken ResetExportUiCts(CancellationToken outer)
        {
            _exportUiCts?.Cancel();
            _exportUiCts?.Dispose();
            _exportUiCts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            return _exportUiCts.Token;
        }

        private Task ShowExportWorkingAsync()
        {
            return _owner.RunOnUiThreadAsync(() =>
            {
                ExportStatus = UIStatusKind.Working;
                IsExportStatusVisible = true;
            });
        }

        private Task HideExportStatusAsync()
        {
            return _owner.RunOnUiThreadAsync(() => IsExportStatusVisible = false);
        }

        private async Task ShowExportSuccessThenAutoHideAsync(CancellationToken ct, int milliseconds = 1400)
        {
            await _owner.RunOnUiThreadAsync(() =>
            {
                ExportStatus = UIStatusKind.Success;
                IsExportStatusVisible = true;
            }).ConfigureAwait(false);

            try
            {
                await Task.Delay(milliseconds, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await HideExportStatusAsync().ConfigureAwait(false);
        }


        
        
        
        
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

        
        
        
        private static string? TryGetString(object obj, string propertyName)
        {
            var p = obj.GetType().GetProperty(propertyName);
            if (p?.GetValue(obj) is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();

            return null;
        }

            
            
            
            
        private static string GetEmployeeDisplayName(ScheduleEmployeeModel emp)
        {
            if (emp is null)
                return string.Empty;

            
            if (TryGetFirstLast(emp, out var fullName))
                return fullName;

            
            var empProp = emp.GetType().GetProperty("Employee");
            if (empProp?.GetValue(emp) is object empObj)
            {
                if (TryGetFirstLast(empObj, out fullName))
                    return fullName;

                
                var nested = TryGetString(empObj, "FullName")
                          ?? TryGetString(empObj, "EmployeeName")
                          ?? TryGetString(empObj, "DisplayName")
                          ?? TryGetString(empObj, "Name");

                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }

            
            var direct = TryGetString(emp, "FullName")
                      ?? TryGetString(emp, "EmployeeName")
                      ?? TryGetString(emp, "DisplayName")
                      ?? TryGetString(emp, "Name");

            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            
            return $"Employee {emp.EmployeeId}";
        }


        
        
        

        
        
        
        
        private sealed class BuildResult
        {
            /// <summary>
            /// Визначає публічний елемент `public DataView View { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public DataView View { get; }
            /// <summary>
            /// Визначає публічний елемент `public Dictionary<string, int> ColMap { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public Dictionary<string, int> ColMap { get; }
            /// <summary>
            /// Визначає публічний елемент `public IList<ScheduleCellStyleModel> Styles { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public IList<ScheduleCellStyleModel> Styles { get; }
            /// <summary>
            /// Визначає публічний елемент `public int TotalEmployees { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int TotalEmployees { get; }
            /// <summary>
            /// Визначає публічний елемент `public string TotalHoursText { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string TotalHoursText { get; }
            /// <summary>
            /// Визначає публічний елемент `public string TotalEmployeesListText { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string TotalEmployeesListText { get; }
            /// <summary>
            /// Визначає публічний елемент `public Dictionary<int, string> PerEmployeeText { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public Dictionary<int, string> PerEmployeeText { get; }
            /// <summary>
            /// Визначає публічний елемент `public List<SummaryDayHeader> SummaryHeaders { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public List<SummaryDayHeader> SummaryHeaders { get; }
            /// <summary>
            /// Визначає публічний елемент `public List<SummaryEmployeeRow> SummaryRows { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public List<SummaryEmployeeRow> SummaryRows { get; }

            /// <summary>
            /// Визначає публічний елемент `public BuildResult(` та контракт його використання у шарі WPFApp.
            /// </summary>
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
