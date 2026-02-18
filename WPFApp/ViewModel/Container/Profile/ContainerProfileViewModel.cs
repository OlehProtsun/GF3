using DataAccessLayer.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.ScheduleMatrix;
using WPFApp.Service;
using WPFApp.ViewModel.Container.ScheduleProfile;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using System.Windows.Media;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.List;
using WPFApp.ViewModel.Container.ScheduleList;
using WPFApp.View.Dialogs;


namespace WPFApp.ViewModel.Container.Profile
{
    public sealed class ContainerProfileViewModel : ViewModelBase
    {
        private readonly ContainerViewModel _owner;

        // ===================== Profile fields =====================

        private bool _isExportStatusVisible;
        public bool IsExportStatusVisible
        {
            get => _isExportStatusVisible;
            private set => SetProperty(ref _isExportStatusVisible, value);
        }

        private UIStatusKind _exportStatus = UIStatusKind.Success;
        public UIStatusKind ExportStatus
        {
            get => _exportStatus;
            private set => SetProperty(ref _exportStatus, value);
        }

        private CancellationTokenSource? _exportUiCts;


        private int _containerId;
        public int ContainerId
        {
            get => _containerId;
            private set => SetProperty(ref _containerId, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        private string? _note;
        public string? Note
        {
            get => _note;
            private set => SetProperty(ref _note, value);
        }

        // ===================== Nested schedule list =====================
        public ContainerScheduleListViewModel ScheduleListVm { get; }

        private ContainerModel? _currentContainer;

        // ===================== Commands =====================
        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand ExportToExcelCommand { get; }
        public AsyncRelayCommand ExportToCodeCommand { get; }

        // ===================== OPTIONAL loaders (кращий шлях для точних даних) =====================
        // Якщо ScheduleListVm.Items не містить employees/slots, то встанови ці делегати з owner/repo
        // і статистика сама підтягне деталі по кожному schedule.
        public Func<int, CancellationToken, Task<IReadOnlyList<ScheduleEmployeeModel>>>? EmployeesLoader { get; set; }
        public Func<int, CancellationToken, Task<IReadOnlyList<ScheduleSlotModel>>>? SlotsLoader { get; set; }

        // ===================== Container Statistic: Top fields =====================
        private int _cellStyleRevision;
        public int CellStyleRevision
        {
            get => _cellStyleRevision;
            private set => SetProperty(ref _cellStyleRevision, value);
        }


        private int _totalEmployees;
        public int TotalEmployees
        {
            get => _totalEmployees;
            private set => SetProperty(ref _totalEmployees, value);
        }

        private int _totalShops;
        public int TotalShops
        {
            get => _totalShops;
            private set => SetProperty(ref _totalShops, value);
        }

        private string _totalEmployeesListText = string.Empty;
        public string TotalEmployeesListText
        {
            get => _totalEmployeesListText;
            private set => SetProperty(ref _totalEmployeesListText, value);
        }

        private string _totalShopsListText = string.Empty;
        public string TotalShopsListText
        {
            get => _totalShopsListText;
            private set => SetProperty(ref _totalShopsListText, value);
        }

        private string _totalHoursText = "0h 0m";
        public string TotalHoursText
        {
            get => _totalHoursText;
            private set => SetProperty(ref _totalHoursText, value);
        }

        // ===================== Grid #1: Employee | HoursSum | Shop1 | Shop2 | ... =====================
        public sealed class ShopHeader
        {
            public string Key { get; }   // stable dictionary key
            public string Name { get; }  // shown in header

            public ShopHeader(string key, string name)
            {
                Key = key ?? string.Empty;
                Name = name ?? string.Empty;
            }
        }

        public ObservableCollection<ShopHeader> ShopHeaders { get; } = new();

        public sealed class EmployeeShopHoursRow
        {
            public string Employee { get; }
            public int WorkDays { get; }
            public int FreeDays { get; }
            public string HoursSum { get; }

            // Key = ShopHeader.Key, Value = formatted hours cell
            public Dictionary<string, string> HoursByShop { get; }

            public EmployeeShopHoursRow(
                string employee,
                int workDays,
                int freeDays,
                string hoursSum,
                Dictionary<string, string> hoursByShop)
            {
                Employee = employee ?? string.Empty;
                WorkDays = workDays;
                FreeDays = freeDays;
                HoursSum = hoursSum ?? "0";
                HoursByShop = hoursByShop ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }

            // ✅ SAFE INDEXER
            public string this[string shopKey]
                => (shopKey != null && HoursByShop != null && HoursByShop.TryGetValue(shopKey, out var v))
                    ? (v ?? string.Empty)
                    : string.Empty;
        }



        public ObservableCollection<EmployeeShopHoursRow> EmployeeShopHoursRows { get; } = new();

        // ===================== Grid #2: Employee | WorkDays | FreeDays =====================
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

        public ObservableCollection<EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; } = new();

        // Notify View to rebuild dynamic columns
        public event EventHandler? StatisticsChanged;

        // ===================== background guards =====================
        private CancellationTokenSource? _statsCts;
        private int _statsVersion;

        // ===================== Auto refresh hooks =====================
        private bool _autoHooked;
        private int _autoQueued;

        // ===================== Helpers =====================
        private static readonly Regex SummaryHoursRegex =
            new(@"^\s*(?:(\d+)\s*h)?\s*(?:(\d+)\s*m)?\s*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public ContainerProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            ScheduleListVm = new ContainerScheduleListViewModel(owner);

            // warnings-free async lambdas
            BackCommand = new AsyncRelayCommand(async () => await _owner.CancelAsync());
            CancelProfileCommand = BackCommand;

            EditCommand = new AsyncRelayCommand(async () => await _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(async () => await _owner.DeleteSelectedAsync());
            ExportToExcelCommand = new AsyncRelayCommand(ExportToExcelAsync, CanExport);
            ExportToCodeCommand = new AsyncRelayCommand(ExportToCodeAsync, CanExport);

            HookScheduleListForAutoStatistics();
        }

        private bool CanExport()
            => _currentContainer != null && ScheduleListVm.Items.Count > 0;

        public void SetProfile(ContainerModel model)
        {
            IsExportStatusVisible = false;
            ExportStatus = UIStatusKind.Success;

            _currentContainer = model;
            ContainerId = model.Id;
            Name = model.Name;
            Note = model.Note;

            // якщо schedules вже завантажені — пробуємо порахувати
            ExportToExcelCommand.RaiseCanExecuteChanged();
            ExportToCodeCommand.RaiseCanExecuteChanged();
            _ = QueueAutoStatsRebuildAsync();
        }

        // =====================================================================
        // PUBLIC API: sources (schedule + employees + slots)
        // =====================================================================

        public sealed class ScheduleStatsSource
        {
            public ScheduleModel Schedule { get; }
            public IReadOnlyList<ScheduleEmployeeModel> Employees { get; }
            public IReadOnlyList<ScheduleSlotModel> Slots { get; }

            public ScheduleStatsSource(
                ScheduleModel schedule,
                IEnumerable<ScheduleEmployeeModel>? employees,
                IEnumerable<ScheduleSlotModel>? slots)
            {
                Schedule = schedule;

                // snapshot -> IReadOnlyList (прибирає CS1503 з Calculate)
                Employees = employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                Slots = slots?.ToList() ?? new List<ScheduleSlotModel>();
            }
        }

        /// <summary>
        /// Ядро підрахунку: приймає повні дані по кожному schedule.
        /// </summary>
        public async Task SetStatisticsAsync(IList<ScheduleStatsSource>? sources, CancellationToken ct = default)
        {
            sources ??= Array.Empty<ScheduleStatsSource>();

            _statsCts?.Cancel();
            _statsCts?.Dispose();

            var localCts = _statsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = localCts.Token;
            var version = Interlocked.Increment(ref _statsVersion);

            await _owner.RunOnUiThreadAsync(() =>
            {
                TotalEmployees = 0;
                TotalShops = 0;
                TotalHoursText = "0h 0m";
                TotalEmployeesListText = string.Empty;
                TotalShopsListText = string.Empty;

                ShopHeaders.Clear();
                EmployeeShopHoursRows.Clear();
                EmployeeWorkFreeStats.Clear();

                StatisticsChanged?.Invoke(this, EventArgs.Empty);
            }).ConfigureAwait(false);

            try
            {
                var built = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    // --------- Collect unique shops ----------
                    var shopKeyToName = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var s in sources)
                    {
                        var (shopKey, shopName) = GetShopKeyName(s.Schedule);
                        if (!shopKeyToName.ContainsKey(shopKey))
                            shopKeyToName[shopKey] = shopName;
                    }

                    // --------- Accumulators ----------
                    var empIdToName = new Dictionary<int, string>();
                    var empShopDur = new Dictionary<int, Dictionary<string, TimeSpan>>();   // empId -> (shopKey -> duration)
                    var empTotalDur = new Dictionary<int, TimeSpan>();                      // empId -> total duration
                    var empWorkDays = new Dictionary<int, int>();
                    var empFreeDays = new Dictionary<int, int>();
                    // ✅ totals only per shop (shopKey -> duration)
                    var shopTotalDur = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);


                    TimeSpan totalAll = TimeSpan.Zero;

                    // --------- Per schedule ----------
                    foreach (var src in sources)
                    {
                        token.ThrowIfCancellationRequested();

                        var schedule = src.Schedule;
                        var (shopKey, _) = GetShopKeyName(schedule);

                        // totals from existing calculator
                        var totals = ScheduleTotalsCalculator.Calculate(src.Employees, src.Slots);

                        totalAll += totals.TotalDuration;

                        // employee names (best-effort)
                        foreach (var emp in src.Employees)
                        {
                            var id = emp.EmployeeId;
                            if (!empIdToName.ContainsKey(id))
                                empIdToName[id] = GetEmployeeDisplayName(emp);
                        }

                        // per-employee duration -> add to pivot accumulators
                        foreach (var kv in totals.PerEmployeeDuration)
                        {
                            var empId = kv.Key;
                            var dur = kv.Value;

                            if (!empTotalDur.TryGetValue(empId, out var curT))
                                curT = TimeSpan.Zero;
                            empTotalDur[empId] = curT + dur;

                            if (!empShopDur.TryGetValue(empId, out var shopMap))
                            {
                                shopMap = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);
                                empShopDur[empId] = shopMap;
                            }

                            shopMap.TryGetValue(shopKey, out var curS);
                            shopMap[shopKey] = curS + dur;

                            // ✅ accumulate total per shop
                            shopTotalDur.TryGetValue(shopKey, out var curShopTotal);
                            shopTotalDur[shopKey] = curShopTotal + dur;
                        }


                        // Work/Free days (sum across schedules)
                        var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);

                        // slots -> empId -> distinct day set
                        var workedDaysByEmp = new Dictionary<int, HashSet<int>>();
                        foreach (var slot in src.Slots)
                        {
                            if (!TryGetSlotEmployeeId(slot, out var empId))
                                continue;

                            if (!TryGetSlotDay(slot, out var day))
                                continue;

                            if (day < 1 || day > daysInMonth)
                                continue;

                            if (!workedDaysByEmp.TryGetValue(empId, out var set))
                            {
                                set = new HashSet<int>();
                                workedDaysByEmp[empId] = set;
                            }
                            set.Add(day);
                        }

                        // apply per employee in this schedule
                        foreach (var empId in workedDaysByEmp.Keys)
                        {
                            var wd = workedDaysByEmp[empId].Count;
                            var fd = Math.Max(0, daysInMonth - wd);

                            empWorkDays.TryGetValue(empId, out var curWd);
                            empFreeDays.TryGetValue(empId, out var curFd);

                            empWorkDays[empId] = curWd + wd;
                            empFreeDays[empId] = curFd + fd;

                            // ensure name exists if came only from slots
                            if (!empIdToName.ContainsKey(empId))
                                empIdToName[empId] = $"Employee {empId}";
                        }
                    }

