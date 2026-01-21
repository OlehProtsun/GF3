using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using WPFApp.Infrastructure;


namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerScheduleEditViewModel : ViewModelBase, INotifyDataErrorInfo, IScheduleMatrixStyleProvider
    {
        private int _scheduleBuildVersion; // інкремент на кожен refresh


        public enum SchedulePaintMode
        {
            None,
            Background,
            Foreground
        }

        public const string DayColumnName = "DayOfMonth";
        public const string ConflictColumnName = "Conflict";
        public const string EmptyMark = "-";

        // кеш: EmployeeId -> "Total hours: 12h 30m"
        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        // ✅ додай
        public const string WeekendColumnName = "IsWeekend";

        private static readonly TimeSpan SelectionDebounceDelay = TimeSpan.FromMilliseconds(200);
        private CancellationTokenSource? _availabilityPreviewCts;
        private CancellationTokenSource? _scheduleMatrixCts;
        private CancellationTokenSource? _shopSelectionCts;
        private CancellationTokenSource? _availabilitySelectionCts;
        private string? _availabilityPreviewKey;


        private readonly ContainerViewModel _owner;
        private readonly Dictionary<string, List<string>> _errors = new();
        private readonly Dictionary<string, int> _colNameToEmpId = new();
        private readonly ScheduleCellStyleStore _cellStyleStore = new();

        private bool _suppressAvailabilityGroupUpdate;

        private SchedulePaintMode _activePaintMode = SchedulePaintMode.None;
        public SchedulePaintMode ActivePaintMode
        {
            get => _activePaintMode;
            private set => SetProperty(ref _activePaintMode, value);
        }

        private int? _lastFillColorArgb;
        public int? LastFillColorArgb
        {
            get => _lastFillColorArgb;
            private set
            {
                if (SetProperty(ref _lastFillColorArgb, value))
                    LastFillBrush = value.HasValue ? ColorHelpers.ToBrush(value.Value) : null;
            }
        }

        private int? _lastTextColorArgb;
        public int? LastTextColorArgb
        {
            get => _lastTextColorArgb;
            private set
            {
                if (SetProperty(ref _lastTextColorArgb, value))
                    LastTextBrush = value.HasValue ? ColorHelpers.ToBrush(value.Value) : null;
            }
        }

        private Brush? _lastFillBrush;
        public Brush? LastFillBrush
        {
            get => _lastFillBrush;
            private set => SetProperty(ref _lastFillBrush, value);
        }

        private Brush? _lastTextBrush;
        public Brush? LastTextBrush
        {
            get => _lastTextBrush;
            private set => SetProperty(ref _lastTextBrush, value);
        }

        private ScheduleMatrixCellRef? _selectedCellRef;
        public ScheduleMatrixCellRef? SelectedCellRef
        {
            get => _selectedCellRef;
            set => SetProperty(ref _selectedCellRef, value);
        }

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


        public ObservableCollection<ScheduleMatrixCellRef> SelectedCellRefs { get; } = new();

        public ObservableCollection<ScheduleBlockViewModel> Blocks { get; } = new();

        public ObservableCollection<ScheduleBlockViewModel> OpenedSchedules => Blocks;

        private ScheduleBlockViewModel? _selectedBlock;
        public ScheduleBlockViewModel? SelectedBlock
        {
            get => _selectedBlock;
            set
            {
                if (SetProperty(ref _selectedBlock, value))
                {
                    OnPropertiesChanged(
                        nameof(ScheduleId),
                        nameof(ScheduleContainerId),
                        nameof(ScheduleShopId),
                        nameof(ScheduleName),
                        nameof(ScheduleYear),
                        nameof(ScheduleMonth),
                        nameof(SchedulePeoplePerShift),
                        nameof(ScheduleShift1),
                        nameof(ScheduleShift2),
                        nameof(ScheduleMaxHoursPerEmp),
                        nameof(ScheduleMaxConsecutiveDays),
                        nameof(ScheduleMaxConsecutiveFull),
                        nameof(ScheduleMaxFullPerMonth),
                        nameof(ScheduleNote),
                        nameof(ScheduleEmployees),
                        nameof(SelectedAvailabilityGroup));

                    OnPropertyChanged(nameof(ActiveSchedule));

                    SyncSelectionFromBlock();
                    RestoreMatricesForSelection();
                    RefreshCellStyleMap();

                    SelectedCellRefs.Clear();
                    SelectedCellRef = null;
                }
            }
        }

        public ScheduleBlockViewModel? ActiveSchedule
        {
            get => SelectedBlock;
            set => SelectedBlock = value;
        }

        private bool _isEdit;
        public bool IsEdit
        {
            get => _isEdit;
            set
            {
                if (SetProperty(ref _isEdit, value))
                    OnPropertiesChanged(nameof(FormTitle), nameof(FormSubtitle), nameof(CanAddBlock));
            }
        }

        public bool CanAddBlock => !IsEdit;

        public string FormTitle => IsEdit ? "Edit Schedule" : "Add Schedule";

        public string FormSubtitle => IsEdit
            ? "Update schedule details and press Save."
            : "Fill the form and press Save.";

        public int ScheduleId
        {
            get => SelectedBlock?.Model.Id ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Id == value) return;
                SelectedBlock.Model.Id = value;
                OnPropertyChanged();
            }
        }

        public int ScheduleContainerId
        {
            get => SelectedBlock?.Model.ContainerId ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.ContainerId == value) return;
                SelectedBlock.Model.ContainerId = value;
                OnPropertyChanged();
            }
        }

        public int ScheduleShopId
        {
            get => SelectedBlock?.Model.ShopId ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.ShopId == value) return;
                SelectedBlock.Model.ShopId = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(ScheduleShopId));
            }
        }

        public string ScheduleName
        {
            get => SelectedBlock?.Model.Name ?? string.Empty;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Name == value) return;
                SelectedBlock.Model.Name = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(ScheduleName));
            }
        }

        public int ScheduleYear
        {
            get => SelectedBlock?.Model.Year ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Year == value) return;
                SelectedBlock.Model.Year = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(ScheduleYear));
                InvalidateGeneratedScheduleAndClearMatrices();
            }
        }

        public int ScheduleMonth
        {
            get => SelectedBlock?.Model.Month ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Month == value) return;
                SelectedBlock.Model.Month = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(ScheduleMonth));

                InvalidateGeneratedScheduleAndClearMatrices();
            }
        }


        public int SchedulePeoplePerShift
        {
            get => SelectedBlock?.Model.PeoplePerShift ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.PeoplePerShift == value) return;
                SelectedBlock.Model.PeoplePerShift = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(SchedulePeoplePerShift));
            }
        }

        public string ScheduleShift1
        {
            get => SelectedBlock?.Model.Shift1Time ?? string.Empty;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Shift1Time == value) return;
                SelectedBlock.Model.Shift1Time = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(ScheduleShift1));

            }
        }

        public string ScheduleShift2
        {
            get => SelectedBlock?.Model.Shift2Time ?? string.Empty;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Shift2Time == value) return;
                SelectedBlock.Model.Shift2Time = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(ScheduleShift2));

            }
        }

        public int ScheduleMaxHoursPerEmp
        {
            get => SelectedBlock?.Model.MaxHoursPerEmpMonth ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.MaxHoursPerEmpMonth == value) return;
                SelectedBlock.Model.MaxHoursPerEmpMonth = value;
                OnPropertyChanged();
                ClearValidationErrors(nameof(ScheduleMaxHoursPerEmp));
            }
        }

        public int ScheduleMaxConsecutiveDays
        {
            get => SelectedBlock?.Model.MaxConsecutiveDays ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.MaxConsecutiveDays == value) return;
                SelectedBlock.Model.MaxConsecutiveDays = value;
                OnPropertyChanged();
            }
        }

        public int ScheduleMaxConsecutiveFull
        {
            get => SelectedBlock?.Model.MaxConsecutiveFull ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.MaxConsecutiveFull == value) return;
                SelectedBlock.Model.MaxConsecutiveFull = value;
                OnPropertyChanged();
            }
        }

        public int ScheduleMaxFullPerMonth
        {
            get => SelectedBlock?.Model.MaxFullPerMonth ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.MaxFullPerMonth == value) return;
                SelectedBlock.Model.MaxFullPerMonth = value;
                OnPropertyChanged();
            }
        }

        public string ScheduleNote
        {
            get => SelectedBlock?.Model.Note ?? string.Empty;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Note == value) return;
                SelectedBlock.Model.Note = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ScheduleEmployeeModel> ScheduleEmployees
            => SelectedBlock?.Employees ?? new ObservableCollection<ScheduleEmployeeModel>();

        public ObservableCollection<ScheduleSlotModel> ScheduleSlots
            => SelectedBlock?.Slots ?? new ObservableCollection<ScheduleSlotModel>();

        public ObservableCollection<ShopModel> Shops { get; } = new();
        public ObservableCollection<AvailabilityGroupModel> AvailabilityGroups { get; } = new();
        public ObservableCollection<EmployeeModel> Employees { get; } = new();

        private ShopModel? _selectedShop;
        public ShopModel? SelectedShop
        {
            get => _selectedShop;
            set
            {
                var oldId = _selectedShop?.Id ?? 0;
                var newId = value?.Id ?? 0;

                if (!SetProperty(ref _selectedShop, value))
                    return;

                if (_selectionSyncDepth > 0)
                    return;

                if (oldId == newId)
                    return;

                ScheduleShopSelectionChange(newId);
            }
        }

        private AvailabilityGroupModel? _selectedAvailabilityGroup;
        public AvailabilityGroupModel? SelectedAvailabilityGroup
        {
            get => _selectedAvailabilityGroup;
            set
            {
                var oldId = _selectedAvailabilityGroup?.Id ?? 0;
                var newId = value?.Id ?? 0;

                if (!SetProperty(ref _selectedAvailabilityGroup, value))
                    return;

                if (_suppressAvailabilityGroupUpdate)
                    return;

                if (oldId == newId)
                    return;

                if (SelectedBlock == null)
                    return;

                if (_selectionSyncDepth > 0)
                    return;

                ScheduleAvailabilitySelectionChange(newId);
            }
        }



        private EmployeeModel? _selectedEmployee;
        public EmployeeModel? SelectedEmployee
        {
            get => _selectedEmployee;
            set => SetProperty(ref _selectedEmployee, value);
        }

        private ScheduleEmployeeModel? _selectedScheduleEmployee;
        public ScheduleEmployeeModel? SelectedScheduleEmployee
        {
            get => _selectedScheduleEmployee;
            set => SetProperty(ref _selectedScheduleEmployee, value);
        }

        private string _shopSearchText = string.Empty;
        public string ShopSearchText
        {
            get => _shopSearchText;
            set => SetProperty(ref _shopSearchText, value);
        }

        private string _availabilitySearchText = string.Empty;
        public string AvailabilitySearchText
        {
            get => _availabilitySearchText;
            set => SetProperty(ref _availabilitySearchText, value);
        }

        private string _employeeSearchText = string.Empty;
        public string EmployeeSearchText
        {
            get => _employeeSearchText;
            set => SetProperty(ref _employeeSearchText, value);
        }

        private DataView _scheduleMatrix = new DataView();
        public DataView ScheduleMatrix
        {
            get => _scheduleMatrix;
            private set => SetProperty(ref _scheduleMatrix, value);
        }

        private DataView _availabilityPreviewMatrix = new DataView();
        public DataView AvailabilityPreviewMatrix
        {
            get => _availabilityPreviewMatrix;
            private set => SetProperty(ref _availabilityPreviewMatrix, value);
        }

        private int _cellStyleRevision;
        public int CellStyleRevision
        {
            get => _cellStyleRevision;
            private set => SetProperty(ref _cellStyleRevision, value);
        }

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand GenerateCommand { get; }
        public AsyncRelayCommand AddBlockCommand { get; }
        public AsyncRelayCommand SearchShopCommand { get; }
        public AsyncRelayCommand SearchAvailabilityCommand { get; }
        public AsyncRelayCommand SearchEmployeeCommand { get; }
        public AsyncRelayCommand AddEmployeeCommand { get; }
        public AsyncRelayCommand RemoveEmployeeCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> SetCellBackgroundColorCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> SetCellTextColorCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ClearCellFormattingCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ClearSelectedCellStyleCommand { get; }
        public RelayCommand ClearAllScheduleStylesCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ApplyLastFillColorCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ApplyLastTextColorCommand { get; }
        public RelayCommand PickFillColorCommand { get; }
        public RelayCommand PickTextColorCommand { get; }

        public event EventHandler? MatrixChanged;

        public ContainerScheduleEditViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            SaveCommand = new AsyncRelayCommand(() => _owner.SaveScheduleAsync());
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            GenerateCommand = new AsyncRelayCommand(() => _owner.GenerateScheduleAsync());
            AddBlockCommand = new AsyncRelayCommand(() => _owner.AddScheduleBlockAsync());

            SearchShopCommand = new AsyncRelayCommand(() => _owner.SearchScheduleShopsAsync());
            SearchAvailabilityCommand = new AsyncRelayCommand(() => _owner.SearchScheduleAvailabilityAsync());
            SearchEmployeeCommand = new AsyncRelayCommand(() => _owner.SearchScheduleEmployeesAsync());

            AddEmployeeCommand = new AsyncRelayCommand(() => _owner.AddScheduleEmployeeAsync());
            RemoveEmployeeCommand = new AsyncRelayCommand(() => _owner.RemoveScheduleEmployeeAsync());

            SetCellBackgroundColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(SetCellBackgroundColor);
            SetCellTextColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(SetCellTextColor);
            ClearCellFormattingCommand = new RelayCommand<ScheduleMatrixCellRef?>(ClearCellFormatting);
            ClearSelectedCellStyleCommand = new RelayCommand<ScheduleMatrixCellRef?>(ClearSelectedCellStyles);
            ClearAllScheduleStylesCommand = new RelayCommand(ClearAllScheduleStyles);
            ApplyLastFillColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(ApplyLastFillColor);
            ApplyLastTextColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(ApplyLastTextColor);
            PickFillColorCommand = new RelayCommand(PickFillColor);
            PickTextColorCommand = new RelayCommand(PickTextColor);
        }

        public Task SelectBlockAsync(ScheduleBlockViewModel block)
            => _owner.SelectScheduleBlockAsync(block);

        public Task CloseBlockAsync(ScheduleBlockViewModel block)
            => _owner.CloseScheduleBlockAsync(block);

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName is null || !_errors.TryGetValue(propertyName, out var list))
                return Array.Empty<string>();

            return list;
        }


        public void ResetForNew()
        {
            Blocks.Clear();
            SelectedBlock = null;
            IsEdit = false;
            ClearValidationErrors();
            ShopSearchText = string.Empty;
            AvailabilitySearchText = string.Empty;
            EmployeeSearchText = string.Empty;
            SelectedShop = null;
            SelectedAvailabilityGroup = null;
            SelectedEmployee = null;
            SelectedScheduleEmployee = null;
            ScheduleMatrix = new DataView();
            AvailabilityPreviewMatrix = new DataView();
            _availabilityPreviewKey = null;
            _cellStyleStore.Load(Array.Empty<ScheduleCellStyleModel>());
            CellStyleRevision++;
            SelectedCellRef = null;
            SelectedCellRefs.Clear();
            ActivePaintMode = SchedulePaintMode.None;
            CancelSelectionUpdates();
        }

        public string GetEmployeeTotalHoursText(string columnName)
        {
            // для Day/Conflict/Weekend та ін. — пусто
            if (SelectedBlock is null) return string.Empty;
            if (!_colNameToEmpId.TryGetValue(columnName, out var empId)) return string.Empty;

            return _employeeTotalHoursText.TryGetValue(empId, out var text)
                ? text
                : "Total hours: 0h 0m";
        }

        private int _selectionSyncDepth;

        private readonly struct SelectionSyncScope : IDisposable
        {
            private readonly ContainerScheduleEditViewModel _vm;
            public SelectionSyncScope(ContainerScheduleEditViewModel vm)
            {
                _vm = vm;
                _vm._selectionSyncDepth++;
            }

            public void Dispose()
            {
                _vm._selectionSyncDepth = Math.Max(0, _vm._selectionSyncDepth - 1);
            }
        }

        private SelectionSyncScope EnterSelectionSync() => new(this);


        private void RecalculateTotals()
        {
            _employeeTotalHoursText.Clear();

            if (SelectedBlock is null)
            {
                TotalEmployees = 0;
                TotalHoursText = "0h 0m";
                return;
            }

            // 1) Total employees (distinct)
            TotalEmployees = SelectedBlock.Employees
                .Select(e => e.EmployeeId)
                .Distinct()
                .Count();

            // 2) Total hours (sum) + per employee totals
            var total = TimeSpan.Zero;
            var perEmp = new Dictionary<int, TimeSpan>();

            foreach (var s in SelectedBlock.Slots)
            {
                // ✅ EmployeeId nullable
                if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0)
                    continue;

                var empId = s.EmployeeId.Value;

                if (string.IsNullOrWhiteSpace(s.FromTime) || string.IsNullOrWhiteSpace(s.ToTime))
                    continue;

                if (!TimeSpan.TryParseExact(s.FromTime.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var from))
                    continue;

                if (!TimeSpan.TryParseExact(s.ToTime.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var to))
                    continue;

                var dur = to - from;
                if (dur < TimeSpan.Zero)
                    dur += TimeSpan.FromHours(24);

                total += dur;

                if (!perEmp.TryGetValue(empId, out var cur))
                    cur = TimeSpan.Zero;

                perEmp[empId] = cur + dur;
            }

            // кешуємо текст для всіх employees блоку (навіть якщо 0)
            foreach (var empId in SelectedBlock.Employees.Select(e => e.EmployeeId).Distinct())
            {
                perEmp.TryGetValue(empId, out var empTotal);
                _employeeTotalHoursText[empId] = $"Total hours: {(int)empTotal.TotalHours}h {empTotal.Minutes}m";
            }

            TotalHoursText = $"{(int)total.TotalHours}h {total.Minutes}m";
        }


        public void SetLookups(IEnumerable<ShopModel> shops, IEnumerable<AvailabilityGroupModel> groups, IEnumerable<EmployeeModel> employees)
        {
            SetShops(shops);
            SetAvailabilityGroups(groups);
            SetEmployees(employees);
        }

        public void SetShops(IEnumerable<ShopModel> shops)
        {
            SetOptions(Shops, shops);
            if (SelectedBlock != null)
                SelectedShop = Shops.FirstOrDefault(s => s.Id == SelectedBlock.Model.ShopId);
        }

        public void SetAvailabilityGroups(IEnumerable<AvailabilityGroupModel> groups)
        {
            SetOptions(AvailabilityGroups, groups);

            if (SelectedBlock != null)
            {
                _suppressAvailabilityGroupUpdate = true;
                SelectedAvailabilityGroup = AvailabilityGroups
                    .FirstOrDefault(g => g.Id == SelectedBlock.SelectedAvailabilityGroupId);
                _suppressAvailabilityGroupUpdate = false;
            }
        }

        // ContainerScheduleEditViewModel.cs

        private void InvalidateGeneratedSchedule()
        {
            if (SelectedBlock is null) return;

            // якщо вже було згенеровано — скинемо результат
            if (SelectedBlock.Slots.Count > 0)
                SelectedBlock.Slots.Clear();

            // очищаємо відображення (без BuildScheduleTable)
            ScheduleMatrix = new DataView();
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }


        public void SetEmployees(IEnumerable<EmployeeModel> employees)
            => SetOptions(Employees, employees);

        public void SyncSelectionFromBlock()
        {
            using var _ = EnterSelectionSync();

            if (SelectedBlock == null)
            {
                SelectedShop = null;

                _suppressAvailabilityGroupUpdate = true;
                try
                {
                    SelectedAvailabilityGroup = null;
                }
                finally
                {
                    _suppressAvailabilityGroupUpdate = false;
                }

                return;
            }

            SelectedShop = Shops.FirstOrDefault(s => s.Id == SelectedBlock.Model.ShopId);

            _suppressAvailabilityGroupUpdate = true;
            try
            {
                SelectedAvailabilityGroup = AvailabilityGroups.FirstOrDefault(g => g.Id == SelectedBlock.SelectedAvailabilityGroupId);
            }
            finally
            {
                _suppressAvailabilityGroupUpdate = false;
            }
        }


        public void RefreshScheduleMatrix()
        {
            SafeForget(RefreshScheduleMatrixAsync());
        }


        private static void SafeForget(Task task)
        {
            // Забираємо Exception, щоб вона не стала "unobserved" у дебагері
            task.ContinueWith(t =>
            {
                _ = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }


        internal async Task RefreshScheduleMatrixAsync(CancellationToken ct = default)
        {
            int buildVer = Interlocked.Increment(ref _scheduleBuildVersion);

            // cancel попереднього білду (щоб не було черги з 10 білдів)
            CancellationTokenSource? prev = Interlocked.Exchange(ref _scheduleMatrixCts, null);
            if (prev != null)
            {
                try { prev.Cancel(); } catch { /* ignore */ }
                try { prev.Dispose(); } catch { /* ignore */ }
            }

            // створюємо новий CTS, прив’язаний до зовнішнього ct
            var localCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _scheduleMatrixCts = localCts;
            var token = localCts.Token;

            try
            {
                // -----------------------------
                // FAST GUARD: clear matrix
                // -----------------------------
                if (SelectedBlock is null ||
                    ScheduleYear < 1 || ScheduleMonth < 1 || ScheduleMonth > 12 ||
                    SelectedBlock.Slots.Count == 0)
                {
                    await _owner.RunOnUiThreadAsync(() =>
                    {
                        ScheduleMatrix = new DataView();
                        RecalculateTotals();

                        MatrixChanged?.Invoke(this, EventArgs.Empty);
                    }).ConfigureAwait(false);

                    return;
                }

                // -----------------------------
                // SNAPSHOT (cheap)
                // -----------------------------
                int year = ScheduleYear;
                int month = ScheduleMonth;

                // важливо: зняти snapshot одразу, але без зайвих ToList якщо можливо.
                var blockId = SelectedBlock.Model.Id;
                var slotsSnapshot = SelectedBlock.Slots.ToList();
                var employeesSnapshot = SelectedBlock.Employees.ToList();

                var result = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var table = BuildScheduleTable(year, month, slotsSnapshot, employeesSnapshot, out var colMap);

                    token.ThrowIfCancellationRequested();

                    return (View: table.DefaultView, ColMap: colMap);
                }, token).ConfigureAwait(false);

                // -----------------------------
                // IGNORE STALE RESULT
                // (якщо під час білду стартанув новий buildVer)
                // -----------------------------
                if (buildVer != Volatile.Read(ref _scheduleBuildVersion) || token.IsCancellationRequested)
                {
                    return;
                }

                // -----------------------------
                // APPLY ON UI THREAD
                // -----------------------------
                await _owner.RunOnUiThreadAsync(() =>
                {
                    // ще раз захист: якщо вже не актуально на момент UI
                    if (buildVer != _scheduleBuildVersion || token.IsCancellationRequested)
                    {
                        return;
                    }

                    _colNameToEmpId.Clear();
                    foreach (var pair in result.ColMap)
                        _colNameToEmpId[pair.Key] = pair.Value;

                    ScheduleMatrix = result.View;

                    RecalculateTotals();

                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                // dispose тільки якщо це досі наш CTS (щоб не прибити новий)
                if (ReferenceEquals(_scheduleMatrixCts, localCts))
                {
                    _scheduleMatrixCts = null;
                    try { localCts.Dispose(); } catch { /* ignore */ }
                }
            }
        }


        public void RefreshAvailabilityPreviewMatrix(int year, int month, IList<ScheduleSlotModel> slots, IList<ScheduleEmployeeModel> employees)
        {
            SafeForget(RefreshAvailabilityPreviewMatrixAsync(year, month, slots, employees, previewKey: null));
        }

        /// <summary>
        /// Builds the preview matrix off the UI thread and only assigns the DataView on the Dispatcher.
        /// This prevents scroll stutter right after Generate / group changes.
        /// </summary>
        internal async Task RefreshAvailabilityPreviewMatrixAsync(
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            string? previewKey = null,
            CancellationToken ct = default)
        {
            // ✅ Стабільний ключ навіть для "очистки"
            var effectiveKey = previewKey ?? $"CLEAR|{year}|{month}";

            // ✅ РЕАЛЬНИЙ SKIP
            if (effectiveKey == _availabilityPreviewKey)
            {
                return;
            }

            _availabilityPreviewCts?.Cancel();
            _availabilityPreviewCts?.Dispose();
            var localCts = _availabilityPreviewCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            try
            {
                var view = await Task.Run(() =>
                {
                    var table = BuildScheduleTable(year, month, slots, employees, out _);
                    return table.DefaultView;
                }, localCts.Token).ConfigureAwait(false);

                await _owner.RunOnUiThreadAsync(() =>
                {
                    AvailabilityPreviewMatrix = view;
                    _availabilityPreviewKey = effectiveKey; // ✅ запам’ятали ключ
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        public bool TryApplyMatrixEdit(string columnName, int day, string rawInput, out string normalizedValue, out string? error)
        {
            normalizedValue = rawInput;
            error = null;

            if (SelectedBlock is null)
                return false;

            if (!_colNameToEmpId.TryGetValue(columnName, out var empId))
                return false;

            if (!TryParseIntervals(rawInput, out var intervals, out error))
                return false;

            ApplyIntervalsToSlots(SelectedBlock, day, empId, intervals);

            normalizedValue = intervals.Count == 0
                ? EmptyMark
                : string.Join(", ", intervals.Select(i => $"{i.from} - {i.to}"));

            RefreshScheduleMatrix();
            return true;
        }

        public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
        {
            cellRef = default;

            if (SelectedBlock is null)
                return false;

            if (string.IsNullOrWhiteSpace(columnName)
                || columnName == DayColumnName
                || columnName == ConflictColumnName)
                return false;

            if (rowData is not DataRowView rowView)
                return false;

            var dayObj = rowView[DayColumnName];
            int day;

            if (dayObj is int i) day = i;
            else if (dayObj is long l) day = (int)l;
            else if (dayObj is short s) day = s;
            else if (dayObj is byte b) day = b;
            else if (dayObj is string str)
            {
                if (!int.TryParse(str, out day)) return false;
            }
            else
            {
                try { day = Convert.ToInt32(dayObj); }
                catch { return false; }
            }


            if (!_colNameToEmpId.TryGetValue(columnName, out var employeeId))
                return false;

            cellRef = new ScheduleMatrixCellRef(day, employeeId, columnName);
            return true;
        }

        public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)
        {
            return TryGetCellStyle(cellRef, out var style)
                   && style.BackgroundColorArgb is int argb
                   && argb != 0
                ? ColorHelpers.ToBrush(argb)
                : null;
        }

        public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)
        {
            return TryGetCellStyle(cellRef, out var style)
                   && style.TextColorArgb is int argb
                   && argb != 0
                ? ColorHelpers.ToBrush(argb)
                : null;
        }

        public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)
            => _cellStyleStore.TryGetStyle(cellRef, out style);

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

        internal void RemoveCellStylesForEmployee(int employeeId)
        {
            if (SelectedBlock is null)
                return;

            if (_cellStyleStore.RemoveByEmployee(employeeId, SelectedBlock.CellStyles) > 0)
                RefreshCellStyleMap();
        }

        private void RefreshCellStyleMap()
        {
            _cellStyleStore.Load(SelectedBlock?.CellStyles?.ToArray() ?? Array.Empty<ScheduleCellStyleModel>());
            CellStyleRevision++;
        }


        public void UpdateSelectedCellRefs(IEnumerable<ScheduleMatrixCellRef> cellRefs)
        {
            SelectedCellRefs.Clear();
            foreach (var cellRef in cellRefs.Distinct())
                SelectedCellRefs.Add(cellRef);
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

        private void PickFillColor()
        {
            SetCellBackgroundColor(null);
        }

        private void PickTextColor()
        {
            SetCellTextColor(null);
        }

        internal void CancelBackgroundWork()
        {
            _availabilityPreviewCts?.Cancel();
            _availabilityPreviewCts?.Dispose();
            _availabilityPreviewCts = null;

            _scheduleMatrixCts?.Cancel();
            _scheduleMatrixCts?.Dispose();
            _scheduleMatrixCts = null;

            CancelSelectionUpdates();
        }

        // ContainerScheduleEditViewModel.cs

        internal bool IsAvailabilityPreviewCurrent(string? previewKey)
        {
            if (string.IsNullOrWhiteSpace(previewKey))
                return false;

            // було: _availabilityPreviewCurrentKey
            return string.Equals(_availabilityPreviewKey, previewKey, StringComparison.Ordinal);
        }



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
                return SelectedCellRefs.ToList();

            if (fallback.HasValue)
                return new List<ScheduleMatrixCellRef> { fallback.Value };

            return Array.Empty<ScheduleMatrixCellRef>();
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();
            foreach (var kv in errors)
                AddError(kv.Key, kv.Value);
        }

        public void ClearValidationErrors()
        {
            var keys = _errors.Keys.ToList();
            _errors.Clear();

            foreach (var key in keys)
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));

            OnPropertyChanged(nameof(HasErrors));
        }

        private void AddError(string propertyName, string message)
        {
            if (!_errors.TryGetValue(propertyName, out var list))
            {
                list = new List<string>();
                _errors[propertyName] = list;
            }

            if (!list.Contains(message))
            {
                list.Add(message);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void ClearValidationErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void SetOptions<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
                target.Add(item);
        }

        private void ScheduleShopSelectionChange(int newId)
        {
            ScheduleSelectionUpdate(
                ref _shopSelectionCts,
                () =>
                {
                    if (SelectedShop?.Id != newId)
                        return;

                    ScheduleShopId = newId;
                });
        }

        private void ScheduleAvailabilitySelectionChange(int newId)
        {
            ScheduleSelectionUpdate(
                ref _availabilitySelectionCts,
                () =>
                {
                    if (SelectedAvailabilityGroup?.Id != newId)
                        return;

                    if (SelectedBlock is null)
                        return;

                    SelectedBlock.SelectedAvailabilityGroupId = newId;
                    InvalidateGeneratedScheduleAndClearMatrices();
                });
        }

        private void ScheduleSelectionUpdate(ref CancellationTokenSource? cts, Action apply)
        {
            cts?.Cancel();
            cts?.Dispose();

            var localCts = new CancellationTokenSource();
            cts = localCts;
            var token = localCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(SelectionDebounceDelay, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                    return;

                await _owner.RunOnUiThreadAsync(() =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    apply();
                }).ConfigureAwait(false);
            }, token);
        }

        private void CancelSelectionUpdates()
        {
            _shopSelectionCts?.Cancel();
            _shopSelectionCts?.Dispose();
            _shopSelectionCts = null;

            _availabilitySelectionCts?.Cancel();
            _availabilitySelectionCts?.Dispose();
            _availabilitySelectionCts = null;
        }

        public static DataTable BuildScheduleTable(
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            out Dictionary<string, int> colNameToEmpId)
        {
            colNameToEmpId = new Dictionary<string, int>();
            var table = new DataTable();

            table.Columns.Add(DayColumnName, typeof(int));
            table.Columns.Add(ConflictColumnName, typeof(bool));
            table.Columns.Add(WeekendColumnName, typeof(bool)); // технічна колонка під RowStyle

            // 1) Колонки по працівниках
            var seenEmpIds = new HashSet<int>();
            foreach (var emp in employees)
            {
                if (!seenEmpIds.Add(emp.EmployeeId))
                    continue;

                var displayName = $"{emp.Employee.FirstName} {emp.Employee.LastName}".Trim();
                var baseName = string.IsNullOrWhiteSpace(displayName) ? $"Employee {emp.EmployeeId}" : displayName;

                // стабільні імена колонок (для кешу WPF/DataView) + заголовок у Caption
                var columnName = $"emp_{emp.EmployeeId}";
                var suffix = 1;
                while (table.Columns.Contains(columnName))
                    columnName = $"emp_{emp.EmployeeId}_{++suffix}";

                var col = table.Columns.Add(columnName, typeof(string));
                col.Caption = baseName;
                colNameToEmpId[columnName] = emp.EmployeeId;
            }

            var daysInMonth = DateTime.DaysInMonth(year, month);

            // 2) Швидка індексація слотів по днях (без GroupBy)
            var slotsByDay = new List<ScheduleSlotModel>?[daysInMonth + 1]; // 1..daysInMonth
            if (slots != null && slots.Count > 0)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    var d = s.DayOfMonth;
                    if ((uint)d > (uint)daysInMonth || d <= 0) continue;

                    var list = slotsByDay[d];
                    if (list == null)
                    {
                        list = new List<ScheduleSlotModel>();
                        slotsByDay[d] = list;
                    }
                    list.Add(s);
                }
            }

            // 3) Щоб не ходити по Dictionary-ітератору з tuple-deconstruct
            var empCols = colNameToEmpId.ToArray(); // KeyValuePair<string,int>[]

            // 4) Форматування інтервалів без LINQ-ланцюжків
            static string FormatMerged(List<(string from, string to)> merged)
            {
                if (merged == null || merged.Count == 0) return EmptyMark;

                var sb = new StringBuilder(capacity: merged.Count * 14);
                for (int i = 0; i < merged.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(merged[i].from).Append(" - ").Append(merged[i].to);
                }
                return sb.ToString();
            }

            // 5) Основний цикл
            for (int day = 1; day <= daysInMonth; day++)
            {
                var daySlots = slotsByDay[day];

                var row = table.NewRow();
                row[DayColumnName] = day;

                var dow = new DateTime(year, month, day).DayOfWeek;
                row[WeekendColumnName] = (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday);

                // Якщо на день немає слотів — заповнюємо рядок “-” і їдемо далі (дешево)
                if (daySlots == null || daySlots.Count == 0)
                {
                    row[ConflictColumnName] = false;
                    for (int i = 0; i < empCols.Length; i++)
                        row[empCols[i].Key] = EmptyMark;

                    table.Rows.Add(row);
                    continue;
                }

                // 5.1) Один прохід: Conflict + групування по EmployeeId (без GroupBy)
                bool conflict = false;
                var byEmp = new Dictionary<int, List<ScheduleSlotModel>>(capacity: Math.Min(employees.Count, 32));

                for (int i = 0; i < daySlots.Count; i++)
                {
                    var s = daySlots[i];

                    if (s.EmployeeId == null)
                    {
                        conflict = true;
                        continue;
                    }

                    int empId = s.EmployeeId.Value;
                    if (!byEmp.TryGetValue(empId, out var list))
                    {
                        list = new List<ScheduleSlotModel>();
                        byEmp[empId] = list;
                    }
                    list.Add(s);
                }

                row[ConflictColumnName] = conflict;

                // 5.2) Заповнюємо клітинки для кожного працівника
                for (int i = 0; i < empCols.Length; i++)
                {
                    var colName = empCols[i].Key;
                    var empId = empCols[i].Value;

                    if (!byEmp.TryGetValue(empId, out var empSlots) || empSlots.Count == 0)
                    {
                        row[colName] = EmptyMark;
                        continue;
                    }

                    var merged = MergeIntervalsForDisplay(empSlots);
                    row[colName] = FormatMerged(merged);
                }

                table.Rows.Add(row);
            }

            return table;
        }
        private static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots)
        {
            var list = new List<(TimeSpan from, TimeSpan to)>();

            foreach (var s in slots)
            {
                if (!TryParseTime(s.FromTime, out var f)) continue;
                if (!TryParseTime(s.ToTime, out var t)) continue;
                list.Add((f, t));
            }

            list = list
                .Distinct()
                .OrderBy(x => x.from)
                .ThenBy(x => x.to)
                .ToList();

            if (list.Count == 0) return new();

            var merged = new List<(TimeSpan from, TimeSpan to)>();
            foreach (var cur in list)
            {
                if (merged.Count == 0)
                {
                    merged.Add(cur);
                    continue;
                }

                var last = merged[^1];
                if (cur.from <= last.to)
                {
                    merged[^1] = (last.from, cur.to > last.to ? cur.to : last.to);
                }
                else
                {
                    merged.Add(cur);
                }
            }

            return merged
                .Select(x => (x.from.ToString(@"hh\:mm"), x.to.ToString(@"hh\:mm")))
                .ToList();
        }

        private static bool TryParseIntervals(string? text, out List<(string from, string to)> intervals, out string? error)
        {
            intervals = new();
            error = null;

            text = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text) || text == EmptyMark)
                return true;

            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var parsed = new List<(TimeSpan from, TimeSpan to)>();
            foreach (var p in parts)
            {
                var dash = p.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (dash.Length != 2)
                {
                    error = "Format: HH:mm - HH:mm (comma separated allowed).";
                    return false;
                }

                if (!TryParseTime(dash[0], out var from) || !TryParseTime(dash[1], out var to))
                {
                    error = "Time must be HH:mm (e.g. 09:00 - 14:30).";
                    return false;
                }

                if (from >= to)
                {
                    error = "From must be earlier than To";
                    return false;
                }

                parsed.Add((from, to));
            }

            var unique = parsed
                .Distinct()
                .OrderBy(x => x.from)
                .ThenBy(x => x.to)
                .ToList();

            intervals = unique
                .Select(x => (x.from.ToString(@"hh\:mm"), x.to.ToString(@"hh\:mm")))
                .ToList();

            return true;
        }

        private static bool TryParseTime(string s, out TimeSpan t)
        {
            return TimeSpan.TryParseExact(
                s.Trim(),
                new[] { @"h\:mm", @"hh\:mm" },
                CultureInfo.InvariantCulture,
                out t);
        }

        private static void ApplyIntervalsToSlots(ScheduleBlockViewModel block, int day, int empId, List<(string from, string to)> intervals)
        {
            var preservedStatus = block.Slots.FirstOrDefault(s => s.DayOfMonth == day && s.EmployeeId == empId)?.Status
                                  ?? SlotStatus.UNFURNISHED;

            var toRemove = block.Slots.Where(s => s.DayOfMonth == day && s.EmployeeId == empId).ToList();
            foreach (var slot in toRemove)
                block.Slots.Remove(slot);

            foreach (var (from, to) in intervals)
            {
                block.Slots.Add(new ScheduleSlotModel
                {
                    ScheduleId = block.Model.Id,
                    DayOfMonth = day,
                    EmployeeId = empId,
                    FromTime = from,
                    ToTime = to,
                    SlotNo = NextFreeSlotNo(block.Slots.ToList(), day, from, to),
                    Status = preservedStatus
                });
            }
        }

        private static int NextFreeSlotNo(List<ScheduleSlotModel> list, int day, string from, string to)
        {
            var used = list
                .Where(s => s.DayOfMonth == day && s.FromTime == from && s.ToTime == to)
                .Select(s => s.SlotNo)
                .ToHashSet();

            var n = 1;
            while (used.Contains(n)) n++;
            return n;
        }

        // ContainerScheduleEditViewModel.cs


        // 1) просто очистити відображення (НЕ чіпаючи Slots) — для вибору блоку
        private void RestoreMatricesForSelection()
        {
            AvailabilityPreviewMatrix = new DataView();
            ScheduleMatrix = new DataView();
            _availabilityPreviewKey = null;
            RecalculateTotals();
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }


        // 2) коли змінили параметри — скинути результат генерації + очистити відображення
        internal void InvalidateGeneratedScheduleAndClearMatrices()
        {
            if (SelectedBlock is null) return;

            // скидаємо вже згенерований результат
            if (SelectedBlock.Slots.Count > 0)
                SelectedBlock.Slots.Clear();

            ScheduleMatrix = new DataView();
            AvailabilityPreviewMatrix = new DataView();
            _availabilityPreviewKey = null;
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
