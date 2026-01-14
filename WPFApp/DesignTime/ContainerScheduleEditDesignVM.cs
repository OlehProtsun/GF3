using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFApp.DesignTime
{
    // Простий базовий клас для биндингів у дизайнері
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Raise([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ===================== DESIGN VM =====================
    public sealed class ContainerScheduleEditDesignVM : NotifyBase
    {
        public string FormTitle { get; set; } = "Edit schedule";
        public string FormSubtitle { get; set; } = "January 2026 • Container #A-102";

        public string ScheduleId { get; set; } = "SCH-000128";
        public string ScheduleName { get; set; } = "Morning + Evening shifts";

        public int ScheduleMonth { get; set; } = 1;
        public int ScheduleYear { get; set; } = 2026;

        public int SchedulePeoplePerShift { get; set; } = 6;
        public string ScheduleShift1 { get; set; } = "06:00 - 14:00";
        public string ScheduleShift2 { get; set; } = "14:00 - 22:00";

        public int ScheduleMaxHoursPerEmp { get; set; } = 180;
        public int ScheduleMaxConsecutiveDays { get; set; } = 5;
        public int ScheduleMaxConsecutiveFull { get; set; } = 3;
        public int ScheduleMaxFullPerMonth { get; set; } = 10;

        public string ScheduleNote { get; set; } =
            "Design-time note:\n• Add extra staff on weekends\n• Avoid >2 full shifts in a row";

        public bool CanAddBlock { get; set; } = true;

        // Chips (Blocks)
        public ObservableCollection<ScheduleBlockVM> Blocks { get; } = new();
        public ScheduleBlockVM? SelectedBlock { get; set; }

        // Shop
        public ObservableCollection<ShopVM> Shops { get; } = new();
        public ShopVM? SelectedShop { get; set; }
        public string ShopSearchText { get; set; } = "Warsaw";

        public ObservableCollection<AvailabilityGroupVM> AvailabilityGroups { get; } = new();
        public AvailabilityGroupVM? SelectedAvailabilityGroup { get; set; }
        public string AvailabilitySearchText { get; set; } = "Default";

        // Employees
        public ObservableCollection<EmployeeVM> Employees { get; } = new();
        public EmployeeVM? SelectedEmployee { get; set; }
        public string EmployeeSearchText { get; set; } = "Ann";

        public ObservableCollection<ScheduleEmployeeVM> ScheduleEmployees { get; } = new();
        public ScheduleEmployeeVM? SelectedScheduleEmployee { get; set; }

        // Matrices
        public ObservableCollection<MatrixRowVM> ScheduleMatrix { get; } = new();
        public ObservableCollection<MatrixRowVM> AvailabilityPreviewMatrix { get; } = new();

        public ContainerScheduleEditDesignVM()
        {
            // ----- Blocks -----
            var b1 = new ScheduleBlockVM("A");
            var b2 = new ScheduleBlockVM("B");
            var b3 = new ScheduleBlockVM("C");

            Blocks.Add(b1);
            Blocks.Add(b2);
            Blocks.Add(b3);
            SelectedBlock = b2;

            // ----- Shops -----
            Shops.Add(new ShopVM("Shop 01 • Center"));
            Shops.Add(new ShopVM("Shop 02 • Mokotów"));
            Shops.Add(new ShopVM("Shop 03 • Wola"));
            SelectedShop = Shops[1];

            // ----- Availability groups -----
            AvailabilityGroups.Add(new AvailabilityGroupVM("Default availability"));
            AvailabilityGroups.Add(new AvailabilityGroupVM("Students (flex)"));
            AvailabilityGroups.Add(new AvailabilityGroupVM("Weekend only"));
            SelectedAvailabilityGroup = AvailabilityGroups[0];

            // ----- Employees list -----
            Employees.Add(new EmployeeVM("Anna", "Kowalska"));
            Employees.Add(new EmployeeVM("Oleh", "Ivanov"));
            Employees.Add(new EmployeeVM("Marek", "Nowak"));
            SelectedEmployee = Employees[0];

            // ----- Schedule employees grid -----
            ScheduleEmployees.Add(new ScheduleEmployeeVM(Employees[0], minHoursMonth: 80));
            ScheduleEmployees.Add(new ScheduleEmployeeVM(Employees[1], minHoursMonth: 120));
            ScheduleEmployees.Add(new ScheduleEmployeeVM(Employees[2], minHoursMonth: 60));
            SelectedScheduleEmployee = ScheduleEmployees[1];

            // ----- Matrices (спрощено: 7 днів як приклад) -----
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

    // ===================== DESIGN MODELS =====================

    public sealed class ScheduleBlockVM
    {
        public ScheduleBlockModel Model { get; }
        public ScheduleBlockVM(string name) => Model = new ScheduleBlockModel { Name = name };
    }

    public sealed class ScheduleBlockModel
    {
        public string Name { get; set; } = "";
    }

    public sealed class ShopVM
    {
        public string Name { get; set; }
        public ShopVM(string name) => Name = name;
    }

    public sealed class AvailabilityGroupVM
    {
        public string Name { get; set; }
        public AvailabilityGroupVM(string name) => Name = name;
    }

    public sealed class EmployeeVM
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public EmployeeVM(string first, string last)
        {
            FirstName = first;
            LastName = last;
        }
    }

    public sealed class ScheduleEmployeeVM
    {
        public EmployeeVM Employee { get; }
        public int MinHoursMonth { get; set; }
        public ScheduleEmployeeVM(EmployeeVM employee, int minHoursMonth)
        {
            Employee = employee;
            MinHoursMonth = minHoursMonth;
        }
    }

    // Простий рядок для DataGrid-матриць (у тебе AutoGenerateColumns=False,
    // але для дизайн-тайму хоча б список рядків з текстом буде видно, якщо колонки реально задані в стилі)
    public sealed class MatrixRowVM
    {
        public string C0 { get; set; }
        public string C1 { get; set; }
        public string C2 { get; set; }
        public string C3 { get; set; }
        public string C4 { get; set; }
        public string C5 { get; set; }
        public string C6 { get; set; }
        public string C7 { get; set; }

        public MatrixRowVM(string c0, string c1, string c2, string c3, string c4, string c5, string c6, string c7)
        {
            C0 = c0; C1 = c1; C2 = c2; C3 = c3; C4 = c4; C5 = c5; C6 = c6; C7 = c7;
        }
    }
}