                    // Ensure all employees in duration map exist in name map
                    foreach (var empId in empTotalDur.Keys)
                        if (!empIdToName.ContainsKey(empId))
                            empIdToName[empId] = $"Employee {empId}";

                    // --------- Build ShopHeaders ----------
                    var shopHeaders = shopKeyToName
                        .Select(kv => new ShopHeader(kv.Key, kv.Value))
                        .OrderBy(h => h.Name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                    // --------- Build pivot rows ----------
                    var allShopKeys = shopHeaders.Select(h => h.Key).ToList();

                    var pivotRows = new List<EmployeeShopHoursRow>(empIdToName.Count + 1);

                    foreach (var emp in empIdToName.OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase))
                    {
                        token.ThrowIfCancellationRequested();

                        var empId = emp.Key;
                        var name = emp.Value;

                        empTotalDur.TryGetValue(empId, out var totalDur);

                        var byShopText = new Dictionary<string, string>(StringComparer.Ordinal);
                        foreach (var sk in allShopKeys)
                        {
                            TimeSpan d = TimeSpan.Zero;

                            if (empShopDur.TryGetValue(empId, out var map) && map.TryGetValue(sk, out var v))
                                d = v;

                            byShopText[sk] = FormatHoursCell(d);
                        }

                        empWorkDays.TryGetValue(empId, out var wd);
                        empFreeDays.TryGetValue(empId, out var fd);

                        pivotRows.Add(new EmployeeShopHoursRow(
                            employee: name,
                            workDays: wd,
                            freeDays: fd,
                            hoursSum: FormatHoursCell(totalDur),
                            hoursByShop: byShopText));
                    }

