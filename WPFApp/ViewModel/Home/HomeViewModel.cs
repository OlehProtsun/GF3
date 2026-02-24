using BusinessLogicLayer.Services.Abstractions;
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
using System.Windows.Threading;
using WPFApp.Applications.Diagnostics;
using WPFApp.Applications.Matrix.Schedule;
using WPFApp.Applications.Notifications;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.UI.Helpers;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Container.Edit.Helpers; // UIStatusKind
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;


namespace WPFApp.ViewModel.Home
{
    /// <summary>
    /// HomeViewModel follows the same shell/module patterns used in the app:
    /// - async loading through AsyncRelayCommand + cancellation tokens
    /// - ObservableCollections for DataGrid sections
    /// - UI-friendly status/error properties (without blocking the UI thread)
    /// - periodic current-time updates via DispatcherTimer
    /// </summary>
    public sealed class HomeViewModel : ViewModelBase
    {
        private readonly IScheduleService _scheduleService;
        private readonly IContainerService _containerService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
        private readonly ILoggerService _logger;

        private readonly DispatcherTimer _clockTimer;
        private bool _initialized;
        private Task? _initializeTask;
        private readonly object _initLock = new();

        private string _currentTimeText = string.Empty;
        private string _statusText = "Loading home data...";
        private bool _isLoading;
        private int _monthSchedulesCount;
        private int _totalContainersCount;
        private int _todayAssignmentsCount;
        private int _activeShopsCount;

        public HomeViewModel(
            IScheduleService scheduleService,
            IContainerService containerService,
            IDatabaseChangeNotifier databaseChangeNotifier,
            ILoggerService logger)
        {
            _scheduleService = scheduleService;
            _containerService = containerService;
            _databaseChangeNotifier = databaseChangeNotifier;
            _logger = logger;

            WhoWorksTodayItems = new ObservableCollection<WhoWorksTodayRowViewModel>();
            ActiveSchedules = new ObservableCollection<HomeScheduleCardViewModel>();

            RefreshCommand = new AsyncRelayCommand(RefreshWithOverlayAsync, () => !IsLoading);

            _databaseChangeNotifier.DatabaseChanged += OnDatabaseChanged;

            _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _clockTimer.Tick += (_, __) => UpdateCurrentTimeText();

            UpdateCurrentTimeText();
            _clockTimer.Start();

            _ = EnsureInitializedAsync();
        }

        public ObservableCollection<WhoWorksTodayRowViewModel> WhoWorksTodayItems { get; }

        public ObservableCollection<HomeScheduleCardViewModel> ActiveSchedules { get; }

        public AsyncRelayCommand RefreshCommand { get; }

        private string _currentMonthContainerName = "-";
        private string _currentMonthLabel = "-";
        private ObservableCollection<string> _currentMonthScheduleNames = new();

        private int _currentMonthTotalEmployees;
        private int _currentMonthTotalSchedules;
        private double _currentMonthTotalHours;
        private int _currentMonthTotalShops;

        private int _overallTotalEmployees;
        private int _overallTotalContainers;
        private int _overallTotalShops;

        public string CurrentMonthContainerName
        {
            get => _currentMonthContainerName;
            private set => SetProperty(ref _currentMonthContainerName, value);
        }

        public string CurrentMonthLabel
        {
            get => _currentMonthLabel;
            private set => SetProperty(ref _currentMonthLabel, value);
        }

        public ObservableCollection<string> CurrentMonthScheduleNames
        {
            get => _currentMonthScheduleNames;
            private set => SetProperty(ref _currentMonthScheduleNames, value);
        }

        public int CurrentMonthTotalEmployees
        {
            get => _currentMonthTotalEmployees;
            private set => SetProperty(ref _currentMonthTotalEmployees, value);
        }

        public int CurrentMonthTotalSchedules
        {
            get => _currentMonthTotalSchedules;
            private set => SetProperty(ref _currentMonthTotalSchedules, value);
        }

