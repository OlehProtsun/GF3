/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewDesignVM у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.MVVM.Commands;
using WPFApp.ViewModel.Availability;
using WPFApp.ViewModel.Availability.Helpers; 

namespace WPFApp.View.Availability.DesignTime
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityEditViewDesignVM` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityEditViewDesignVM
    {
        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public ICommand CancelCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand CancelCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public ICommand CancelInformationCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand CancelInformationCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public ICommand CancelEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand CancelEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public ICommand CancelBindCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand CancelBindCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public ICommand SaveCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Визначає публічний елемент `public ICommand SearchEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand SearchEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public ICommand AddEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand AddEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public ICommand RemoveEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand RemoveEmployeeCommand { get; }

        /// <summary>
        /// Визначає публічний елемент `public ICommand AddBindCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand AddBindCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public ICommand DeleteBindCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand DeleteBindCommand { get; }

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityId { get; set; } = 12;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityId { get; set; } = 12;
        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityMonth { get; set; } = 8;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityMonth { get; set; } = 8;
        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityYear { get; set; } = 2026;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityYear { get; set; } = 2026;
        /// <summary>
        /// Визначає публічний елемент `public string AvailabilityName { get; set; } = "Design-time availability group";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilityName { get; set; } = "Design-time availability group";

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public string EmployeeSearchText { get; set; } = "iv";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string EmployeeSearchText { get; set; } = "iv";

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<EmployeeListItem> Employees { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<EmployeeListItem> Employees { get; } = new();

        /// <summary>
        /// Визначає публічний елемент `public EmployeeListItem? SelectedEmployee { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeListItem? SelectedEmployee { get; set; }

        
        /// <summary>
        /// Визначає публічний елемент `public string SelectedEmployeeId => SelectedEmployee?.Id > 0` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string SelectedEmployeeId => SelectedEmployee?.Id > 0
            ? SelectedEmployee.Id.ToString()
            : string.Empty;

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<BindRow> Binds { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<BindRow> Binds { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public BindRow? SelectedBind { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public BindRow? SelectedBind { get; set; }

        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public DataView AvailabilityDays => _table.DefaultView;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView AvailabilityDays => _table.DefaultView;

        
        /// <summary>
        /// Визначає публічний елемент `public object? SelectedAvailabilityDay { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object? SelectedAvailabilityDay { get; set; }

        
        private readonly DataTable _table = new();

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityEditViewDesignVM()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityEditViewDesignVM()
        {
            
            
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

            
            Employees.Add(new EmployeeListItem { Id = 1, FullName = "Ivan Petrenko" });
            Employees.Add(new EmployeeListItem { Id = 2, FullName = "Olena Koval" });
            Employees.Add(new EmployeeListItem { Id = 3, FullName = "Serhii Bondar" });

            
            SelectedEmployee = Employees[0];

            
            Binds.Add(new BindRow { Id = 101, Key = "1", Value = "+", IsActive = true });
            Binds.Add(new BindRow { Id = 102, Key = "2", Value = "-", IsActive = true });
            Binds.Add(new BindRow { Id = 103, Key = "Ctrl+M", Value = "09:00-18:00", IsActive = true });

            SelectedBind = Binds[0];

            
            AvailabilityMatrixEngine.EnsureDayColumn(_table);

            
            AvailabilityMatrixEngine.TryAddEmployeeColumn(_table, 1, "Ivan Petrenko", out _);
            AvailabilityMatrixEngine.TryAddEmployeeColumn(_table, 2, "Olena Koval", out _);

            
            
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
