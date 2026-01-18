using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using System.Globalization;


namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerScheduleProfileViewModel : ViewModelBase, IScheduleMatrixStyleProvider
    {
        private readonly ContainerViewModel _owner;
        private readonly Dictionary<string, int> _colNameToEmpId = new();
        private readonly ScheduleCellStyleStore _cellStyleStore = new();

        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        private int _scheduleId;
        public int ScheduleId
        {
            get => _scheduleId;
            set => SetProperty(ref _scheduleId, value);
        }

        private string _scheduleName = string.Empty;
        public string ScheduleName
        {
            get => _scheduleName;
            set => SetProperty(ref _scheduleName, value);
        }

        private string _scheduleMonthYear = string.Empty;
        public string ScheduleMonthYear
        {
            get => _scheduleMonthYear;
            set => SetProperty(ref _scheduleMonthYear, value);
        }

        private string _shopName = string.Empty;
        public string ShopName
        {
            get => _shopName;
            set => SetProperty(ref _shopName, value);
        }

        private string _note = string.Empty;
        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
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

        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public event EventHandler? MatrixChanged;

        public ContainerScheduleProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            BackCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync());
        }

        public void SetProfile(
            ScheduleModel schedule,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel> cellStyles)
        {
            ScheduleId = schedule.Id;
            ScheduleName = schedule.Name;
            ScheduleMonthYear = $"{schedule.Month:D2}.{schedule.Year}";
            ShopName = schedule.Shop?.Name ?? string.Empty;
            Note = schedule.Note ?? string.Empty;

            Employees.Clear();
            foreach (var emp in employees)
                Employees.Add(emp);

            var table = ContainerScheduleEditViewModel.BuildScheduleTable(
                schedule.Year,
                schedule.Month,
                slots,
                employees,
                out var colMap);


            ScheduleMatrix = table.DefaultView;
            RecalculateTotals(employees, slots, out var empCount, out var hoursText);
            RebuildStyleMaps(colMap, cellStyles);
            TotalEmployees = empCount;
            TotalHoursText = hoursText;
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RecalculateTotals(
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            out int totalEmployees,
            out string totalHoursText)
        {
            _employeeTotalHoursText.Clear();

            totalEmployees = employees
                .Select(e => e.EmployeeId)
                .Distinct()
                .Count();

            var total = TimeSpan.Zero;
            var perEmp = new Dictionary<int, TimeSpan>();

            foreach (var s in slots)
            {
                if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0)
                    continue;

                if (!TryParseTime(s.FromTime, out var from)) continue;
                if (!TryParseTime(s.ToTime, out var to)) continue;

                var dur = to - from;
                if (dur < TimeSpan.Zero)
                    dur += TimeSpan.FromHours(24);

                total += dur;

                var empId = s.EmployeeId.Value;
                perEmp.TryGetValue(empId, out var cur);
                perEmp[empId] = cur + dur;
            }

            // ✅ пер-employee кеш (НЕ ламає старі total out-параметри)
            foreach (var empId in employees.Select(e => e.EmployeeId).Distinct())
            {
                perEmp.TryGetValue(empId, out var empTotal);
                _employeeTotalHoursText[empId] = $"Total hours: {(int)empTotal.TotalHours}h {empTotal.Minutes}m";
            }

            totalHoursText = $"{(int)total.TotalHours}h {total.Minutes}m";

            static bool TryParseTime(string? t, out TimeSpan value)
            {
                value = default;
                t = (t ?? string.Empty).Trim();

                return TimeSpan.TryParseExact(t, @"hh\:mm", CultureInfo.InvariantCulture, out value)
                    || TimeSpan.TryParse(t, CultureInfo.InvariantCulture, out value);
            }
        }

        public string GetEmployeeTotalHoursText(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            // пропускаємо технічні колонки
            if (columnName == ContainerScheduleEditViewModel.DayColumnName
                || columnName == ContainerScheduleEditViewModel.ConflictColumnName
                || columnName == ContainerScheduleEditViewModel.WeekendColumnName)
                return string.Empty;

            if (!_colNameToEmpId.TryGetValue(columnName, out var empId))
                return string.Empty;

            return _employeeTotalHoursText.TryGetValue(empId, out var text)
                ? text
                : "Total hours: 0h 0m";
        }

        public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
        {
            cellRef = default;

            if (string.IsNullOrWhiteSpace(columnName)
                || columnName == ContainerScheduleEditViewModel.DayColumnName
                || columnName == ContainerScheduleEditViewModel.ConflictColumnName)
                return false;

            if (rowData is not DataRowView rowView)
                return false;

            if (!int.TryParse(rowView[ContainerScheduleEditViewModel.DayColumnName]?.ToString(), out var day))
                return false;

            if (!_colNameToEmpId.TryGetValue(columnName, out var employeeId))
                return false;

            cellRef = new ScheduleMatrixCellRef(day, employeeId, columnName);
            return true;
        }

        public System.Windows.Media.Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)
        {
            return TryGetCellStyle(cellRef, out var style) && style.BackgroundColorArgb.HasValue
                ? ColorHelpers.ToBrush(style.BackgroundColorArgb.Value)
                : null;
        }

        public System.Windows.Media.Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)
        {
            return TryGetCellStyle(cellRef, out var style) && style.TextColorArgb.HasValue
                ? ColorHelpers.ToBrush(style.TextColorArgb.Value)
                : null;
        }

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
    }
}
