/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityProfileViewDesignVM у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WPFApp.View.Availability.DesignTime
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityProfileViewDesignVM : INotifyPropertyChanged` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityProfileViewDesignVM : INotifyPropertyChanged
    {
        /// <summary>
        /// Визначає публічний елемент `public event PropertyChangedEventHandler? PropertyChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityId { get; set; } = 101;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityId { get; set; } = 101;
        /// <summary>
        /// Визначає публічний елемент `public string AvailabilityName { get; set; } = "Default Availability";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilityName { get; set; } = "Default Availability";
        /// <summary>
        /// Визначає публічний елемент `public string AvailabilityMonthYear { get; set; } = "January 2026";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilityMonthYear { get; set; } = "January 2026";

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<AvailabilityProfileRow> ProfileAvailabilityMonths { get; set; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<AvailabilityProfileRow> ProfileAvailabilityMonths { get; set; }

        private AvailabilityProfileRow? _selectedProfileMonth;
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityProfileRow? SelectedProfileMonth` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityProfileRow? SelectedProfileMonth
        {
            get => _selectedProfileMonth;
            set { _selectedProfileMonth = value; OnPropertyChanged(); }
        }

        
        /// <summary>
        /// Визначає публічний елемент `public ICommand BackCommand { get; } = new DesignCommand();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand BackCommand { get; } = new DesignCommand();
        /// <summary>
        /// Визначає публічний елемент `public ICommand CancelProfileCommand { get; } = new DesignCommand();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand CancelProfileCommand { get; } = new DesignCommand();
        /// <summary>
        /// Визначає публічний елемент `public ICommand DeleteCommand { get; } = new DesignCommand();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand DeleteCommand { get; } = new DesignCommand();
        /// <summary>
        /// Визначає публічний елемент `public ICommand CancelTableCommand { get; } = new DesignCommand();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand CancelTableCommand { get; } = new DesignCommand();
        /// <summary>
        /// Визначає публічний елемент `public ICommand EditCommand { get; } = new DesignCommand();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand EditCommand { get; } = new DesignCommand();

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityProfileViewDesignVM()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityProfileViewDesignVM()
        {
            ProfileAvailabilityMonths = new ObservableCollection<AvailabilityProfileRow>
            {
                new AvailabilityProfileRow { Month = "January",  Week1="✔", Week2="✔", Week3="—", Week4="✔", Notes="Good" },
                new AvailabilityProfileRow { Month = "February", Week1="—", Week2="✔", Week3="✔", Week4="—", Notes="Busy" },
                new AvailabilityProfileRow { Month = "March",    Week1="✔", Week2="—", Week3="—", Week4="✔", Notes="OK" },
                new AvailabilityProfileRow { Month = "April",    Week1="✔", Week2="✔", Week3="✔", Week4="✔", Notes="Free" },
            };

            SelectedProfileMonth = ProfileAvailabilityMonths[0];
        }

        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityProfileRow` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityProfileRow
    {
        /// <summary>
        /// Визначає публічний елемент `public string Month { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Month { get; set; } = "";
        /// <summary>
        /// Визначає публічний елемент `public string Week1 { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Week1 { get; set; } = "";
        /// <summary>
        /// Визначає публічний елемент `public string Week2 { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Week2 { get; set; } = "";
        /// <summary>
        /// Визначає публічний елемент `public string Week3 { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Week3 { get; set; } = "";
        /// <summary>
        /// Визначає публічний елемент `public string Week4 { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Week4 { get; set; } = "";
        /// <summary>
        /// Визначає публічний елемент `public string Notes { get; set; } = "";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Notes { get; set; } = "";
    }

    internal sealed class DesignCommand : ICommand
    {
        /// <summary>
        /// Визначає публічний елемент `public bool CanExecute(object? parameter) => true;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool CanExecute(object? parameter) => true;
        /// <summary>
        /// Визначає публічний елемент `public void Execute(object? parameter) { }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Execute(object? parameter) { }
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? CanExecuteChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? CanExecuteChanged;
    }
}
