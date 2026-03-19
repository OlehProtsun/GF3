/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerProfileViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
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
using WPFApp.ViewModel.Container.ScheduleProfile;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using System.Windows.Media;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.List;
using WPFApp.ViewModel.Container.ScheduleList;
using WPFApp.View.Dialogs;
using WPFApp.Applications.Export;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Commands;
using WPFApp.Applications.Matrix.Schedule;


namespace WPFApp.ViewModel.Container.Profile
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerProfileViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerProfileViewModel : ViewModelBase
    {
        private readonly ContainerViewModel _owner;

        

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


        private int _containerId;
        /// <summary>
        /// Визначає публічний елемент `public int ContainerId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ContainerId
        {
            get => _containerId;
            private set => SetProperty(ref _containerId, value);
        }

        private string _name = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Name` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        private string? _note;
        /// <summary>
        /// Визначає публічний елемент `public string? Note` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string? Note
        {
            get => _note;
            private set => SetProperty(ref _note, value);
        }

        
        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleListViewModel ScheduleListVm { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleListViewModel ScheduleListVm { get; }

        private ContainerModel? _currentContainer;

        
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
        /// Визначає публічний елемент `public AsyncRelayCommand ExportToCodeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ExportToCodeCommand { get; }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Func<int, CancellationToken, Task<IReadOnlyList<ScheduleEmployeeModel>>>? EmployeesLoader { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Func<int, CancellationToken, Task<IReadOnlyList<ScheduleEmployeeModel>>>? EmployeesLoader { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public Func<int, CancellationToken, Task<IReadOnlyList<ScheduleSlotModel>>>? SlotsLoader { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Func<int, CancellationToken, Task<IReadOnlyList<ScheduleSlotModel>>>? SlotsLoader { get; set; }

        
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

        private int _totalShops;
        /// <summary>
        /// Визначає публічний елемент `public int TotalShops` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalShops
        {
            get => _totalShops;
            private set => SetProperty(ref _totalShops, value);
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

        private string _totalShopsListText = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string TotalShopsListText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalShopsListText
        {
            get => _totalShopsListText;
            private set => SetProperty(ref _totalShopsListText, value);
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

        
        /// <summary>
        /// Визначає публічний елемент `public sealed class ShopHeader` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class ShopHeader
        {
            /// <summary>
            /// Визначає публічний елемент `public string Key { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string Key { get; }   
            /// <summary>
            /// Визначає публічний елемент `public string Name { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string Name { get; }  

            /// <summary>
            /// Визначає публічний елемент `public ShopHeader(string key, string name)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public ShopHeader(string key, string name)
            {
                Key = key ?? string.Empty;
                Name = name ?? string.Empty;
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ShopHeader> ShopHeaders { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ShopHeader> ShopHeaders { get; } = new();

        /// <summary>
        /// Визначає публічний елемент `public sealed class EmployeeShopHoursRow` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class EmployeeShopHoursRow
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
            /// Визначає публічний елемент `public string HoursSum { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string HoursSum { get; }

            
            /// <summary>
            /// Визначає публічний елемент `public Dictionary<string, string> HoursByShop { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public Dictionary<string, string> HoursByShop { get; }

            /// <summary>
            /// Визначає публічний елемент `public EmployeeShopHoursRow(` та контракт його використання у шарі WPFApp.
            /// </summary>
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

            
            /// <summary>
            /// Визначає публічний елемент `public string this[string shopKey]` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string this[string shopKey]
                => (shopKey != null && HoursByShop != null && HoursByShop.TryGetValue(shopKey, out var v))
                    ? (v ?? string.Empty)
                    : string.Empty;
        }



        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<EmployeeShopHoursRow> EmployeeShopHoursRows { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<EmployeeShopHoursRow> EmployeeShopHoursRows { get; } = new();

        
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
        /// Визначає публічний елемент `public event EventHandler? StatisticsChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? StatisticsChanged;

        
        private CancellationTokenSource? _statsCts;
        private int _statsVersion;

        
        private bool _autoHooked;
        private int _autoQueued;

        
        private static readonly Regex SummaryHoursRegex =
            new(@"^\s*(?:(\d+)\s*h)?\s*(?:(\d+)\s*m)?\s*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Визначає публічний елемент `public ContainerProfileViewModel(ContainerViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            ScheduleListVm = new ContainerScheduleListViewModel(owner);

            
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

        /// <summary>
        /// Визначає публічний елемент `public void SetProfile(ContainerModel model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetProfile(ContainerModel model)
        {
            IsExportStatusVisible = false;
            ExportStatus = UIStatusKind.Success;

            _currentContainer = model;
            ContainerId = model.Id;
            Name = model.Name;
            Note = model.Note;

            
            ExportToExcelCommand.RaiseCanExecuteChanged();
            ExportToCodeCommand.RaiseCanExecuteChanged();
            _ = QueueAutoStatsRebuildAsync();
        }

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public sealed class ScheduleStatsSource` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class ScheduleStatsSource
        {
            /// <summary>
            /// Визначає публічний елемент `public ScheduleModel Schedule { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public ScheduleModel Schedule { get; }
            /// <summary>
            /// Визначає публічний елемент `public IReadOnlyList<ScheduleEmployeeModel> Employees { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public IReadOnlyList<ScheduleEmployeeModel> Employees { get; }
            /// <summary>
            /// Визначає публічний елемент `public IReadOnlyList<ScheduleSlotModel> Slots { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public IReadOnlyList<ScheduleSlotModel> Slots { get; }

            /// <summary>
            /// Визначає публічний елемент `public ScheduleStatsSource(` та контракт його використання у шарі WPFApp.
            /// </summary>
            public ScheduleStatsSource(
                ScheduleModel schedule,
                IEnumerable<ScheduleEmployeeModel>? employees,
                IEnumerable<ScheduleSlotModel>? slots)
            {
                Schedule = schedule;

                
                Employees = employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                Slots = slots?.ToList() ?? new List<ScheduleSlotModel>();
            }
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public async Task SetStatisticsAsync(IList<ScheduleStatsSource>? sources, CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
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

                    
                    var shopKeyToName = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var s in sources)
                    {
                        var (shopKey, shopName) = GetShopKeyName(s.Schedule);
                        if (!shopKeyToName.ContainsKey(shopKey))
                            shopKeyToName[shopKey] = shopName;
                    }

                    
                    var empIdToName = new Dictionary<int, string>();
                    var empShopDur = new Dictionary<int, Dictionary<string, TimeSpan>>();   
                    var empTotalDur = new Dictionary<int, TimeSpan>();                      
                    var empWorkDays = new Dictionary<int, int>();
                    var empFreeDays = new Dictionary<int, int>();
                    
                    var shopTotalDur = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);


                    TimeSpan totalAll = TimeSpan.Zero;

                    
                    foreach (var src in sources)
                    {
                        token.ThrowIfCancellationRequested();

                        var schedule = src.Schedule;
                        var (shopKey, _) = GetShopKeyName(schedule);

                        
                        var totals = ScheduleTotalsCalculator.Calculate(src.Employees, src.Slots);

                        totalAll += totals.TotalDuration;

                        
                        foreach (var emp in src.Employees)
                        {
                            var id = emp.EmployeeId;
                            if (!empIdToName.ContainsKey(id))
                                empIdToName[id] = GetEmployeeDisplayName(emp);
                        }

                        
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

                            
                            shopTotalDur.TryGetValue(shopKey, out var curShopTotal);
                            shopTotalDur[shopKey] = curShopTotal + dur;
                        }


                        
                        var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);

                        
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

                        
                        foreach (var empId in workedDaysByEmp.Keys)
                        {
                            var wd = workedDaysByEmp[empId].Count;
                            var fd = Math.Max(0, daysInMonth - wd);

                            empWorkDays.TryGetValue(empId, out var curWd);
                            empFreeDays.TryGetValue(empId, out var curFd);

                            empWorkDays[empId] = curWd + wd;
                            empFreeDays[empId] = curFd + fd;

                            
                            if (!empIdToName.ContainsKey(empId))
                                empIdToName[empId] = $"Employee {empId}";
                        }
                    }

                    
                    foreach (var empId in empTotalDur.Keys)
                        if (!empIdToName.ContainsKey(empId))
                            empIdToName[empId] = $"Employee {empId}";

                    
                    var shopHeaders = shopKeyToName
                        .Select(kv => new ShopHeader(kv.Key, kv.Value))
                        .OrderBy(h => h.Name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                    
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
        /// Визначає публічний елемент `public async Task RefreshStatisticsFromContainerSchedulesAsync(CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
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

        
        private sealed class BuildStatsResult
        {
            /// <summary>
            /// Визначає публічний елемент `public string TotalHoursText { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string TotalHoursText { get; }
            /// <summary>
            /// Визначає публічний елемент `public int TotalEmployees { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int TotalEmployees { get; }
            /// <summary>
            /// Визначає публічний елемент `public int TotalShops { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int TotalShops { get; }
            /// <summary>
            /// Визначає публічний елемент `public string TotalEmployeesListText { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string TotalEmployeesListText { get; }
            /// <summary>
            /// Визначає публічний елемент `public string TotalShopsListText { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public string TotalShopsListText { get; }
            /// <summary>
            /// Визначає публічний елемент `public List<ShopHeader> ShopHeaders { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public List<ShopHeader> ShopHeaders { get; }
            /// <summary>
            /// Визначає публічний елемент `public List<EmployeeShopHoursRow> PivotRows { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public List<EmployeeShopHoursRow> PivotRows { get; }
            /// <summary>
            /// Визначає публічний елемент `public List<EmployeeWorkFreeStatRow> WorkFreeRows { get; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public List<EmployeeWorkFreeStatRow> WorkFreeRows { get; }

            /// <summary>
            /// Визначає публічний елемент `public BuildStatsResult(string totalHoursText, int totalEmployees, int totalShops,` та контракт його використання у шарі WPFApp.
            /// </summary>
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

        
        private void HookScheduleListForAutoStatistics()
        {
            if (_autoHooked) return;
            _autoHooked = true;

            
            if (ScheduleListVm.Items is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += (_, __) => _ = QueueAutoStatsRebuildAsync();
            }

            
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

                
                await ShowExportSuccessThenAutoHideAsync(uiToken, 1400).ConfigureAwait(false);

                
                
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

                var sumText = FormatHoursCell(sum); 
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
            /// <summary>
            /// Визначає публічний елемент `public int CellStyleRevision => 0;` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int CellStyleRevision => 0;
            /// <summary>
            /// Визначає публічний елемент `public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef) => null;` та контракт його використання у шарі WPFApp.
            /// </summary>
            public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef) => null;
            /// <summary>
            /// Визначає публічний елемент `public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef) => null;` та контракт його використання у шарі WPFApp.
            /// </summary>
            public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef) => null;
            /// <summary>
            /// Визначає публічний елемент `public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
            {
                cellRef = default;
                return false;
            }
            /// <summary>
            /// Визначає публічний елемент `public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            {
                style = null!;
                return false;
            }
        }

        
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

                
                var employees = TryGetEnumerable<ScheduleEmployeeModel>(row, "Employees", "ScheduleEmployees")
                             ?? TryGetEnumerable<ScheduleEmployeeModel>(schedule, "Employees", "ScheduleEmployees");

                var slots = TryGetEnumerable<ScheduleSlotModel>(row, "Slots", "ScheduleSlots")
                         ?? TryGetEnumerable<ScheduleSlotModel>(schedule, "Slots", "ScheduleSlots");

                
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
