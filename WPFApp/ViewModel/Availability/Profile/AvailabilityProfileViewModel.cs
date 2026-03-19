/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityProfileViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Availability.Main;

namespace WPFApp.ViewModel.Availability.Profile
{
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityProfileViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityProfileViewModel : ViewModelBase
    {
        
        private static readonly string DayColumnName = AvailabilityMatrixEngine.DayColumnName;

        
        private readonly AvailabilityViewModel _owner;

        
        private readonly DataTable _profileTable = new();

        
        /// <summary>
        /// Визначає публічний елемент `public DataView ProfileAvailabilityMonths => _profileTable.DefaultView;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView ProfileAvailabilityMonths => _profileTable.DefaultView;

        
        private object? _selectedProfileMonth;
        /// <summary>
        /// Визначає публічний елемент `public object? SelectedProfileMonth` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object? SelectedProfileMonth
        {
            get => _selectedProfileMonth;
            set => SetProperty(ref _selectedProfileMonth, value);
        }

        
        private int _availabilityId;
        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityId
        {
            get => _availabilityId;
            set
            {
                if (!SetProperty(ref _availabilityId, value))
                    return;

                
                UpdateGroupCommands();
            }
        }

        private string _availabilityName = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string AvailabilityName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilityName
        {
            get => _availabilityName;
            set => SetProperty(ref _availabilityName, value);
        }

        private string _availabilityMonthYear = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string AvailabilityMonthYear` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilityMonthYear
        {
            get => _availabilityMonthYear;
            set => SetProperty(ref _availabilityMonthYear, value);
        }

        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand BackCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand BackCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelProfileCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelProfileCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelTableCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelTableCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand AddNewCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand AddNewCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand EditCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand DeleteCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }

        private readonly AsyncRelayCommand[] _groupDependentCommands;

        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? MatrixChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? MatrixChanged;

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityProfileViewModel(AvailabilityViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityProfileViewModel(AvailabilityViewModel owner)
        {
            
            _owner = owner;

            
            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = BackCommand;
            CancelTableCommand = BackCommand;

            
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            
            EditCommand = new AsyncRelayCommand(
                execute: () => _owner.EditSelectedAsync(),
                canExecute: () => HasLoadedGroup);

            DeleteCommand = new AsyncRelayCommand(
                execute: () => _owner.DeleteSelectedAsync(),
                canExecute: () => HasLoadedGroup);

            _groupDependentCommands = new[] { EditCommand, DeleteCommand };

            
            AvailabilityMatrixEngine.EnsureDayColumn(_profileTable);

            
            _profileTable.Columns[DayColumnName].ReadOnly = true;
        }

        private bool HasLoadedGroup => AvailabilityId > 0;

        private void UpdateGroupCommands()
        {
            for (int i = 0; i < _groupDependentCommands.Length; i++)
                _groupDependentCommands[i].RaiseCanExecuteChanged();
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetProfile(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetProfile(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days)
        {
            
            if (group is null) throw new ArgumentNullException(nameof(group));
            if (members is null) throw new ArgumentNullException(nameof(members));
            if (days is null) throw new ArgumentNullException(nameof(days));

            
            
            _owner.ListVm.SelectedItem = group;

            
            AvailabilityId = group.Id;
            AvailabilityName = group.Name ?? string.Empty;
            AvailabilityMonthYear = $"{group.Month:D2}-{group.Year}";

            
            
            
            
            
            
            
            
            AvailabilityMatrixEngine.Reset(
                table: _profileTable,
                regenerateDays: true,
                year: group.Year,
                month: group.Month);

            
            _profileTable.Columns[DayColumnName].ReadOnly = true;

            
            var memberIdToCol = new Dictionary<int, string>(capacity: members.Count);

            
            
            var employeeColumns = new HashSet<string>(StringComparer.Ordinal);

            
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];

                
                var header = m.Employee is null
                    ? $"Employee #{m.EmployeeId}"
                    : $"{m.Employee.FirstName} {m.Employee.LastName}";

                
                var added = AvailabilityMatrixEngine.TryAddEmployeeColumn(
                    table: _profileTable,
                    employeeId: m.EmployeeId,
                    header: header,
                    columnName: out var colName);

                
                if (!added)
                    colName = AvailabilityMatrixEngine.GetEmployeeColumnName(m.EmployeeId);

                
                memberIdToCol[m.Id] = colName;

                
                employeeColumns.Add(colName);
            }

            
            
            var dayLookup = days
                .GroupBy(d => (d.AvailabilityGroupMemberId, d.DayOfMonth))
                .ToDictionary(g => g.Key, g => g.Last());

            
            
            int dim = _profileTable.Rows.Count;

            for (int rowIndex = 0; rowIndex < dim; rowIndex++)
            {
                int day = rowIndex + 1;
                var row = _profileTable.Rows[rowIndex];

                
                
                
                
                
#if DEBUG
                
                var existingDay = Convert.ToInt32(row[DayColumnName]);
                if (existingDay != day)
                {
                    
                    
                }
#endif

                
                for (int mi = 0; mi < members.Count; mi++)
                {
                    var m = members[mi];

                    
                    if (!memberIdToCol.TryGetValue(m.Id, out var colName))
                        continue;

                    
                    if (!dayLookup.TryGetValue((m.Id, day), out var d))
                    {
                        
                        row[colName] = AvailabilityCellCodeParser.NoneMark;
                        continue;
                    }

                    
                    row[colName] = ToProfileCellCode(d);
                }
            }

            
            
            foreach (var colName in employeeColumns)
            {
                if (_profileTable.Columns.Contains(colName))
                    _profileTable.Columns[colName].ReadOnly = true;
            }

            
            NotifyMatrixChanged();
        }

        
        
        
        
        
        
        private static string ToProfileCellCode(AvailabilityGroupDayModel d)
        {
            
            var intervalRaw = d.IntervalStr ?? string.Empty;

            return d.Kind switch
            {
                AvailabilityKind.ANY => AvailabilityCellCodeParser.AnyMark,
                AvailabilityKind.NONE => AvailabilityCellCodeParser.NoneMark,
                AvailabilityKind.INT => NormalizeInterval(intervalRaw),
                _ => string.Empty
            };
        }

        
        
        
        
        
        private static string NormalizeInterval(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            return AvailabilityMatrixEngine.TryNormalizeCell(raw, out var normalized, out _)
                ? normalized
                : raw;
        }

        private void NotifyMatrixChanged()
        {
            MatrixChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(ProfileAvailabilityMonths));
        }
    }
}
