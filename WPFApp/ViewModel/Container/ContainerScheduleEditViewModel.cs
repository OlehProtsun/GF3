using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerScheduleEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        public const string DayColumnName = "DayOfMonth";
        public const string ConflictColumnName = "Conflict";
        public const string EmptyMark = "-";

        private CancellationTokenSource? _availabilityPreviewCts;

        private readonly ContainerViewModel _owner;
        private readonly Dictionary<string, List<string>> _errors = new();
        private readonly Dictionary<string, int> _colNameToEmpId = new();

        private bool _suppressAvailabilityGroupUpdate;

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

                InvalidateGeneratedScheduleAndClearMatrices(); // очищає і schedule і preview
                _ = LoadAvailabilityPreviewForSelectedGroupAsync(); // ✅ підвантажити заново під новий місяць
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
                InvalidateGeneratedScheduleAndClearMatrices();

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
                InvalidateGeneratedScheduleAndClearMatrices();

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
                if (SetProperty(ref _selectedShop, value))
                {
                    ScheduleShopId = value?.Id ?? 0;
                }
            }
        }
        private AvailabilityGroupModel? _selectedAvailabilityGroup;
        public AvailabilityGroupModel? SelectedAvailabilityGroup
        {
            get => _selectedAvailabilityGroup;
            set
            {
                if (!SetProperty(ref _selectedAvailabilityGroup, value))
                    return;

                if (_suppressAvailabilityGroupUpdate)
                    return;

                // записати вибір у блок
                if (SelectedBlock != null)
                    SelectedBlock.SelectedAvailabilityGroupId = value?.Id ?? 0;

                var groupId = value?.Id ?? 0;

                // 1) підтягнути працівників в MinHours
                if (groupId > 0)
                    _ = _owner.SyncEmployeesFromAvailabilityGroupAsync(groupId);

                // 2) Availability grid — завантажити одразу
                _ = LoadAvailabilityPreviewForSelectedGroupAsync();

                // 3) Schedule grid результат стає невалідним (але Availability preview НЕ чіпаємо)
                if (SelectedBlock?.Slots.Count > 0)
                    SelectedBlock.Slots.Clear();

                ScheduleMatrix = new DataView();
                MatrixChanged?.Invoke(this, EventArgs.Empty);
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

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand GenerateCommand { get; }
        public AsyncRelayCommand AddBlockCommand { get; }
        public AsyncRelayCommand SearchShopCommand { get; }
        public AsyncRelayCommand SearchAvailabilityCommand { get; }
        public AsyncRelayCommand SearchEmployeeCommand { get; }
        public AsyncRelayCommand AddEmployeeCommand { get; }
        public AsyncRelayCommand RemoveEmployeeCommand { get; }

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

                    // <-- замість LoadAvailabilityPreviewAsync()
                    var groupId = SelectedBlock.SelectedAvailabilityGroupId;
                    if (groupId > 0)
                        _ = _owner.SyncEmployeesFromAvailabilityGroupAsync(groupId);

                    RestoreMatricesForSelection();

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
            if (SelectedBlock == null)
            {
                SelectedShop = null;
                SelectedAvailabilityGroup = null;
                return;
            }

            SelectedShop = Shops.FirstOrDefault(s => s.Id == SelectedBlock.Model.ShopId);

            _suppressAvailabilityGroupUpdate = true;
            SelectedAvailabilityGroup = AvailabilityGroups.FirstOrDefault(g => g.Id == SelectedBlock.SelectedAvailabilityGroupId);
            _suppressAvailabilityGroupUpdate = false;
        }

        public void RefreshScheduleMatrix()
        {
            if (SelectedBlock is null)
            {
                ScheduleMatrix = new DataView();
                MatrixChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (ScheduleYear < 1 || ScheduleMonth < 1 || ScheduleMonth > 12)
            {
                ScheduleMatrix = new DataView();
                MatrixChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            var table = BuildScheduleTable(ScheduleYear, ScheduleMonth,
                SelectedBlock.Slots.ToList(), SelectedBlock.Employees.ToList(), out var colMap);

            _colNameToEmpId.Clear();
            foreach (var pair in colMap)
                _colNameToEmpId[pair.Key] = pair.Value;

            ScheduleMatrix = table.DefaultView;
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshAvailabilityPreviewMatrix(int year, int month, IList<ScheduleSlotModel> slots, IList<ScheduleEmployeeModel> employees)
        {
            if (year < 1 || month < 1 || month > 12)
            {
                AvailabilityPreviewMatrix = new DataView();
                MatrixChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            var table = BuildScheduleTable(year, month, slots, employees, out _);
            AvailabilityPreviewMatrix = table.DefaultView;
            MatrixChanged?.Invoke(this, EventArgs.Empty);
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

            foreach (var emp in employees)
            {
                var displayName = $"{emp.Employee.FirstName} {emp.Employee.LastName}".Trim();
                var columnName = string.IsNullOrWhiteSpace(displayName) ? $"Employee {emp.EmployeeId}" : displayName;
                var suffix = 1;

                while (table.Columns.Contains(columnName))
                    columnName = $"{displayName} ({++suffix})";

                table.Columns.Add(columnName, typeof(string));
                colNameToEmpId[columnName] = emp.EmployeeId;
            }

            var daysInMonth = DateTime.DaysInMonth(year, month);

            var byDay = (slots ?? Array.Empty<ScheduleSlotModel>())
                .GroupBy(s => s.DayOfMonth)
                .ToDictionary(g => g.Key, g => g.ToList());

            for (int day = 1; day <= daysInMonth; day++)
            {
                byDay.TryGetValue(day, out var daySlots);

                var row = table.NewRow();
                row[DayColumnName] = day;
                row[ConflictColumnName] = daySlots?.Any(s => s.EmployeeId == null) ?? false;

                foreach (var (colName, empId) in colNameToEmpId)
                {
                    if (daySlots == null || daySlots.Count == 0)
                    {
                        row[colName] = EmptyMark;
                        continue;
                    }

                    var byEmp = daySlots
                        .Where(s => s.EmployeeId != null)
                        .GroupBy(s => s.EmployeeId!.Value)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    if (!byEmp.TryGetValue(empId, out var empSlots) || empSlots.Count == 0)
                    {
                        row[colName] = EmptyMark;
                        continue;
                    }

                    var merged = MergeIntervalsForDisplay(empSlots);
                    row[colName] = merged.Count == 0
                        ? EmptyMark
                        : string.Join(", ", merged.Select(i => $"{i.from} - {i.to}"));
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
            RefreshScheduleMatrix();
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
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task LoadAvailabilityPreviewForSelectedGroupAsync()
        {
            // debounce + cancel попередніх
            _availabilityPreviewCts?.Cancel();
            var cts = _availabilityPreviewCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(150, cts.Token); // короткий debounce, щоб не штормило при швидких кліках

                if (SelectedBlock is null)
                {
                    AvailabilityPreviewMatrix = new DataView();
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }

                var groupId = SelectedAvailabilityGroup?.Id ?? 0;
                if (groupId <= 0)
                {
                    AvailabilityPreviewMatrix = new DataView();
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // тягнемо групу повністю (members + days)
                var (group, members, days) = await _owner.AvailabilityGroupService_LoadFullAsync(groupId, cts.Token);

                // якщо група не під цей місяць — просто очистити preview
                if (group.Year != ScheduleYear || group.Month != ScheduleMonth)
                {
                    AvailabilityPreviewMatrix = new DataView();
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // прив'язуємо days до members
                var daysByMember = days
                    .GroupBy(d => d.AvailabilityGroupMemberId)
                    .ToDictionary(g => g.Key, g => (ICollection<AvailabilityGroupDayModel>)g.ToList());

                foreach (var m in members)
                    m.Days = daysByMember.TryGetValue(m.Id, out var list) ? list : new List<AvailabilityGroupDayModel>();

                // будуємо слоти для preview: беремо всі інтервали availability як "slots"
                // (ми не чіпаємо Schedule Slots, це окрема візуалізація)
                var previewSlots = new List<ScheduleSlotModel>();
                foreach (var m in members)
                {
                    foreach (var d in m.Days)
                    {
                        // якщо інтервалу нема — нема що показувати
                        if (string.IsNullOrWhiteSpace(d.IntervalStr))
                            continue;

                        // Парсимо всі інтервали з IntervalStr (підтримує "HH:mm - HH:mm, HH:mm - HH:mm")
                        if (!TryParseIntervals(d.IntervalStr, out var intervals, out _))
                            continue;

                        var slotNo = 1;
                        foreach (var (from, to) in intervals)
                        {
                            previewSlots.Add(new ScheduleSlotModel
                            {
                                ScheduleId = 0,              // preview, тому можна 0
                                DayOfMonth = d.DayOfMonth,
                                EmployeeId = m.EmployeeId,
                                SlotNo = slotNo++,
                                FromTime = from,
                                ToTime = to
                            });
                        }
                    }

                }

                // employees для колонок: беремо з members
                var previewEmployees = members
                    .GroupBy(m => m.EmployeeId)
                    .Select(g => new ScheduleEmployeeModel { EmployeeId = g.Key, Employee = g.First().Employee })
                    .ToList();

                RefreshAvailabilityPreviewMatrix(group.Year, group.Month, previewSlots, previewEmployees);
            }
            catch (OperationCanceledException) { }
        }

        private static (TimeSpan? from, TimeSpan? to) ParseFirstInterval(string? intervalStr)
        {
            if (string.IsNullOrWhiteSpace(intervalStr))
                return (null, null);

            // беремо перший інтервал, якщо їх декілька
            var first = intervalStr.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(first))
                return (null, null);

            // підтримка звичайного "08:00-17:00" і варіантів тире
            var parts = first.Split(new[] { '-', '–', '—' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .ToArray();

            if (parts.Length != 2)
                return (null, null);

            // найчастіше це TimeSpan ("HH:mm")
            if (TimeSpan.TryParse(parts[0], CultureInfo.InvariantCulture, out var from) &&
                TimeSpan.TryParse(parts[1], CultureInfo.InvariantCulture, out var to))
                return (from, to);

            // fallback: якщо раптом там DateTime
            if (DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var df) &&
                DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return (df.TimeOfDay, dt.TimeOfDay);

            return (null, null);
        }


    }
}