                    // ✅ TOTAL row: totals for ALL columns (додаємо 1 раз в самому кінці)
                    var totalWorkDays = empWorkDays.Values.Sum();
                    var totalFreeDays = empFreeDays.Values.Sum();

                    var totalsByShopText = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var sk in allShopKeys)
                    {
                        shopTotalDur.TryGetValue(sk, out var d);
                        totalsByShopText[sk] = FormatHoursCell(d);
                    }

                    pivotRows.Add(new EmployeeShopHoursRow(
                        employee: "TOTAL",
                        workDays: totalWorkDays,
                        freeDays: totalFreeDays,
                        hoursSum: FormatHoursCell(totalAll),
                        hoursByShop: totalsByShopText
                    ));



                    // --------- Work/Free rows ----------
                    var wfRows = new List<EmployeeWorkFreeStatRow>(empIdToName.Count);
                    foreach (var emp in empIdToName.OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase))
                    {
                        var empId = emp.Key;
                        empWorkDays.TryGetValue(empId, out var wd);
                        empFreeDays.TryGetValue(empId, out var fd);

                        wfRows.Add(new EmployeeWorkFreeStatRow(emp.Value, wd, fd));
                    }

                    var employeeNames = empIdToName
                        .OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase)
                        .Select(kv => kv.Value)
                        .ToList();

                    var shopNames = shopHeaders
                        .Select(h => h.Name)
                        .ToList();

                    return new BuildStatsResult(
                        totalHoursText: FormatHoursMinutes(totalAll),
                        totalEmployees: empIdToName.Count,
                        totalShops: shopHeaders.Count,
                        totalEmployeesListText: BuildPreviewList(employeeNames),
                        totalShopsListText: BuildPreviewList(shopNames),
                        shopHeaders: shopHeaders,
                        pivotRows: pivotRows,
                        workFreeRows: wfRows
                    );

                }, token).ConfigureAwait(false);

                if (token.IsCancellationRequested || version != _statsVersion)
                    return;

                await _owner.RunOnUiThreadAsync(() =>
                {
                    if (token.IsCancellationRequested || version != _statsVersion)
                        return;

                    TotalHoursText = built.TotalHoursText;
                    TotalEmployees = built.TotalEmployees;
                    TotalShops = built.TotalShops;
                    TotalEmployeesListText = built.TotalEmployeesListText;
                    TotalShopsListText = built.TotalShopsListText;

                    ShopHeaders.Clear();
                    foreach (var h in built.ShopHeaders)
                        ShopHeaders.Add(h);

                    EmployeeShopHoursRows.Clear();
                    foreach (var r in built.PivotRows)
                        EmployeeShopHoursRows.Add(r);

                    EmployeeWorkFreeStats.Clear();
                    foreach (var r in built.WorkFreeRows)
                        EmployeeWorkFreeStats.Add(r);

                    CellStyleRevision++;

                    StatisticsChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ok
            }
        }

        private CancellationToken ResetExportUiCts(CancellationToken outer)
        {
            _exportUiCts?.Cancel();
            _exportUiCts?.Dispose();
            _exportUiCts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            return _exportUiCts.Token;
        }

        private Task ShowExportWorkingAsync()
            => _owner.RunOnUiThreadAsync(() =>
            {
                ExportStatus = UIStatusKind.Working;
                IsExportStatusVisible = true;
            });

        private Task HideExportStatusAsync()
            => _owner.RunOnUiThreadAsync(() => IsExportStatusVisible = false);

        private async Task ShowExportSuccessThenAutoHideAsync(CancellationToken ct, int ms = 1400)
        {
            await _owner.RunOnUiThreadAsync(() =>
            {
                ExportStatus = UIStatusKind.Success;
                IsExportStatusVisible = true;
            }).ConfigureAwait(false);

            try { await Task.Delay(ms, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }

            await HideExportStatusAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Основна “корисна” функція: бере всі schedules, що зараз в ScheduleListVm.Items,
        /// підтягує employees/slots (через делегати або з об’єктів),
        /// і запускає SetStatisticsAsync.
        /// </summary>
        public async Task RefreshStatisticsFromContainerSchedulesAsync(CancellationToken ct = default)
        {
            var sources = await BuildSourcesFromScheduleListAsync(ct).ConfigureAwait(false);
            await SetStatisticsAsync(sources, ct).ConfigureAwait(false);
        }

        internal void CancelBackgroundWork()
        {
            _statsCts?.Cancel();
            _statsCts?.Dispose();
            _statsCts = null;
        }

        // ===================== DTO =====================
        private sealed class BuildStatsResult
        {
            public string TotalHoursText { get; }
            public int TotalEmployees { get; }
            public int TotalShops { get; }
            public string TotalEmployeesListText { get; }
            public string TotalShopsListText { get; }
            public List<ShopHeader> ShopHeaders { get; }
            public List<EmployeeShopHoursRow> PivotRows { get; }
            public List<EmployeeWorkFreeStatRow> WorkFreeRows { get; }

            public BuildStatsResult(string totalHoursText, int totalEmployees, int totalShops,
                                    string totalEmployeesListText, string totalShopsListText,
                                    List<ShopHeader> shopHeaders,
                                    List<EmployeeShopHoursRow> pivotRows,
                                    List<EmployeeWorkFreeStatRow> workFreeRows)
            {
                TotalHoursText = totalHoursText;
                TotalEmployees = totalEmployees;
                TotalShops = totalShops;
                TotalEmployeesListText = totalEmployeesListText;
                TotalShopsListText = totalShopsListText;
                ShopHeaders = shopHeaders;
                PivotRows = pivotRows;
                WorkFreeRows = workFreeRows;
            }
        }

        // ===================== Auto hook =====================
        private void HookScheduleListForAutoStatistics()
        {
            if (_autoHooked) return;
            _autoHooked = true;

            // якщо Items = ObservableCollection -> ловимо зміну
            if (ScheduleListVm.Items is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += (_, __) => _ = QueueAutoStatsRebuildAsync();
            }

            // перший запуск (на випадок якщо Items вже заповнений)
            ExportToExcelCommand.RaiseCanExecuteChanged();
            ExportToCodeCommand.RaiseCanExecuteChanged();
            _ = QueueAutoStatsRebuildAsync();
        }

        private async Task QueueAutoStatsRebuildAsync()
        {
            if (Interlocked.Exchange(ref _autoQueued, 1) == 1)
                return;

            try
            {
                await Task.Delay(120).ConfigureAwait(false);
                await RefreshStatisticsFromContainerSchedulesAsync().ConfigureAwait(false);
            }
            catch
            {
                // не валимо UI через background exceptions
            }
            finally
            {
                Interlocked.Exchange(ref _autoQueued, 0);
                ExportToExcelCommand.RaiseCanExecuteChanged();
                ExportToCodeCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task ExportToExcelAsync(CancellationToken ct)
        {
            if (_currentContainer is null)
            {
                _owner.ShowError("No container is selected for export.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Container to Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FileName = ScheduleExportService.SanitizeFileName($"{Name}.xlsx", "Container.xlsx")
            };

            if (dialog.ShowDialog() != true)
                return;

            var uiToken = ResetExportUiCts(ct);
            await ShowExportWorkingAsync().ConfigureAwait(false);

            try
            {
                // важке завантаження даних контейнера — тепер під Working
                var chartData = await BuildChartExportDataAsync(uiToken).ConfigureAwait(false);
                if (chartData.Count == 0)
                {
                    await HideExportStatusAsync().ConfigureAwait(false);
                    _owner.ShowError("No charts were found in this container.");
                    return;
                }

                var context = new ContainerExcelExportContext(
                    containerId: ContainerId,
                    containerName: Name,
                    containerNote: Note ?? string.Empty,
                    totalEmployees: TotalEmployees,
                    totalShops: TotalShops,
                    totalEmployeesListText: TotalEmployeesListText,
                    totalShopsListText: TotalShopsListText,
                    totalHoursText: TotalHoursText,
                    shopHeaders: ShopHeaders.ToList(),
                    employeeShopHoursRows: EmployeeShopHoursRows.ToList(),
                    employeeWorkFreeStats: EmployeeWorkFreeStats.ToList(),
                    charts: chartData.Select(x => x.Excel).ToList());

                await _owner.ExportContainerToExcelAsync(context, dialog.FileName, uiToken).ConfigureAwait(false);

                // Success toast + auto hide
                await ShowExportSuccessThenAutoHideAsync(uiToken, 1400).ConfigureAwait(false);

                // якщо хочеш ще й MessageBox — розкоментуй:
                // _owner.ShowInfo($"Excel export saved to:{Environment.NewLine}{dialog.FileName}");
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

        private async Task ExportToCodeAsync(CancellationToken ct)
        {
            if (_currentContainer is null)
            {
                _owner.ShowError("No container is selected for export.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Container to SQLite Script",
                Filter = "SQLite Script (*.sql)|*.sql|All Files (*.*)|*.*",
                FileName = ScheduleExportService.SanitizeFileName($"{Name}.sqlite.sql", "Container.sqlite.sql")
            };

            if (dialog.ShowDialog() != true)
                return;

            var uiToken = ResetExportUiCts(ct);
            await ShowExportWorkingAsync().ConfigureAwait(false);

            try
            {
                var chartData = await BuildChartExportDataAsync(uiToken).ConfigureAwait(false);
                if (chartData.Count == 0)
                {
                    await HideExportStatusAsync().ConfigureAwait(false);
                    _owner.ShowError("No charts were found in this container.");
                    return;
                }

                var context = new ContainerSqlExportContext(
                    container: _currentContainer,
                    charts: chartData.Select(x => x.Sql).ToList());

                await _owner.ExportContainerToSqlAsync(context, dialog.FileName, uiToken).ConfigureAwait(false);

                await ShowExportSuccessThenAutoHideAsync(uiToken, 1400).ConfigureAwait(false);

                // _owner.ShowInfo($"SQLite export saved to:{Environment.NewLine}{dialog.FileName}");
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

        private async Task<List<(ContainerExcelExportChartContext Excel, ContainerSqlExportScheduleContext Sql)>> BuildChartExportDataAsync(CancellationToken ct)
        {
            var result = new List<(ContainerExcelExportChartContext Excel, ContainerSqlExportScheduleContext Sql)>();
            var items = ScheduleListVm.Items?.ToList() ?? new List<ScheduleRowVm>();

            foreach (var row in items)
            {
                ct.ThrowIfCancellationRequested();

                var detailed = await _owner.LoadScheduleDetailsForExportAsync(row.Model.Id, ct).ConfigureAwait(false);
                if (detailed is null)
                    continue;

                var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
                var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

                var matrix = ScheduleMatrixEngine.BuildScheduleTable(
                    detailed.Year,
                    detailed.Month,
                    slots,
                    employees,
                    out var colMap,
                    ct);

                var totals = ScheduleTotalsCalculator.Calculate(employees, slots);
                var summary = BuildScheduleSummary(detailed, matrix, colMap, employees);

                var shop = detailed.Shop;
                var scheduleName = string.IsNullOrWhiteSpace(detailed.Name) ? $"Schedule {detailed.Id}" : detailed.Name;
                var shopName = shop?.Name ?? string.Empty;
                var employeeNames = employees
                    .Select(GetEmployeeDisplayName)
                    .Select(x => (x ?? string.Empty).Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                var scheduleExcel = new ScheduleExportContext(
                    scheduleName: scheduleName,
                    scheduleMonth: detailed.Month,
                    scheduleYear: detailed.Year,
                    shopName: shopName,
                    shopAddress: GetShopAddress(shop),
                    totalHoursText: FormatHoursMinutes(totals.TotalDuration),
                    totalEmployees: totals.TotalEmployees,
                    totalDays: DateTime.DaysInMonth(detailed.Year, detailed.Month),
                    shift1: detailed.Shift1Time ?? string.Empty,
                    shift2: detailed.Shift2Time ?? string.Empty,
                    totalEmployeesListText: BuildPreviewList(employeeNames),

                    scheduleMatrix: matrix.DefaultView,
                    summaryDayHeaders: summary.Headers,
                    summaryRows: summary.Rows,
                    employeeWorkFreeStats: BuildEmployeeWorkFreeStats(summary.Rows, detailed.Year, detailed.Month),
                    styleProvider: new NullScheduleMatrixStyleProvider());

                var availabilityData = await _owner.LoadAvailabilityGroupExportDataAsync(detailed.AvailabilityGroupId, ct).ConfigureAwait(false);
                var scheduleSql = new ScheduleSqlExportContext(detailed, employees, slots, cellStyles, availabilityData);

                result.Add((
                    new ContainerExcelExportChartContext(scheduleName, scheduleExcel),
                    new ContainerSqlExportScheduleContext(scheduleSql)));
            }

            return result;
        }

        private static (List<ContainerScheduleProfileViewModel.SummaryDayHeader> Headers, List<ContainerScheduleProfileViewModel.SummaryEmployeeRow> Rows) BuildScheduleSummary(
            ScheduleModel schedule,
            System.Data.DataTable matrix,
            Dictionary<string, int> colMap,
            IList<ScheduleEmployeeModel> employees)
        {
            var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);
            var rowsByDay = matrix.Rows.Cast<System.Data.DataRow>()
                .ToDictionary(r => Convert.ToInt32(r[ScheduleMatrixConstants.DayColumnName], CultureInfo.InvariantCulture), r => r);

            var headers = new List<ContainerScheduleProfileViewModel.SummaryDayHeader>(daysInMonth);
            for (var d = 1; d <= daysInMonth; d++)
            {
                var dt = new DateTime(schedule.Year, schedule.Month, d);
                headers.Add(new ContainerScheduleProfileViewModel.SummaryDayHeader(d, dt.ToString("dddd(dd.MM.yyyy)", CultureInfo.InvariantCulture)));
            }

            var colByEmpId = colMap.ToDictionary(kv => kv.Value, kv => kv.Key);
            var rows = new List<ContainerScheduleProfileViewModel.SummaryEmployeeRow>(employees.Count);

            foreach (var emp in employees)
            {
                if (!colByEmpId.TryGetValue(emp.EmployeeId, out var colName))
                    continue;

                var dayCells = new List<ContainerScheduleProfileViewModel.SummaryDayCell>(daysInMonth);
                var sum = TimeSpan.Zero;

                for (var d = 1; d <= daysInMonth; d++)
                {
                    if (!rowsByDay.TryGetValue(d, out var dr))
                    {
                        dayCells.Add(new ContainerScheduleProfileViewModel.SummaryDayCell());
                        continue;
                    }

                    var raw = dr[colName]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(raw) || raw == ScheduleMatrixConstants.EmptyMark)
                    {
                        dayCells.Add(new ContainerScheduleProfileViewModel.SummaryDayCell());
                        continue;
                    }

                    if (TryParseTimeRanges(raw, out var from, out var to, out var dur))
                    {
                        sum += dur;
                        dayCells.Add(new ContainerScheduleProfileViewModel.SummaryDayCell(from, to, FormatHoursCell(dur)));
                    }
                    else
                    {
                        dayCells.Add(new ContainerScheduleProfileViewModel.SummaryDayCell(raw, string.Empty, string.Empty));
                    }
                }

                var sumText = FormatHoursCell(sum); // "132" або "145h 30m" як у UI
                var workDays = dayCells.Count(c =>
                    !string.IsNullOrWhiteSpace(c.From) ||
                    !string.IsNullOrWhiteSpace(c.To) ||
                    (TryParseSummaryHoursToMinutes(c.Hours, out var mm) && mm > 0));
                var freeDays = Math.Max(0, daysInMonth - workDays);

                rows.Add(new ContainerScheduleProfileViewModel.SummaryEmployeeRow(
                    employee: GetEmployeeDisplayName(emp),
                    workDays: workDays,
                    freeDays: freeDays,
                    sum: sumText,
                    days: dayCells));
            }

            return (headers, rows);
        }

        private static List<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> BuildEmployeeWorkFreeStats(
            IList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> summaryRows,
            int year,
            int month)
        {
            var totalDays = DateTime.DaysInMonth(year, month);
            var list = new List<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow>(summaryRows.Count);

            foreach (var row in summaryRows)
            {
                var work = row.Days.Count(d => !string.IsNullOrWhiteSpace(d.From) || !string.IsNullOrWhiteSpace(d.To) || TryParseSummaryHoursToMinutes(d.Hours, out var m) && m > 0);
                var free = Math.Max(0, totalDays - work);
                list.Add(new ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow(row.Employee, work, free));
            }

            return list;
        }

        private static bool TryParseTimeRanges(string text, out string from, out string to, out TimeSpan duration)
        {
            from = string.Empty;
            to = string.Empty;
            duration = TimeSpan.Zero;

            var matches = Regex.Matches(text ?? string.Empty, @"\b([01]?\d|2[0-3]):[0-5]\d\b");

            if (matches.Count < 2)
                return false;

            var times = new List<TimeSpan>(matches.Count);
            foreach (Match m in matches)
            {
                if (TimeSpan.TryParseExact(m.Value, new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var t))
                    times.Add(t);
            }

            if (times.Count < 2)
                return false;

            from = times.First().ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            to = times.Last().ToString(@"hh\:mm", CultureInfo.InvariantCulture);

            for (var i = 0; i + 1 < times.Count; i += 2)
            {
                var start = times[i];
                var end = times[i + 1];
                if (end < start) end += TimeSpan.FromDays(1);
                duration += end - start;
            }

            return true;
        }

        private static bool TryParseSummaryHoursToMinutes(string? text, out int minutes)
        {
            minutes = 0;
            var s = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s) || s == "0") return true;

            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
            {
                minutes = Math.Max(0, h * 60);
                return true;
            }

            var m = SummaryHoursRegex.Match(s);
            if (!m.Success) return false;

            var hh = m.Groups[1].Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            var mm = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            minutes = Math.Max(0, hh * 60 + mm);
            return true;
        }

        private static string GetShopAddress(ShopModel? shop)
        {
            if (shop == null) return string.Empty;
            return shop.Address ?? string.Empty;
        }

        private sealed class NullScheduleMatrixStyleProvider : IScheduleMatrixStyleProvider
        {
            public int CellStyleRevision => 0;
            public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef) => null;
            public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef) => null;
            public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
            {
                cellRef = default;
                return false;
            }
            public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            {
                style = null!;
                return false;
            }
        }

        // ===================== Build sources from ScheduleListVm.Items =====================
        private async Task<List<ScheduleStatsSource>> BuildSourcesFromScheduleListAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var items = ScheduleListVm.Items?.ToList() ?? new List<ScheduleRowVm>();
            if (items.Count == 0)
                return new List<ScheduleStatsSource>();

            var sources = new List<ScheduleStatsSource>(items.Count);

            foreach (var row in items)
            {
                ct.ThrowIfCancellationRequested();

                if (!TryExtractScheduleModel(row, out var schedule))
                    continue;

                // 1) спроба взяти прямо з row або schedule (якщо там вже є повні дані)
                var employees = TryGetEnumerable<ScheduleEmployeeModel>(row, "Employees", "ScheduleEmployees")
                             ?? TryGetEnumerable<ScheduleEmployeeModel>(schedule, "Employees", "ScheduleEmployees");

                var slots = TryGetEnumerable<ScheduleSlotModel>(row, "Slots", "ScheduleSlots")
                         ?? TryGetEnumerable<ScheduleSlotModel>(schedule, "Slots", "ScheduleSlots");

                // 2) якщо немає — пробуємо підтягнути через loader (найкращий шлях)
                var scheduleId = schedule.Id;

                if ((employees == null || !employees.Any()) && EmployeesLoader != null)
                {
                    var loaded = await EmployeesLoader(scheduleId, ct).ConfigureAwait(false);
                    employees = loaded;
                }

                if ((slots == null || !slots.Any()) && SlotsLoader != null)
                {
                    var loaded = await SlotsLoader(scheduleId, ct).ConfigureAwait(false);
                    slots = loaded;
                }

                sources.Add(new ScheduleStatsSource(schedule, employees, slots));
            }

            return sources;
        }

        private static bool TryExtractScheduleModel(object row, out ScheduleModel schedule)
        {
            schedule = null!;

            if (row is ScheduleModel sm)
            {
                schedule = sm;
                return true;
            }

            // найчастіші варіанти: row.Schedule / row.Model / row.Source / row.Entity
            var possible = new[] { "Schedule", "Model", "Source", "Entity", "Data" };
            foreach (var name in possible)
            {
                var p = row.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (p?.GetValue(row) is ScheduleModel s2)
                {
                    schedule = s2;
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<T>? TryGetEnumerable<T>(object obj, params string[] propNames)
        {
            foreach (var prop in propNames)
            {
                var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
                if (p?.GetValue(obj) is IEnumerable<T> en)
                    return en;
            }
            return null;
        }

        // ===================== Formatting =====================
        private static string FormatHoursMinutes(TimeSpan ts)
        {
            var totalMinutes = (int)Math.Round(ts.TotalMinutes);
            if (totalMinutes < 0) totalMinutes = 0;

            var h = totalMinutes / 60;
            var m = totalMinutes % 60;

            return $"{h}h {m}m";
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

        // ===================== Shop helpers =====================
        private static (string Key, string Name) GetShopKeyName(ScheduleModel schedule)
        {
            var shop = schedule.Shop;

            var id = shop?.Id ?? 0;
            var name = shop?.Name ?? (id > 0 ? $"Shop {id}" : "Shop");

            var key = id > 0
                ? id.ToString(CultureInfo.InvariantCulture)
                : (name ?? "Shop");

            return (key, name);
        }

        // ===================== Slot reflection: EmployeeId + Day =====================
        private static bool TryGetSlotEmployeeId(ScheduleSlotModel slot, out int employeeId)
        {
            employeeId = 0;
            if (slot == null) return false;

            var t = slot.GetType();

            var p = t.GetProperty("EmployeeId") ?? t.GetProperty("EmpId") ?? t.GetProperty("WorkerId");
            if (p?.GetValue(slot) is int i && i > 0)
            {
                employeeId = i;
                return true;
            }

            var empProp = t.GetProperty("Employee");
            if (empProp?.GetValue(slot) is object empObj)
            {
                var ep = empObj.GetType().GetProperty("Id") ?? empObj.GetType().GetProperty("EmployeeId");
                if (ep?.GetValue(empObj) is int ei && ei > 0)
                {
                    employeeId = ei;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetSlotDay(ScheduleSlotModel slot, out int day)
        {
            day = 0;
            if (slot == null) return false;

            var t = slot.GetType();

            var p = t.GetProperty("Day") ?? t.GetProperty("DayOfMonth");
            if (p?.GetValue(slot) is int di && di > 0)
            {
                day = di;
                return true;
            }

            var dp = t.GetProperty("Date") ?? t.GetProperty("DayDate") ?? t.GetProperty("StartDate") ?? t.GetProperty("Start");
            var v = dp?.GetValue(slot);

            if (v is DateTime dt)
            {
                day = dt.Day;
                return true;
            }

            return false;
        }

        // ===================== Employee display name =====================
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
    }
}
