using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Input;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Availability
{
    public sealed class AvailabilityEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private const string DayColumn = "DayOfMonth";

        private readonly AvailabilityViewModel _owner;
        private readonly Dictionary<int, string> _employeeIdToColumn = new();
        private readonly Dictionary<int, string> _employeeNames = new();
        private readonly Dictionary<string, List<string>> _errors = new();

        private readonly DataTable _groupTable = new();

        public DataView AvailabilityDays => _groupTable.DefaultView;

        public ObservableCollection<EmployeeListItem> Employees { get; } = new();
        public ObservableCollection<BindRow> Binds { get; } = new();

        private EmployeeListItem? _selectedEmployee;
        public EmployeeListItem? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (SetProperty(ref _selectedEmployee, value))
                    OnPropertyChanged(nameof(SelectedEmployeeId));
            }
        }

        public string SelectedEmployeeId => SelectedEmployee?.Id > 0
            ? SelectedEmployee.Id.ToString()
            : string.Empty;

        private BindRow? _selectedBind;
        public BindRow? SelectedBind
        {
            get => _selectedBind;
            set => SetProperty(ref _selectedBind, value);
        }

        private object? _selectedAvailabilityDay;
        public object? SelectedAvailabilityDay
        {
            get => _selectedAvailabilityDay;
            set => SetProperty(ref _selectedAvailabilityDay, value);
        }

        private int _availabilityId;
        public int AvailabilityId
        {
            get => _availabilityId;
            set => SetProperty(ref _availabilityId, value);
        }

        private int _availabilityMonth;
        public int AvailabilityMonth
        {
            get => _availabilityMonth;
            set
            {
                if (SetProperty(ref _availabilityMonth, value))
                {
                    ClearValidationErrors(nameof(AvailabilityMonth));
                    RegenerateGroupDays();
                }
            }
        }

        private int _availabilityYear;
        public int AvailabilityYear
        {
            get => _availabilityYear;
            set
            {
                if (SetProperty(ref _availabilityYear, value))
                {
                    ClearValidationErrors(nameof(AvailabilityYear));
                    RegenerateGroupDays();
                }
            }
        }

        private string _availabilityName = string.Empty;
        public string AvailabilityName
        {
            get => _availabilityName;
            set
            {
                if (SetProperty(ref _availabilityName, value))
                    ClearValidationErrors(nameof(AvailabilityName));
            }
        }

        private string _employeeSearchText = string.Empty;
        public string EmployeeSearchText
        {
            get => _employeeSearchText;
            set => SetProperty(ref _employeeSearchText, value);
        }

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand CancelInformationCommand { get; }
        public AsyncRelayCommand CancelEmployeeCommand { get; }
        public AsyncRelayCommand CancelBindCommand { get; }
        public AsyncRelayCommand AddEmployeeCommand { get; }
        public AsyncRelayCommand RemoveEmployeeCommand { get; }
        public AsyncRelayCommand SearchEmployeeCommand { get; }
        public AsyncRelayCommand AddBindCommand { get; }
        public AsyncRelayCommand DeleteBindCommand { get; }

        public event EventHandler? MatrixChanged;

        public AvailabilityEditViewModel(AvailabilityViewModel owner)
        {
            _owner = owner;

            SaveCommand = new AsyncRelayCommand(() => _owner.SaveAsync());
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelInformationCommand = CancelCommand;
            CancelEmployeeCommand = CancelCommand;
            CancelBindCommand = CancelCommand;

            AddEmployeeCommand = new AsyncRelayCommand(AddEmployeeAsync);
            RemoveEmployeeCommand = new AsyncRelayCommand(RemoveEmployeeAsync);
            SearchEmployeeCommand = new AsyncRelayCommand(SearchEmployeeAsync);
            AddBindCommand = new AsyncRelayCommand(() => _owner.AddBindAsync());
            DeleteBindCommand = new AsyncRelayCommand(() => _owner.DeleteBindAsync());

            EnsureDayColumn();
        }

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName is null || !_errors.TryGetValue(propertyName, out var list))
                return Array.Empty<string>();

            return list;
        }

        public void SetEmployees(IEnumerable<EmployeeModel> employees, IReadOnlyDictionary<int, string> nameLookup)
        {
            Employees.Clear();
            _employeeNames.Clear();

            foreach (var employee in employees)
            {
                var name = nameLookup.TryGetValue(employee.Id, out var fullName)
                    ? fullName
                    : $"{employee.FirstName} {employee.LastName}";

                Employees.Add(new EmployeeListItem { Id = employee.Id, FullName = name });
                _employeeNames[employee.Id] = name;
            }
        }

        public void SetBinds(IEnumerable<BindModel> binds)
        {
            Binds.Clear();
            foreach (var bind in binds)
                Binds.Add(BindRow.FromModel(bind));
        }

        public void ResetForNew()
        {
            AvailabilityId = 0;
            AvailabilityName = string.Empty;
            AvailabilityMonth = DateTime.Today.Month;
            AvailabilityYear = DateTime.Today.Year;
            ClearValidationErrors();
            ResetGroupMatrix();
        }

        public void LoadGroup(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days,
            IReadOnlyDictionary<int, string> nameLookup)
        {
            AvailabilityId = group.Id;
            AvailabilityName = group.Name;
            AvailabilityMonth = group.Month;
            AvailabilityYear = group.Year;
            ClearValidationErrors();
            ResetGroupMatrix();

            foreach (var m in members)
            {
                var header = m.Employee is null
                    ? (nameLookup.TryGetValue(m.EmployeeId, out var n) ? n : $"Employee #{m.EmployeeId}")
                    : $"{m.Employee.FirstName} {m.Employee.LastName}";

                TryAddEmployeeColumn(m.EmployeeId, header);
            }

            var dim = DateTime.DaysInMonth(group.Year, group.Month);
            var dayLookup = days
                .GroupBy(d => (d.AvailabilityGroupMemberId, d.DayOfMonth))
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var mb in members)
            {
                var codes = new List<(int day, string code)>(capacity: dim);

                for (var day = 1; day <= dim; day++)
                {
                    if (!dayLookup.TryGetValue((mb.Id, day), out var d))
                    {
                        codes.Add((day, "-"));
                        continue;
                    }

                    var code = d.Kind switch
                    {
                        AvailabilityKind.ANY => "+",
                        AvailabilityKind.NONE => "-",
                        AvailabilityKind.INT => d.IntervalStr ?? "",
                        _ => "-"
                    };

                    codes.Add((day, code));
                }

                SetEmployeeCodes(mb.EmployeeId, codes);
            }
        }

        public void ResetGroupMatrix()
        {
            var toRemove = _employeeIdToColumn.Keys.ToList();
            foreach (var empId in toRemove)
                RemoveEmployeeColumn(empId);

            _groupTable.Rows.Clear();
            RegenerateGroupDays();
        }

        public IReadOnlyList<int> GetSelectedEmployeeIds()
            => _employeeIdToColumn.Keys.ToList();

        public IList<(int employeeId, IList<(int dayOfMonth, string code)> codes)> ReadGroupCodes()
        {
            var result = new List<(int employeeId, IList<(int day, string code)> codes)>();

            foreach (var kv in _employeeIdToColumn)
            {
                var employeeId = kv.Key;
                var colName = kv.Value;

                var list = new List<(int day, string code)>();

                foreach (DataRow r in _groupTable.Rows)
                {
                    var day = Convert.ToInt32(r[DayColumn]);
                    var code = Convert.ToString(r[colName]) ?? string.Empty;
                    list.Add((day, code));
                }

                result.Add((employeeId, list));
            }

            return result;
        }

        public void SetEmployeeCodes(int employeeId, IList<(int dayOfMonth, string code)> codes)
        {
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return;

            var map = codes.ToDictionary(x => x.dayOfMonth, x => x.code ?? string.Empty);

            foreach (DataRow r in _groupTable.Rows)
            {
                var day = Convert.ToInt32(r[DayColumn]);
                r[colName] = map.TryGetValue(day, out var v) ? v : string.Empty;
            }
        }

        public bool TryGetBindValue(string rawKey, out string value)
        {
            value = string.Empty;
            if (!_owner.TryNormalizeKey(rawKey, out var normalizedKey))
                return false;

            foreach (var bind in Binds)
            {
                if (!bind.IsActive) continue;
                if (!string.Equals(bind.Key, normalizedKey, StringComparison.OrdinalIgnoreCase)) continue;

                value = bind.Value ?? string.Empty;
                return true;
            }

            return false;
        }

        public Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)
            => _owner.UpsertBindAsync(bind, ct);

        public string? FormatKeyGesture(Key key, ModifierKeys modifiers)
            => _owner.FormatKeyGesture(key, modifiers);

        public bool TryApplyBindToCell(string columnName, int rowIndex, string bindValue, out int? nextRowIndex)
        {
            nextRowIndex = null;

            if (string.IsNullOrWhiteSpace(columnName) || columnName == DayColumn)
                return false;

            if (rowIndex < 0 || rowIndex >= _groupTable.Rows.Count)
                return false;

            _groupTable.Rows[rowIndex][columnName] = bindValue;

            var nextRow = rowIndex + 1;
            if (nextRow < _groupTable.Rows.Count)
                nextRowIndex = nextRow;

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

        private Task AddEmployeeAsync()
        {
            var empId = SelectedEmployee?.Id ?? 0;
            if (empId <= 0)
            {
                _owner.ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            var header = _employeeNames.TryGetValue(empId, out var name)
                ? name
                : $"Employee #{empId}";

            if (!TryAddEmployeeColumn(empId, header))
                _owner.ShowInfo("This employee is already added.");

            return Task.CompletedTask;
        }

        private Task RemoveEmployeeAsync()
        {
            var empId = SelectedEmployee?.Id ?? 0;
            if (empId <= 0)
            {
                _owner.ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            if (!RemoveEmployeeColumn(empId))
                _owner.ShowInfo("This employee is not in the group.");
            return Task.CompletedTask;
        }

        private Task SearchEmployeeAsync()
        {
            _owner.ApplyEmployeeFilter(EmployeeSearchText);
            return Task.CompletedTask;
        }

        private void EnsureDayColumn()
        {
            if (_groupTable.Columns.Contains(DayColumn)) return;

            var col = new DataColumn(DayColumn, typeof(int))
            {
                Caption = "Day",
                ReadOnly = true
            };

            _groupTable.Columns.Add(col);
        }

        private void RegenerateGroupDays()
        {
            var year = AvailabilityYear;
            var month = AvailabilityMonth;
            if (year <= 0 || month < 1 || month > 12) return;

            var daysInMonth = DateTime.DaysInMonth(year, month);

            EnsureDayColumn();

            var old = new Dictionary<(int day, string col), string>();
            foreach (DataRow r in _groupTable.Rows)
            {
                var d = Convert.ToInt32(r[DayColumn]);
                foreach (DataColumn c in _groupTable.Columns)
                {
                    if (c.ColumnName == DayColumn) continue;
                    old[(d, c.ColumnName)] = Convert.ToString(r[c]) ?? string.Empty;
                }
            }

            _groupTable.Rows.Clear();

            for (var day = 1; day <= daysInMonth; day++)
            {
                var row = _groupTable.NewRow();
                row[DayColumn] = day;

                foreach (DataColumn c in _groupTable.Columns)
                {
                    if (c.ColumnName == DayColumn) continue;
                    row[c.ColumnName] = old.TryGetValue((day, c.ColumnName), out var v) ? v : string.Empty;
                }

                _groupTable.Rows.Add(row);
            }

            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool TryAddEmployeeColumn(int employeeId, string header)
        {
            if (employeeId <= 0) return false;
            if (_employeeIdToColumn.ContainsKey(employeeId)) return false;

            var colName = $"emp_{employeeId}";
            if (_groupTable.Columns.Contains(colName)) return false;

            var col = new DataColumn(colName, typeof(string))
            {
                Caption = header
            };

            _groupTable.Columns.Add(col);
            _employeeIdToColumn[employeeId] = colName;

            foreach (DataRow r in _groupTable.Rows)
                r[colName] = string.Empty;

            MatrixChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private bool RemoveEmployeeColumn(int employeeId)
        {
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return false;

            _employeeIdToColumn.Remove(employeeId);

            if (_groupTable.Columns.Contains(colName))
                _groupTable.Columns.Remove(colName);

            MatrixChanged?.Invoke(this, EventArgs.Empty);
            return true;
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
    }
}
