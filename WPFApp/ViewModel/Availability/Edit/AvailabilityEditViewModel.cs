/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.MVVM.Commands;
using WPFApp.ViewModel.Availability.Main;

namespace WPFApp.ViewModel.Availability.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? MatrixChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? MatrixChanged;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityEditViewModel(AvailabilityViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityEditViewModel(AvailabilityViewModel owner)
        {
            
            _owner = owner;

            
            
            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);

            
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());

            
            CancelInformationCommand = CancelCommand;
            CancelEmployeeCommand = CancelCommand;
            CancelBindCommand = CancelCommand;

            
            AddEmployeeCommand = new AsyncRelayCommand(AddEmployeeAsync);
            RemoveEmployeeCommand = new AsyncRelayCommand(RemoveEmployeeAsync);
            SearchEmployeeCommand = new AsyncRelayCommand(SearchEmployeeAsync);

            
            AddBindCommand = new AsyncRelayCommand(() => _owner.AddBindAsync());
            DeleteBindCommand = new AsyncRelayCommand(() => _owner.DeleteBindAsync());

            
            
            AvailabilityMatrixEngine.EnsureDayColumn(_groupTable);

            
            
            
            
            _groupTable.ColumnChanged += GroupTable_ColumnChanged;

            
            
            
            Binds.CollectionChanged += Binds_CollectionChanged;
        }
    }
}
