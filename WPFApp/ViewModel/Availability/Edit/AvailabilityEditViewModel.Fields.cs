/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewModel.Fields у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation;
using WPFApp.ViewModel.Availability.Helpers;
using WPFApp.ViewModel.Availability.Main;

namespace WPFApp.ViewModel.Availability.Edit
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityEditViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel : ViewModelBase
    {
        
        private static readonly string DayColumnName = AvailabilityMatrixEngine.DayColumnName;

        
        private readonly AvailabilityViewModel _owner;

        
        private readonly ValidationErrors _validation = new();

        
        private readonly DataTable _groupTable = new();

        
        /// <summary>
        /// Визначає публічний елемент `public DataView AvailabilityDays => _groupTable.DefaultView;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView AvailabilityDays => _groupTable.DefaultView;

        
        private readonly Dictionary<int, string> _employeeIdToColumn = new();

        
        private readonly Dictionary<int, string> _employeeNames = new();

        
        private readonly Dictionary<string, string> _activeBindCache = new(StringComparer.OrdinalIgnoreCase);

        
        private bool _bindCacheDirty = true;

        
        private readonly Dictionary<BindRow, PropertyChangedEventHandler> _bindRowHandlers = new();

        
        private int _matrixUpdateDepth;
        private bool _pendingMatrixChanged;

        
        private bool _suppressColumnChangedHandler;

        
        private int _dateSyncDepth;

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<EmployeeListItem> Employees { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<EmployeeListItem> Employees { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<BindRow> Binds { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<BindRow> Binds { get; } = new();

        
        
        

        private EmployeeListItem? _selectedEmployee;

        /// <summary>
        /// Визначає публічний елемент `public EmployeeListItem? SelectedEmployee` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeListItem? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                
                
                if (SetProperty(ref _selectedEmployee, value))
                {
                    
                    OnPropertyChanged(nameof(SelectedEmployeeId));
                }
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public string SelectedEmployeeId =>` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string SelectedEmployeeId =>
            
            SelectedEmployee?.Id > 0
                ? SelectedEmployee.Id.ToString()
                
                : string.Empty;

        private BindRow? _selectedBind;

        /// <summary>
        /// Визначає публічний елемент `public BindRow? SelectedBind` та контракт його використання у шарі WPFApp.
        /// </summary>
        public BindRow? SelectedBind
        {
            get => _selectedBind;
            set => SetProperty(ref _selectedBind, value);
        }

        private object? _selectedAvailabilityDay;

        /// <summary>
        /// Визначає публічний елемент `public object? SelectedAvailabilityDay` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object? SelectedAvailabilityDay
        {
            get => _selectedAvailabilityDay;
            set => SetProperty(ref _selectedAvailabilityDay, value);
        }

        
        
        

        private int _availabilityId;

        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityId
        {
            get => _availabilityId;
            set => SetProperty(ref _availabilityId, value);
        }

        private int _availabilityMonth;

        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityMonth` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityMonth
        {
            get => _availabilityMonth;
            set
            {
                
                if (!SetProperty(ref _availabilityMonth, value))
                    return;

                
                ClearValidationErrors(nameof(AvailabilityMonth));

                
                ValidateProperty(nameof(AvailabilityMonth));

                
                if (_dateSyncDepth > 0)
                    return;

                
                RegenerateGroupDays();
            }
        }

        private int _availabilityYear;

        /// <summary>
        /// Визначає публічний елемент `public int AvailabilityYear` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int AvailabilityYear
        {
            get => _availabilityYear;
            set
            {
                
                if (!SetProperty(ref _availabilityYear, value))
                    return;

                
                ClearValidationErrors(nameof(AvailabilityYear));

                
                ValidateProperty(nameof(AvailabilityYear));

                
                if (_dateSyncDepth > 0)
                    return;

                
                RegenerateGroupDays();
            }
        }

        private string _availabilityName = string.Empty;

        /// <summary>
        /// Визначає публічний елемент `public string AvailabilityName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilityName
        {
            get => _availabilityName;
            set
            {
                
                if (!SetProperty(ref _availabilityName, value))
                    return;

                
                ClearValidationErrors(nameof(AvailabilityName));

                
                ValidateProperty(nameof(AvailabilityName));
            }
        }

        private string _employeeSearchText = string.Empty;

        /// <summary>
        /// Визначає публічний елемент `public string EmployeeSearchText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string EmployeeSearchText
        {
            get => _employeeSearchText;
            set => SetProperty(ref _employeeSearchText, value);
        }
    }
}