        public double CurrentMonthTotalHours
        {
            get => _currentMonthTotalHours;
            private set => SetProperty(ref _currentMonthTotalHours, value);
        }

        public int CurrentMonthTotalShops
        {
            get => _currentMonthTotalShops;
            private set => SetProperty(ref _currentMonthTotalShops, value);
        }

        public int OverallTotalEmployees
        {
            get => _overallTotalEmployees;
            private set => SetProperty(ref _overallTotalEmployees, value);
        }

        public int OverallTotalContainers
        {
            get => _overallTotalContainers;
            private set => SetProperty(ref _overallTotalContainers, value);
        }

        public int OverallTotalShops
        {
            get => _overallTotalShops;
            private set => SetProperty(ref _overallTotalShops, value);
        }

        public string CurrentTimeText
        {
            get => _currentTimeText;
            private set => SetProperty(ref _currentTimeText, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        private ObservableCollection<string> _currentMonthShopNames = new();
        public ObservableCollection<string> CurrentMonthShopNames
        {
            get => _currentMonthShopNames;
            private set => SetProperty(ref _currentMonthShopNames, value);
        }

        private ObservableCollection<string> _currentMonthEmployeeNames = new();
        public ObservableCollection<string> CurrentMonthEmployeeNames
        {
            get => _currentMonthEmployeeNames;
            private set => SetProperty(ref _currentMonthEmployeeNames, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                    RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        public int MonthSchedulesCount
        {
            get => _monthSchedulesCount;
            private set => SetProperty(ref _monthSchedulesCount, value);
        }

        public int TotalContainersCount
        {
            get => _totalContainersCount;
            private set => SetProperty(ref _totalContainersCount, value);
        }

        public int TodayAssignmentsCount
        {
            get => _todayAssignmentsCount;
            private set => SetProperty(ref _todayAssignmentsCount, value);
        }

        public int ActiveShopsCount
        {
            get => _activeShopsCount;
            private set => SetProperty(ref _activeShopsCount, value);
        }

        public Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_initialized)
                return Task.CompletedTask;

            if (_initializeTask != null)
                return _initializeTask;

            lock (_initLock)
            {
                if (_initialized)
                    return Task.CompletedTask;

                if (_initializeTask != null)
                    return _initializeTask;

                _initializeTask = LoadDataAsync(ct);
                return _initializeTask;
            }
        }

        // ---- Overlay (Working / Success) for manual refresh
        private bool _isNavStatusVisible;
        public bool IsNavStatusVisible
        {
            get => _isNavStatusVisible;
            private set => SetProperty(ref _isNavStatusVisible, value);
        }

        private UIStatusKind _navStatus = UIStatusKind.Success;
        public UIStatusKind NavStatus
        {
            get => _navStatus;
            private set => SetProperty(ref _navStatus, value);
        }

        private CancellationTokenSource? _navUiCts;
        private bool _lastLoadSuccessful;

        private CancellationToken ResetNavUiCts(CancellationToken outer)
        {
            _navUiCts?.Cancel();
            _navUiCts?.Dispose();
            _navUiCts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            return _navUiCts.Token;
        }

        private Task RunOnUiThreadAsync(Action a)
        {
            var d = Application.Current?.Dispatcher;
            if (d == null || d.CheckAccess())
            {
                a();
                return Task.CompletedTask;
            }
            return d.InvokeAsync(a).Task;
        }

        private Task ShowNavWorkingAsync()
            => RunOnUiThreadAsync(() =>
            {
                NavStatus = UIStatusKind.Working;
                IsNavStatusVisible = true;
            });

        private Task HideNavStatusAsync()
            => RunOnUiThreadAsync(() => IsNavStatusVisible = false);

        private async Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 700)
        {
            await RunOnUiThreadAsync(() =>
            {
                NavStatus = UIStatusKind.Success;
                IsNavStatusVisible = true;
            });

            try { await Task.Delay(ms, ct); }
            catch (OperationCanceledException) { return; }

            await HideNavStatusAsync();
        }

        private async Task LoadDataAsync(CancellationToken ct)
        {
            void UI(Action a)
            {
                var d = App.Current?.Dispatcher;
                if (d == null || d.CheckAccess()) a();
                else d.Invoke(a);
            }

            UI(() =>
            {
                IsLoading = true;
                StatusText = "Loading home data...";
            });

            _lastLoadSuccessful = false;


            // Helper: parse "08:00", "8:00", "08:00:00" (and tolerant formats)
            static bool TryParseTime(string? value, out TimeSpan ts)
            {
                ts = default;
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                value = value.Trim();

                if (TimeSpan.TryParse(value, out ts))
                    return true;

                if (TimeSpan.TryParseExact(value, @"hh\:mm\:ss", null, out ts))
                    return true;

                if (TimeSpan.TryParseExact(value, @"h\:mm", null, out ts))
                    return true;

                if (TimeSpan.TryParseExact(value, @"hh\:mm", null, out ts))
                    return true;

                return false;
            }

            static object? GetObjProp(object obj, params string[] names)
            {
                var t = obj.GetType();
                foreach (var n in names)
                {
                    var p = t.GetProperty(n);
                    if (p == null) continue;
                    return p.GetValue(obj);
                }
                return null;
            }

            static string? ReadString(object obj, params string[] names)
            {
                foreach (var n in names)
                {
                    var v = GetObjProp(obj, n);
                    if (v is string s && !string.IsNullOrWhiteSpace(s))
                        return s.Trim();
                }
                return null;
            }

            static int ReadInt(object obj, params string[] names)
            {
                foreach (var n in names)
                {
                    var v = GetObjProp(obj, n);
                    if (v == null) continue;

                    if (v is int i) return i;
                    if (v is long l) return (int)l;
                    if (v is short sh) return sh;
                    if (v is byte b) return b;

                    if (v.GetType().IsEnum)
                        return Convert.ToInt32(v);

                    if (v is string s && int.TryParse(s, out var parsed))
                        return parsed;

                    try { return Convert.ToInt32(v); }
                    catch { /* ignore */ }
                }
                return 0;
            }

            static bool ReadBool(object obj, params string[] names)
            {
                foreach (var n in names)
                {
                    var v = GetObjProp(obj, n);
                    if (v == null) continue;

                    if (v is bool b) return b;
                    if (v is int i) return i != 0;

                    if (v is string s)
                    {
                        if (bool.TryParse(s, out var bb)) return bb;
                        if (int.TryParse(s, out var ii)) return ii != 0;
                    }
                }
                return false;
            }

            static DateTime? ReadDate(object obj, params string[] names)
            {
                foreach (var n in names)
                {
                    var v = GetObjProp(obj, n);
                    if (v == null) continue;

                    if (v is DateTime dt) return dt;
                    if (v is DateTimeOffset dto) return dto.DateTime;
                    if (v is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);

                    if (v is string s)
                    {
                        s = s.Trim();
                        if (DateTime.TryParse(s, out var parsed)) return parsed;

                        if (DateTime.TryParseExact(
                                s,
                                new[] { "MM.yyyy", "M.yyyy" },
                                null,
                                DateTimeStyles.None,
                                out parsed))
                            return parsed;
                    }
                }
                return null;
            }

            // Extract employees snapshot (prefer detailed.Employees if exists; otherwise derive from slots)
            static List<ScheduleEmployeeModel> ExtractEmployeesSnapshot(object detailed, List<ScheduleSlotModel> slotsSnapshot)
            {
                var empObj = GetObjProp(detailed, "Employees", "ScheduleEmployees", "Staff");
                if (empObj is IEnumerable<ScheduleEmployeeModel> empList)
                {
                    return empList
                        .Where(e => e != null)
                        .GroupBy(e => e.EmployeeId)
                        .Select(g => g.First())
                        .ToList();
                }

                // fallback: derive from slots
                var derived = slotsSnapshot
                    .Where(s => s.EmployeeId.HasValue && s.EmployeeId.Value > 0)
                    .GroupBy(s => s.EmployeeId!.Value)
                    .Select(g =>
                    {
                        var emp = g.Select(x => x.Employee).FirstOrDefault(x => x != null);
                        return new ScheduleEmployeeModel
                        {
                            EmployeeId = g.Key,
                            Employee = emp
                        };
                    })
                    .ToList();

                return derived;
            }

            // Extract cell styles snapshot (if service returns them in detailed)
            static List<ScheduleCellStyleModel> ExtractStylesSnapshot(object detailed)
            {
                var stObj = GetObjProp(detailed, "CellStyles", "ScheduleCellStyles", "Styles");
                if (stObj is IEnumerable<ScheduleCellStyleModel> stList)
                    return stList.Where(s => s != null).ToList();

                return new List<ScheduleCellStyleModel>();
            }

            try
            {
                var now = DateTime.Now;

                // Load base lists
                var schedules = await _scheduleService.GetAllAsync(ct).ConfigureAwait(false);

                var monthSchedules = schedules
                    .Where(s => s.Year == now.Year && s.Month == now.Month)
                    .OrderBy(s => s.Shop?.Name)
                    .ThenBy(s => s.Name)
                    .ToList();

                var containers = (await _containerService.GetAllAsync(ct).ConfigureAwait(false)).ToList();

                var todayRows = new List<WhoWorksTodayRowViewModel>();
                var scheduleCards = new List<HomeScheduleCardViewModel>();

                // ---- Information aggregates (This month)
                var monthEmployeeIds = new HashSet<int>();
                var monthEmployeeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var monthShopIds = new HashSet<int>();
                var monthShopNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var monthScheduleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                double monthTotalHours = 0;

                foreach (var schedule in monthSchedules)
                {
                    ct.ThrowIfCancellationRequested();

                    var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct).ConfigureAwait(false);
                    if (detailed is null)
                        continue;

                    var year = ReadInt(detailed, "Year") != 0 ? ReadInt(detailed, "Year") : now.Year;
                    var month = ReadInt(detailed, "Month") != 0 ? ReadInt(detailed, "Month") : now.Month;

                    var slotsSnapshot = (detailed.Slots ?? new List<ScheduleSlotModel>()).ToList();
                    var employeesSnapshot = ExtractEmployeesSnapshot(detailed, slotsSnapshot);
                    var stylesSnapshot = ExtractStylesSnapshot(detailed);

                    // Schedule name list
                    if (!string.IsNullOrWhiteSpace(detailed.Name))
                        monthScheduleNames.Add(detailed.Name);

                    // ShopId
                    if (detailed.ShopId > 0)
                        monthShopIds.Add(detailed.ShopId);

                    // Shop name list
                    var shopName = detailed.Shop?.Name;
                    if (!string.IsNullOrWhiteSpace(shopName))
                        monthShopNames.Add(shopName);

                    // ---- Who works today
                    if (year == now.Year && month == now.Month)
                    {
                        var todaySlots = slotsSnapshot
                            .Where(slot => slot.DayOfMonth == now.Day && slot.EmployeeId.HasValue)
                            .OrderBy(slot => slot.FromTime)
                            .ThenBy(slot => slot.Employee?.LastName)
                            .ThenBy(slot => slot.Employee?.FirstName)
                            .ToList();

                        foreach (var slot in todaySlots)
                        {
                            var employee = slot.Employee;
                            if (employee == null)
                                continue;

                            todayRows.Add(new WhoWorksTodayRowViewModel
                            {
                                Date = new DateTime(now.Year, now.Month, slot.DayOfMonth),
                                Employee = $"{employee.FirstName} {employee.LastName}".Trim(),
                                Shift = $"{slot.FromTime} - {slot.ToTime}",
                                Shop = detailed.Shop?.Name ?? "-"
                            });
                        }
                    }

                    // ---- Month totals (employees + hours)
                    foreach (var slot in slotsSnapshot.Where(s => s.EmployeeId.HasValue))
                    {
                        monthEmployeeIds.Add(slot.EmployeeId!.Value);

                        if (slot.Employee != null)
                        {
                            var nameKey = $"{slot.Employee.FirstName} {slot.Employee.LastName}".Trim();
                            if (!string.IsNullOrWhiteSpace(nameKey))
                                monthEmployeeNames.Add(nameKey);
                        }

                        if (TryParseTime(slot.FromTime, out var fromTs) &&
                            TryParseTime(slot.ToTime, out var toTs))
                        {
                            var delta = toTs - fromTs;
                            if (delta.TotalMinutes < 0)
                                delta = delta.Add(TimeSpan.FromDays(1));

                            if (delta.TotalMinutes > 0)
                                monthTotalHours += delta.TotalHours;
                        }
                    }

                    // ---- Active schedule card matrix (IDENTICAL data shape to ContainerScheduleProfile)
                    var table = ScheduleMatrixEngine.BuildScheduleTable(
                        year,
                        month,
                        slotsSnapshot,
                        employeesSnapshot,
                        out var colMap,
                        ct);

                    // recompute conflicts WITH staffing (same as ContainerScheduleProfileViewModel)
                    var peoplePerShift = ReadInt(detailed, "PeoplePerShift", "MinPeoplePerShift", "StaffPerShift");
                    var shift1Time = ReadString(detailed, "Shift1Time", "Shift1");
                    var shift2Time = ReadString(detailed, "Shift2Time", "Shift2");

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

                    // totals for header tooltips / second-line headers (optional but consistent)
                    var totals = ScheduleTotalsCalculator.Calculate(employeesSnapshot, slotsSnapshot);

                    var perEmpText = new Dictionary<int, string>(capacity: Math.Max(8, totals.TotalEmployees));
                    foreach (var emp in employeesSnapshot)
                    {
                        totals.PerEmployeeDuration.TryGetValue(emp.EmployeeId, out var empTotal);
                        perEmpText[emp.EmployeeId] = $"Total hours: {ScheduleTotalsCalculator.FormatHoursMinutes(empTotal)}";
                    }

                    var totalHoursText = ScheduleTotalsCalculator.FormatHoursMinutes(totals.TotalDuration);

                    var card = new HomeScheduleCardViewModel(
                        title: detailed.Name ?? "-",
                        shopName: detailed.Shop?.Name ?? "-",
                    monthLabel: new DateTime(year, month, 1).ToString("MM.yyyy", CultureInfo.InvariantCulture));

                    card.ApplyBuild(
                        view: table.DefaultView,
                        colMap: colMap,
                        styles: stylesSnapshot,
                        perEmployeeTotals: perEmpText,
                        totalHoursText: totalHoursText);

                    scheduleCards.Add(card);
                }

                // Deduplicate today
                var dedupedToday = todayRows
                    .GroupBy(r => new { r.Date, r.Employee, r.Shift, r.Shop })
                    .Select(g => g.First())
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Shift)
                    .ThenBy(r => r.Employee)
                    .ToList();

                // ---- Container name for current month
                string currentContainerName = "-";

                var monthContainerId = monthSchedules
                    .Select(s => ReadInt(s, "ContainerId", "ContainerID", "Container_Id"))
                    .FirstOrDefault(id => id > 0);

                ContainerModel? currentContainer = null;

                if (monthContainerId > 0)
                {
                    currentContainer = containers.FirstOrDefault(c =>
                        ReadInt(c, "Id", "ContainerId", "ContainerID") == monthContainerId);
                }

                currentContainer ??= containers.FirstOrDefault(c =>
                    ReadBool(c, "IsActive", "Active", "IsCurrent", "Current"));

                currentContainer ??= containers.FirstOrDefault(c =>
                {
                    var y = ReadInt(c, "Year", "ContainerYear", "ScheduleYear", "PeriodYear");
                    var m = ReadInt(c, "Month", "ContainerMonth", "ScheduleMonth", "PeriodMonth");
                    return y == now.Year && m == now.Month;
                });

                currentContainer ??= containers.FirstOrDefault(c =>
                {
                    var dt = ReadDate(c, "MonthYear", "MonthDate", "ForMonth", "Date", "StartDate", "CreatedAt", "CreatedOn");
                    return dt.HasValue && dt.Value.Year == now.Year && dt.Value.Month == now.Month;
                });

                currentContainer ??= containers
                    .OrderByDescending(c => ReadDate(c, "CreatedAt", "CreatedOn", "StartDate", "Date") ?? DateTime.MinValue)
                    .ThenByDescending(c => ReadInt(c, "Id", "ContainerId", "ContainerID"))
                    .FirstOrDefault();

                if (currentContainer != null)
                {
                    var name =
                        ReadString(currentContainer, "Name", "Title", "ContainerName", "Code", "Number")
                        ?? ReadString(GetObjProp(currentContainer, "Container") ?? currentContainer,
                                      "Name", "Title", "ContainerName", "Code", "Number");

                    if (!string.IsNullOrWhiteSpace(name))
                        currentContainerName = name!;
                }

                // Overall
                var overallTotalContainers = containers.Count;

                var overallTotalShops = schedules
                    .Select(s => s.ShopId)
                    .Where(id => id > 0)
                    .Distinct()
                    .Count();

                var overallTotalEmployees = monthEmployeeIds.Count > 0
                    ? monthEmployeeIds.Count
                    : monthEmployeeNames.Count;

                // Lists for UI
                var monthScheduleList = monthScheduleNames.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                var monthShopList = monthShopNames.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
                var monthEmployeeList = monthEmployeeNames.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();

                // ---- Apply to UI
                UI(() =>
                {
                    WhoWorksTodayItems.Clear();
                    foreach (var row in dedupedToday)
                        WhoWorksTodayItems.Add(row);

                    ActiveSchedules.Clear();
                    foreach (var card in scheduleCards)
                        ActiveSchedules.Add(card);

                    MonthSchedulesCount = monthSchedules.Count;
                    TotalContainersCount = containers.Count;
                    TodayAssignmentsCount = dedupedToday.Count;
                    ActiveShopsCount = monthSchedules.Select(s => s.ShopId).Where(id => id > 0).Distinct().Count();

                    // Information - This month
                    CurrentMonthContainerName = currentContainerName;
                    CurrentMonthLabel = now.ToString("MM.yyyy", CultureInfo.InvariantCulture);

                    CurrentMonthScheduleNames = new ObservableCollection<string>(monthScheduleList);
                    CurrentMonthShopNames = new ObservableCollection<string>(monthShopList);
                    CurrentMonthEmployeeNames = new ObservableCollection<string>(monthEmployeeList);

                    CurrentMonthTotalEmployees = monthEmployeeIds.Count > 0 ? monthEmployeeIds.Count : monthEmployeeNames.Count;
                    CurrentMonthTotalSchedules = monthSchedules.Count;
                    CurrentMonthTotalHours = Math.Round(monthTotalHours, 1);
                    CurrentMonthTotalShops = monthShopIds.Count;

                    // Information - Overall
                    OverallTotalEmployees = overallTotalEmployees;
                    OverallTotalContainers = overallTotalContainers;
                    OverallTotalShops = overallTotalShops;

                    StatusText = "Home data is up to date.";
                    _initialized = true;
                });

                _lastLoadSuccessful = true;

            }
            catch (OperationCanceledException)
            {
                UI(() => StatusText = "Loading cancelled.");
            }
            catch (Exception ex)
            {
                _logger.Log($"[Home] Failed to load data: {ex}");
                UI(() => StatusText = "Failed to load home data.");
                _lastLoadSuccessful = false;

            }
            finally
            {
                UI(() => IsLoading = false);
            }
        }

