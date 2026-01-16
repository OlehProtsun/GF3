using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerScheduleProfileViewModel : ViewModelBase, IScheduleMatrixStyleProvider
    {
        private readonly ContainerViewModel _owner;
        private readonly Dictionary<string, int> _colNameToEmpId = new();
        private readonly Dictionary<(int day, int employeeId), ScheduleCellStyleModel> _cellStyleMap = new();

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
            RebuildStyleMaps(colMap, cellStyles);
            MatrixChanged?.Invoke(this, EventArgs.Empty);
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
            => _cellStyleMap.TryGetValue((cellRef.DayOfMonth, cellRef.EmployeeId), out style!);

        private void RebuildStyleMaps(Dictionary<string, int> colMap, IList<ScheduleCellStyleModel> cellStyles)
        {
            _colNameToEmpId.Clear();
            foreach (var pair in colMap)
                _colNameToEmpId[pair.Key] = pair.Value;

            _cellStyleMap.Clear();
            foreach (var style in cellStyles)
            {
                _cellStyleMap[(style.DayOfMonth, style.EmployeeId)] = style;
            }

            CellStyleRevision++;
        }
    }
}
