using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
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
    /// AvailabilityProfileViewModel — VM для “профілю” AvailabilityGroup (read-only перегляд матриці).
    ///
    /// Важливий момент:
    /// - Ми будуємо DataTable так, щоб Day колонка була ReadOnly (логічно: день не редагується).
    /// - Тому НІКОЛИ не можна присвоювати row[DayColumnName] після того, як рядок вже в таблиці.
    ///   (ReadOnlyException — це очікувана поведінка DataTable.)
    /// </summary>
    public sealed class AvailabilityProfileViewModel : ViewModelBase
    {
        // Єдина “правда” про назву Day колонки.
        private const string DayColumnName = AvailabilityMatrixEngine.DayColumnName;

        // Owner (координатор): навігація / CRUD / повідомлення.
        private readonly AvailabilityViewModel _owner;

        // DataTable для профілю (ми не змінюємо інстанс, лише його вміст).
        private readonly DataTable _profileTable = new();

        // DataView для WPF binding.
        public DataView ProfileAvailabilityMonths => _profileTable.DefaultView;

        // UI selection (як і в інших таблицях).
        private object? _selectedProfileMonth;
        public object? SelectedProfileMonth
        {
            get => _selectedProfileMonth;
            set => SetProperty(ref _selectedProfileMonth, value);
        }

        // Header fields.
        private int _availabilityId;
        public int AvailabilityId
        {
            get => _availabilityId;
            set
            {
                if (!SetProperty(ref _availabilityId, value))
                    return;

                // При зміні Id оновлюємо canExecute для Edit/Delete.
                UpdateGroupCommands();
            }
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

        // Commands.
        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand CancelTableCommand { get; }
        public AsyncRelayCommand AddNewCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        private readonly AsyncRelayCommand[] _groupDependentCommands;

        // Matrix changed (щоб View перебудував колонки).
        public event EventHandler? MatrixChanged;

        public AvailabilityProfileViewModel(AvailabilityViewModel owner)
        {
            // Зберігаємо owner.
            _owner = owner;

            // Один Cancel/Back command.
            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = BackCommand;
            CancelTableCommand = BackCommand;

            // AddNew.
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            // Edit/Delete доступні лише коли є завантажена група (Id > 0).
            EditCommand = new AsyncRelayCommand(
                execute: () => _owner.EditSelectedAsync(),
                canExecute: () => HasLoadedGroup);

            DeleteCommand = new AsyncRelayCommand(
                execute: () => _owner.DeleteSelectedAsync(),
                canExecute: () => HasLoadedGroup);

            _groupDependentCommands = new[] { EditCommand, DeleteCommand };

            // Гарантуємо Day column.
            AvailabilityMatrixEngine.EnsureDayColumn(_profileTable);

            // Day column логічно ReadOnly (але значення day ми ставимо ТІЛЬКИ коли row detached).
            _profileTable.Columns[DayColumnName].ReadOnly = true;
        }

        private bool HasLoadedGroup => AvailabilityId > 0;

        private void UpdateGroupCommands()
        {
            for (int i = 0; i < _groupDependentCommands.Length; i++)
                _groupDependentCommands[i].RaiseCanExecuteChanged();
        }

        /// <summary>
        /// SetProfile — завантажити дані профілю і перебудувати таблицю.
        /// </summary>
        public void SetProfile(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days)
        {
            // 1) Захист від null — зручна діагностика.
            if (group is null) throw new ArgumentNullException(nameof(group));
            if (members is null) throw new ArgumentNullException(nameof(members));
            if (days is null) throw new ArgumentNullException(nameof(days));

            // 2) Синхронізуємо selection у owner.ListVm.
            //    Це потрібно, бо owner.EditSelectedAsync/DeleteSelectedAsync беруть SelectedItem зі списку.
            _owner.ListVm.SelectedItem = group;

            // 3) Header.
            AvailabilityId = group.Id;
            AvailabilityName = group.Name ?? string.Empty;
            AvailabilityMonthYear = $"{group.Month:D2}-{group.Year}";

            // 4) Reset таблиці:
            //    - прибираємо employee колонки
            //    - чистимо rows
            //    - генеруємо day rows 1..N для month/year
            //
            // Важливо:
            // - Day column ReadOnly, але engine заповнює day значення,
            //   поки рядок detached (NewRow), тому exception не буде.
            AvailabilityMatrixEngine.Reset(
                table: _profileTable,
                regenerateDays: true,
                year: group.Year,
                month: group.Month);

            // 5) Переконуємось, що Day column лишається ReadOnly (на випадок змін engine).
            _profileTable.Columns[DayColumnName].ReadOnly = true;

            // 6) Map memberId -> columnName (бо days прив’язані до member.Id).
            var memberIdToCol = new Dictionary<int, string>(capacity: members.Count);

            // 7) Запам’ятаємо список employee колонок, щоб після заповнення зробити їх ReadOnly.
            //    (НЕ робимо ReadOnly одразу, бо нам треба записати значення у row[colName].)
            var employeeColumns = new HashSet<string>(StringComparer.Ordinal);

            // 8) Додаємо employee колонки.
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];

                // Caption/заголовок колонки.
                var header = m.Employee is null
                    ? $"Employee #{m.EmployeeId}"
                    : $"{m.Employee.FirstName} {m.Employee.LastName}";

                // Додаємо колонку (якщо дублікати EmployeeId — TryAdd поверне false).
                var added = AvailabilityMatrixEngine.TryAddEmployeeColumn(
                    table: _profileTable,
                    employeeId: m.EmployeeId,
                    header: header,
                    columnName: out var colName);

                // Якщо не додалося — все одно знаємо стандартну назву.
                if (!added)
                    colName = AvailabilityMatrixEngine.GetEmployeeColumnName(m.EmployeeId);

                // Фіксуємо mapping member -> column.
                memberIdToCol[m.Id] = colName;

                // Запам’ятовуємо як employee колонку (для ReadOnly після fill).
                employeeColumns.Add(colName);
            }

            // 9) Будуємо lookup (memberId, day) -> DayModel, без падіння на дублікатах ключа.
            //    Беремо останній запис як “правильний”.
            var dayLookup = days
                .GroupBy(d => (d.AvailabilityGroupMemberId, d.DayOfMonth))
                .ToDictionary(g => g.Key, g => g.Last());

            // 10) Заповнюємо клітинки.
            //     Рядки вже існують (1..N), day = rowIndex+1.
            int dim = _profileTable.Rows.Count;

            for (int rowIndex = 0; rowIndex < dim; rowIndex++)
            {
                int day = rowIndex + 1;
                var row = _profileTable.Rows[rowIndex];

                // !!! ВАЖЛИВО !!!
                // НЕ РОБИМО: row[DayColumnName] = day;
                // Бо DayColumn ReadOnly і рядок вже в таблиці -> ReadOnlyException.
                //
                // Якщо хочеш sanity-check — робимо лише перевірку (без присвоєння):
#if DEBUG
                // Якщо engine колись зміниться і не заповнить Day — це буде видно у дебазі.
                var existingDay = Convert.ToInt32(row[DayColumnName]);
                if (existingDay != day)
                {
                    // Не кидаємо exception у релізі. У debug можна ставити breakpoint тут.
                    // Це лише діагностичний сигнал.
                }
#endif

                // Заповнюємо employee колонки.
                for (int mi = 0; mi < members.Count; mi++)
                {
                    var m = members[mi];

                    // Яка колонка для цього member.
                    if (!memberIdToCol.TryGetValue(m.Id, out var colName))
                        continue;

                    // Знаходимо запис.
                    if (!dayLookup.TryGetValue((m.Id, day), out var d))
                    {
                        // Якщо запису нема — показуємо "-"
                        row[colName] = AvailabilityCellCodeParser.NoneMark;
                        continue;
                    }

                    // Є запис — перетворюємо у відображуваний код.
                    row[colName] = ToProfileCellCode(d);
                }
            }

            // 11) Тепер, коли ми все заповнили, можемо зробити employee колонки ReadOnly.
            //     Це додатковий “data-level захист”, а не лише UI-level.
            foreach (var colName in employeeColumns)
            {
                if (_profileTable.Columns.Contains(colName))
                    _profileTable.Columns[colName].ReadOnly = true;
            }

            // 12) Повідомляємо view, що матриця перебудована.
            NotifyMatrixChanged();
        }

        /// <summary>
        /// Перетворення доменного DayModel у код для клітинки профілю:
        /// - ANY  => "+"
        /// - NONE => "-"
        /// - INT  => "HH:mm-HH:mm" (нормалізований формат, якщо можливо)
        /// </summary>
        private static string ToProfileCellCode(AvailabilityGroupDayModel d)
        {
            // intervalStr може бути null.
            var intervalRaw = d.IntervalStr ?? string.Empty;

            return d.Kind switch
            {
                AvailabilityKind.ANY => AvailabilityCellCodeParser.AnyMark,
                AvailabilityKind.NONE => AvailabilityCellCodeParser.NoneMark,
                AvailabilityKind.INT => NormalizeInterval(intervalRaw),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Нормалізація інтервалу для профілю:
        /// - якщо валідно: повертаємо канонічний "HH:mm-HH:mm"
        /// - якщо невалідно: показуємо як є (краще показати дані, ніж “загубити”)
        /// </summary>
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
