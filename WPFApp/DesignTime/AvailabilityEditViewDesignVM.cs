using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.MVVM.Commands;
using WPFApp.ViewModel.Availability;
using WPFApp.ViewModel.Availability.Helpers; // EmployeeListItem, BindRow

namespace WPFApp.View.Availability.DesignTime
{
    /// <summary>
    /// AvailabilityEditViewDesignVM — design-time VM ТІЛЬКИ для XAML Designer.
    ///
    /// Навіщо:
    /// - Реальний AvailabilityEditViewModel потребує owner/service DI, тому Designer не може його створити.
    /// - DesignerInstance у XAML очікує parameterless тип.
    /// - Цей клас дає “підробні” дані, щоб верстка у Designer була живою.
    ///
    /// Важливо:
    /// - ЦЕ НЕ RUNTIME VM.
    /// - У продакшені DataContext задається реальним AvailabilityEditViewModel.
    /// </summary>
    public sealed class AvailabilityEditViewDesignVM
    {
        // ----------------------------
        // 1) Команди (заглушки)
        // ----------------------------

        // Заглушки команд: Designer не повинен виконувати бізнес-логіку.
        public ICommand CancelCommand { get; }
        public ICommand CancelInformationCommand { get; }
        public ICommand CancelEmployeeCommand { get; }
        public ICommand CancelBindCommand { get; }
        public ICommand SaveCommand { get; }

        public ICommand SearchEmployeeCommand { get; }
        public ICommand AddEmployeeCommand { get; }
        public ICommand RemoveEmployeeCommand { get; }

        public ICommand AddBindCommand { get; }
        public ICommand DeleteBindCommand { get; }

        // ----------------------------
        // 2) Поля форми (Information)
        // ----------------------------

        public int AvailabilityId { get; set; } = 12;
        public int AvailabilityMonth { get; set; } = 8;
        public int AvailabilityYear { get; set; } = 2026;
        public string AvailabilityName { get; set; } = "Design-time availability group";

        // ----------------------------
        // 3) Employees (ліва панель)
        // ----------------------------

        public string EmployeeSearchText { get; set; } = "iv";

        public ObservableCollection<EmployeeListItem> Employees { get; } = new();

        public EmployeeListItem? SelectedEmployee { get; set; }

        // У тебе у VM це derived property, тому тут теж робимо так само.
        public string SelectedEmployeeId => SelectedEmployee?.Id > 0
            ? SelectedEmployee.Id.ToString()
            : string.Empty;

        // ----------------------------
        // 4) Binds (ліва панель)
        // ----------------------------

        public ObservableCollection<BindRow> Binds { get; } = new();
        public BindRow? SelectedBind { get; set; }

        // ----------------------------
        // 5) Matrix (права панель)
        // ----------------------------

        // DataView — як у runtime VM, бо твій DataGrid прив’язаний до AvailabilityDays.
        public DataView AvailabilityDays => _table.DefaultView;

        // Selected row placeholder (DataGrid SelectedItem), у Designer це може бути null.
        public object? SelectedAvailabilityDay { get; set; }

        // Внутрішня таблиця, щоб показати демо-матрицю.
        private readonly DataTable _table = new();

        public AvailabilityEditViewDesignVM()
        {
            // 1) Ініціалізуємо команди “нічого не роблять”.
            //    AsyncRelayCommand вже є в твоєму проекті, тому використовуємо його як стабільну заглушку.
            CancelCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            CancelInformationCommand = CancelCommand;
            CancelEmployeeCommand = CancelCommand;
            CancelBindCommand = CancelCommand;

            SaveCommand = new AsyncRelayCommand(() => Task.CompletedTask);

            SearchEmployeeCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            AddEmployeeCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            RemoveEmployeeCommand = new AsyncRelayCommand(() => Task.CompletedTask);

            AddBindCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            DeleteBindCommand = new AsyncRelayCommand(() => Task.CompletedTask);

            // 2) Наповнюємо Employees тестовими даними для Designer.
            Employees.Add(new EmployeeListItem { Id = 1, FullName = "Ivan Petrenko" });
            Employees.Add(new EmployeeListItem { Id = 2, FullName = "Olena Koval" });
            Employees.Add(new EmployeeListItem { Id = 3, FullName = "Serhii Bondar" });

            // 3) Виставляємо SelectedEmployee (щоб у UI показувався Employee ID).
            SelectedEmployee = Employees[0];

            // 4) Наповнюємо Binds тестовими даними.
            Binds.Add(new BindRow { Id = 101, Key = "1", Value = "+", IsActive = true });
            Binds.Add(new BindRow { Id = 102, Key = "2", Value = "-", IsActive = true });
            Binds.Add(new BindRow { Id = 103, Key = "Ctrl+M", Value = "09:00-18:00", IsActive = true });

            SelectedBind = Binds[0];

            // 5) Будуємо демо-матрицю через engine-конвенції (Day column + emp columns).
            AvailabilityMatrixEngine.EnsureDayColumn(_table);

            // Додаємо 2 працівники як колонки.
            AvailabilityMatrixEngine.TryAddEmployeeColumn(_table, 1, "Ivan Petrenko", out _);
            AvailabilityMatrixEngine.TryAddEmployeeColumn(_table, 2, "Olena Koval", out _);

            // Генеруємо 7 днів (для Designer достатньо).
            // Engine вміє робити по місяцю, але тут швидше вручну показати 1..7.
            for (int day = 1; day <= 7; day++)
            {
                var row = _table.NewRow();
                row[AvailabilityMatrixEngine.DayColumnName] = day;

                row[AvailabilityMatrixEngine.GetEmployeeColumnName(1)] = day % 2 == 0 ? "+" : "09:00-18:00";
                row[AvailabilityMatrixEngine.GetEmployeeColumnName(2)] = day % 3 == 0 ? "-" : "+";

                _table.Rows.Add(row);
            }
        }
    }
}