        private void UpdateCurrentTimeText()
            => CurrentTimeText = DateTime.Now.ToString("HH:mm dd.MM.yyyy", CultureInfo.InvariantCulture);

        private async void OnDatabaseChanged(object? sender, DatabaseChangedEventArgs e)
        {
            _logger.Log($"[Home] Database changed from {e.Source}; refreshing Home.");
            await LoadDataAsync(CancellationToken.None);
        }

        private async Task RefreshWithOverlayAsync(CancellationToken ct)
        {
            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await LoadDataAsync(uiToken);

                // Якщо LoadDataAsync зловив exception всередині (він не кидає назовні),
                // то не показуємо Success при fail.
                if (_lastLoadSuccessful)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    await ShowNavSuccessThenAutoHideAsync(uiToken, 700);
                }
                else
                {
                    await HideNavStatusAsync();
                }
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                _logger.Log($"[Home] Refresh failed: {ex}");
            }
        }

    }

    public sealed class WhoWorksTodayRowViewModel
    {
        public DateTime Date { get; init; }
        public string Employee { get; init; } = string.Empty;
        public string Shift { get; init; } = string.Empty;
        public string Shop { get; init; } = string.Empty;
    }

    /// <summary>
    /// Card VM for Active Schedules (DataGrid matrix) + style provider (cell colors like Schedule Profile).
    /// </summary>
    public sealed class HomeScheduleCardViewModel : ViewModelBase, IScheduleMatrixStyleProvider
    {
        public HomeScheduleCardViewModel(string title, string shopName, string monthLabel)
        {
            Title = title ?? "-";
            ShopName = shopName ?? "-";
            MonthLabel = monthLabel ?? "-";
        }

        public string Title { get; }
        public string ShopName { get; }
        public string MonthLabel { get; }

        // optional (if you later want header “Total hours: ...”)
        private string _totalHoursText = "0h 0m";
        public string TotalHoursText
        {
            get => _totalHoursText;
            private set => SetProperty(ref _totalHoursText, value);
        }

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

        private readonly Dictionary<string, int> _colNameToEmpId = new();
        private readonly ScheduleCellStyleStore _cellStyleStore = new();
        private readonly Dictionary<int, Brush> _brushCache = new();
        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        public void ApplyBuild(
            DataView view,
            Dictionary<string, int> colMap,
            IList<ScheduleCellStyleModel> styles,
            Dictionary<int, string> perEmployeeTotals,
            string totalHoursText)
        {
            ScheduleMatrix = view ?? new DataView();

            _colNameToEmpId.Clear();
            if (colMap != null)
            {
                foreach (var pair in colMap)
                    _colNameToEmpId[pair.Key] = pair.Value;
            }

            _cellStyleStore.Load(styles ?? Array.Empty<ScheduleCellStyleModel>());

            _employeeTotalHoursText.Clear();
            if (perEmployeeTotals != null)
            {
                foreach (var kv in perEmployeeTotals)
                    _employeeTotalHoursText[kv.Key] = kv.Value;
            }

            TotalHoursText = string.IsNullOrWhiteSpace(totalHoursText) ? "0h 0m" : totalHoursText;

            // trigger WPF to re-evaluate styles
            CellStyleRevision++;
        }

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
            try { day = Convert.ToInt32(dayObj, CultureInfo.InvariantCulture); }
            catch { return false; }

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
    }

    public sealed class HomeScheduleEntryViewModel
    {
        public int Day { get; init; }
        public string Employee { get; init; } = string.Empty;
        public string Shift { get; init; } = string.Empty;
    }
}
