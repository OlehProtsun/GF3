/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditDesignVM у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFApp.DesignTime
{
    
    /// <summary>
    /// Визначає публічний елемент `public abstract class NotifyBase : INotifyPropertyChanged` та контракт його використання у шарі WPFApp.
    /// </summary>
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Визначає публічний елемент `public event PropertyChangedEventHandler? PropertyChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Raise([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerScheduleEditDesignVM : NotifyBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerScheduleEditDesignVM : NotifyBase
    {
        /// <summary>
        /// Визначає публічний елемент `public string FormTitle { get; set; } = "Edit schedule";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormTitle { get; set; } = "Edit schedule";
        /// <summary>
        /// Визначає публічний елемент `public string FormSubtitle { get; set; } = "January 2026 • Container #A-102";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormSubtitle { get; set; } = "January 2026 • Container #A-102";

        /// <summary>
        /// Визначає публічний елемент `public string ScheduleId { get; set; } = "SCH-000128";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleId { get; set; } = "SCH-000128";
        /// <summary>
        /// Визначає публічний елемент `public string ScheduleName { get; set; } = "Morning + Evening shifts";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleName { get; set; } = "Morning + Evening shifts";

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMonth { get; set; } = 1;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMonth { get; set; } = 1;
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleYear { get; set; } = 2026;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleYear { get; set; } = 2026;

        /// <summary>
        /// Визначає публічний елемент `public int SchedulePeoplePerShift { get; set; } = 6;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int SchedulePeoplePerShift { get; set; } = 6;
        /// <summary>
        /// Визначає публічний елемент `public string ScheduleShift1 { get; set; } = "06:00 - 14:00";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleShift1 { get; set; } = "06:00 - 14:00";
        /// <summary>
        /// Визначає публічний елемент `public string ScheduleShift2 { get; set; } = "14:00 - 22:00";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleShift2 { get; set; } = "14:00 - 22:00";

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxHoursPerEmp { get; set; } = 180;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxHoursPerEmp { get; set; } = 180;
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxConsecutiveDays { get; set; } = 5;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxConsecutiveDays { get; set; } = 5;
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxConsecutiveFull { get; set; } = 3;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxConsecutiveFull { get; set; } = 3;
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxFullPerMonth { get; set; } = 10;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxFullPerMonth { get; set; } = 10;

        /// <summary>
        /// Визначає публічний елемент `public string ScheduleNote { get; set; } =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleNote { get; set; } =
            "Design-time note:\n• Add extra staff on weekends\n• Avoid >2 full shifts in a row";

        /// <summary>
        /// Визначає публічний елемент `public bool CanAddBlock { get; set; } = true;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool CanAddBlock { get; set; } = true;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleBlockVM> OpenedSchedules { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleBlockVM> OpenedSchedules { get; } = new();

        private ScheduleBlockVM? _activeSchedule;
        /// <summary>
        /// Визначає публічний елемент `public ScheduleBlockVM? ActiveSchedule` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleBlockVM? ActiveSchedule
        {
            get => _activeSchedule;
            set { _activeSchedule = value; Raise(); }
        }

        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleBlockVM> Blocks { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleBlockVM> Blocks { get; } = new();

        private ScheduleBlockVM? _selectedBlock;
        /// <summary>
        /// Визначає публічний елемент `public ScheduleBlockVM? SelectedBlock` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleBlockVM? SelectedBlock
        {
            get => _selectedBlock;
            set { _selectedBlock = value; Raise(); }
        }

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ShopVM> Shops { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ShopVM> Shops { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public ShopVM? SelectedShop { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopVM? SelectedShop { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string ShopSearchText { get; set; } = "Warsaw";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ShopSearchText { get; set; } = "Warsaw";

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<AvailabilityGroupVM> AvailabilityGroups { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<AvailabilityGroupVM> AvailabilityGroups { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupVM? SelectedAvailabilityGroup { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityGroupVM? SelectedAvailabilityGroup { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string AvailabilitySearchText { get; set; } = "Default";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilitySearchText { get; set; } = "Default";

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<EmployeeVM> Employees { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<EmployeeVM> Employees { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public EmployeeVM? SelectedEmployee { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeVM? SelectedEmployee { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string EmployeeSearchText { get; set; } = "Ann";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string EmployeeSearchText { get; set; } = "Ann";

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleEmployeeVM> ScheduleEmployees { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleEmployeeVM> ScheduleEmployees { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public ScheduleEmployeeVM? SelectedScheduleEmployee { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleEmployeeVM? SelectedScheduleEmployee { get; set; }

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<MatrixRowVM> ScheduleMatrix { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<MatrixRowVM> ScheduleMatrix { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<MatrixRowVM> AvailabilityPreviewMatrix { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<MatrixRowVM> AvailabilityPreviewMatrix { get; } = new();

        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleEditDesignVM()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleEditDesignVM()
        {
            
            var t1 = new ScheduleBlockVM("A • Main");
            var t2 = new ScheduleBlockVM("B • Weekend");
            var t3 = new ScheduleBlockVM("C • Holiday");
            var t4 = new ScheduleBlockVM("D • Staff+");
            var t5 = new ScheduleBlockVM("E • Test");

            OpenedSchedules.Add(t1);
            OpenedSchedules.Add(t2);
            OpenedSchedules.Add(t3);
            OpenedSchedules.Add(t4);
            OpenedSchedules.Add(t5);

            ActiveSchedule = t3; 

            
            Blocks.Add(t1);
            Blocks.Add(t2);
            Blocks.Add(t3);
            SelectedBlock = t2;

            
            Shops.Add(new ShopVM("Shop 01 • Center"));
            Shops.Add(new ShopVM("Shop 02 • Mokotów"));
            Shops.Add(new ShopVM("Shop 03 • Wola"));
            SelectedShop = Shops[1];

            
            AvailabilityGroups.Add(new AvailabilityGroupVM("Default availability"));
            AvailabilityGroups.Add(new AvailabilityGroupVM("Students (flex)"));
            AvailabilityGroups.Add(new AvailabilityGroupVM("Weekend only"));
            SelectedAvailabilityGroup = AvailabilityGroups[0];

            
            Employees.Add(new EmployeeVM("Anna", "Kowalska"));
            Employees.Add(new EmployeeVM("Oleh", "Ivanov"));
            Employees.Add(new EmployeeVM("Marek", "Nowak"));
            SelectedEmployee = Employees[0];

            
            ScheduleEmployees.Add(new ScheduleEmployeeVM(Employees[0], minHoursMonth: 80));
            ScheduleEmployees.Add(new ScheduleEmployeeVM(Employees[1], minHoursMonth: 120));
            ScheduleEmployees.Add(new ScheduleEmployeeVM(Employees[2], minHoursMonth: 60));
            SelectedScheduleEmployee = ScheduleEmployees[1];

            
            ScheduleMatrix.Add(new MatrixRowVM("Emp / Day", "D1", "D2", "D3", "D4", "D5", "D6", "D7"));
            ScheduleMatrix.Add(new MatrixRowVM("Anna K.", "M", "M", "", "E", "E", "", ""));
            ScheduleMatrix.Add(new MatrixRowVM("Oleh I.", "", "E", "E", "", "M", "M", ""));
            ScheduleMatrix.Add(new MatrixRowVM("Marek N.", "E", "", "M", "M", "", "", "E"));

            AvailabilityPreviewMatrix.Add(new MatrixRowVM("Emp / Day", "D1", "D2", "D3", "D4", "D5", "D6", "D7"));
            AvailabilityPreviewMatrix.Add(new MatrixRowVM("Anna K.", "✓", "✓", "✓", "×", "✓", "×", "×"));
            AvailabilityPreviewMatrix.Add(new MatrixRowVM("Oleh I.", "×", "✓", "✓", "✓", "✓", "✓", "×"));
            AvailabilityPreviewMatrix.Add(new MatrixRowVM("Marek N.", "✓", "×", "✓", "✓", "×", "×", "✓"));
        }
    }

    

    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleBlockVM` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleBlockVM
    {
        /// <summary>
        /// Визначає публічний елемент `public ScheduleBlockModel Model { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleBlockModel Model { get; }
        /// <summary>
        /// Визначає публічний елемент `public ScheduleBlockVM(string name) => Model = new ScheduleBlockModel { Name = name };` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleBlockVM(string name) => Model = new ScheduleBlockModel { Name = name };
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleBlockModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleBlockModel
    {
        /// <summary>
        /// Визначає публічний елемент `public string Name { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class ShopVM` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ShopVM
    {
        /// <summary>
        /// Визначає публічний елемент `public string Name { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public ShopVM(string name) => Name = name;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopVM(string name) => Name = name;
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityGroupVM` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityGroupVM
    {
        /// <summary>
        /// Визначає публічний елемент `public string Name { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupVM(string name) => Name = name;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityGroupVM(string name) => Name = name;
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class EmployeeVM` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class EmployeeVM
    {
        /// <summary>
        /// Визначає публічний елемент `public string FirstName { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string LastName { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public EmployeeVM(string first, string last)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeVM(string first, string last)
        {
            FirstName = first;
            LastName = last;
        }
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleEmployeeVM` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleEmployeeVM
    {
        /// <summary>
        /// Визначає публічний елемент `public EmployeeVM Employee { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeVM Employee { get; }
        /// <summary>
        /// Визначає публічний елемент `public int MinHoursMonth { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int MinHoursMonth { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public ScheduleEmployeeVM(EmployeeVM employee, int minHoursMonth)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleEmployeeVM(EmployeeVM employee, int minHoursMonth)
        {
            Employee = employee;
            MinHoursMonth = minHoursMonth;
        }
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class MatrixRowVM` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class MatrixRowVM
    {
        /// <summary>
        /// Визначає публічний елемент `public string C0 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C0 { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string C1 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C1 { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string C2 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C2 { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string C3 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C3 { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string C4 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C4 { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string C5 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C5 { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string C6 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C6 { get; set; }
        /// <summary>
        /// Визначає публічний елемент `public string C7 { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string C7 { get; set; }

        /// <summary>
        /// Визначає публічний елемент `public MatrixRowVM(string c0, string c1, string c2, string c3, string c4, string c5, string c6, string c7)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public MatrixRowVM(string c0, string c1, string c2, string c3, string c4, string c5, string c6, string c7)
        {
            C0 = c0; C1 = c1; C2 = c2; C3 = c3; C4 = c4; C5 = c5; C6 = c6; C7 = c7;
        }
    }
}
