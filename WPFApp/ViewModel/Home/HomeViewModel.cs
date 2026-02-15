using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using WPFApp.Infrastructure;
using WPFApp.Service;

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

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync, () => !IsLoading);

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

        private async Task LoadDataAsync(CancellationToken ct)
        {
            IsLoading = true;
            StatusText = "Loading home data...";

            try
            {
                var now = DateTime.Now;

                var schedules = await _scheduleService.GetAllAsync(ct).ConfigureAwait(false);
                var monthSchedules = schedules
                    .Where(s => s.Year == now.Year && s.Month == now.Month)
                    .OrderBy(s => s.Shop?.Name)
                    .ThenBy(s => s.Name)
                    .ToList();

                var containers = await _containerService.GetAllAsync(ct).ConfigureAwait(false);

                var todayRows = new List<WhoWorksTodayRowViewModel>();
                var scheduleCards = new List<HomeScheduleCardViewModel>();

                foreach (var schedule in monthSchedules)
                {
                    ct.ThrowIfCancellationRequested();

                    var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct).ConfigureAwait(false);
                    if (detailed is null)
                        continue;

                    var todaySlots = detailed.Slots
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

                    var scheduleEntries = detailed.Slots
                        .Where(slot => slot.EmployeeId.HasValue)
                        .OrderBy(slot => slot.DayOfMonth)
                        .ThenBy(slot => slot.FromTime)
                        .ThenBy(slot => slot.Employee?.LastName)
                        .ThenBy(slot => slot.Employee?.FirstName)
                        .Select(slot => new HomeScheduleEntryViewModel
                        {
                            Day = slot.DayOfMonth,
                            Employee = slot.Employee == null
                                ? "-"
                                : $"{slot.Employee.FirstName} {slot.Employee.LastName}".Trim(),
                            Shift = $"{slot.FromTime} - {slot.ToTime}"
                        })
                        .ToList();

                    scheduleCards.Add(new HomeScheduleCardViewModel
                    {
                        Title = detailed.Name,
                        Subtitle = $"{detailed.Shop?.Name ?? "-"} • {now:MMMM yyyy}",
                        Items = new ObservableCollection<HomeScheduleEntryViewModel>(scheduleEntries)
                    });
                }

                var dedupedToday = todayRows
                    .GroupBy(r => new { r.Date, r.Employee, r.Shift, r.Shop })
                    .Select(g => g.First())
                    .OrderBy(r => r.Shift)
                    .ThenBy(r => r.Employee)
                    .ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    WhoWorksTodayItems.Clear();
                    foreach (var row in dedupedToday)
                        WhoWorksTodayItems.Add(row);

                    ActiveSchedules.Clear();
                    foreach (var card in scheduleCards)
                        ActiveSchedules.Add(card);
                });

                MonthSchedulesCount = monthSchedules.Count;
                TotalContainersCount = containers.Count;
                TodayAssignmentsCount = dedupedToday.Count;
                ActiveShopsCount = monthSchedules
                    .Select(s => s.ShopId)
                    .Distinct()
                    .Count();

                StatusText = "Home data is up to date.";
                _initialized = true;
            }
            catch (OperationCanceledException)
            {
                StatusText = "Loading cancelled.";
            }
            catch (Exception ex)
            {
                _logger.Log($"[Home] Failed to load data: {ex}");
                StatusText = "Failed to load home data.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateCurrentTimeText()
            => CurrentTimeText = DateTime.Now.ToString("HH:mm dd.MM.yyyy");

        private async void OnDatabaseChanged(object? sender, DatabaseChangedEventArgs e)
        {
            _logger.Log($"[Home] Database changed from {e.Source}; refreshing Home.");
            await LoadDataAsync(CancellationToken.None);
        }
    }

    public sealed class WhoWorksTodayRowViewModel
    {
        public DateTime Date { get; init; }
        public string Employee { get; init; } = string.Empty;
        public string Shift { get; init; } = string.Empty;
        public string Shop { get; init; } = string.Empty;
    }

    public sealed class HomeScheduleCardViewModel
    {
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public ObservableCollection<HomeScheduleEntryViewModel> Items { get; init; } = new();
    }

    public sealed class HomeScheduleEntryViewModel
    {
        public int Day { get; init; }
        public string Employee { get; init; } = string.Empty;
        public string Shift { get; init; } = string.Empty;
    }
}
