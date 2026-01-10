using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Availability
{
    public sealed class AvailabilityProfileViewModel : ViewModelBase
    {
        private const string DayColumn = "DayOfMonth";

        private readonly AvailabilityViewModel _owner;
        private readonly DataTable _profileTable = new();

        public DataView ProfileAvailabilityMonths => _profileTable.DefaultView;

        private object? _selectedProfileMonth;
        public object? SelectedProfileMonth
        {
            get => _selectedProfileMonth;
            set => SetProperty(ref _selectedProfileMonth, value);
        }

        private int _availabilityId;
        public int AvailabilityId
        {
            get => _availabilityId;
            set => SetProperty(ref _availabilityId, value);
        }

        private string _availabilityName = string.Empty;
        public string AvailabilityName
        {
            get => _availabilityName;
            set => SetProperty(ref _availabilityName, value);
        }

        private string _availabilityMonthYear = string.Empty;
        public string AvailabilityMonthYear
        {
            get => _availabilityMonthYear;
            set => SetProperty(ref _availabilityMonthYear, value);
        }

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand AddNewCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand CancelTableCommand { get; }
        public AsyncRelayCommand EditCommand { get; }

        public event EventHandler? MatrixChanged;

        public AvailabilityProfileViewModel(AvailabilityViewModel owner)
        {
            _owner = owner;

            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
            CancelTableCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
        }

        public void SetProfile(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days)
        {
            AvailabilityId = group.Id;
            AvailabilityName = group.Name;
            AvailabilityMonthYear = $"{group.Month}-{group.Year}";

            _profileTable.Clear();
            _profileTable.Columns.Clear();
            _profileTable.Columns.Add(new DataColumn(DayColumn, typeof(int))
            {
                Caption = "Day",
                ReadOnly = true
            });

            var memberIdToCol = new Dictionary<int, string>();

            foreach (var m in members)
            {
                var colName = $"emp_{m.EmployeeId}";
                memberIdToCol[m.Id] = colName;

                var header = m.Employee is null
                    ? $"Employee #{m.EmployeeId}"
                    : $"{m.Employee.FirstName} {m.Employee.LastName}";

                _profileTable.Columns.Add(new DataColumn(colName, typeof(string))
                {
                    Caption = header
                });
            }

            var daysMap = days.ToDictionary(
                d => (d.AvailabilityGroupMemberId, d.DayOfMonth),
                d => d.Kind switch
                {
                    AvailabilityKind.ANY => "+",
                    AvailabilityKind.NONE => "-",
                    AvailabilityKind.INT => d.IntervalStr ?? "",
                    _ => ""
                });

            var dim = DateTime.DaysInMonth(group.Year, group.Month);
            for (var day = 1; day <= dim; day++)
            {
                var row = _profileTable.NewRow();
                row[DayColumn] = day;

                foreach (var m in members)
                {
                    var colName = memberIdToCol[m.Id];
                    row[colName] = daysMap.TryGetValue((m.Id, day), out var v) ? v : "-";
                }

                _profileTable.Rows.Add(row);
            }

            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
