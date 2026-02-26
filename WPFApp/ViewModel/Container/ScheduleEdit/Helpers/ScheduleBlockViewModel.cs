/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleBlockViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BusinessLogicLayer.Contracts.Models;
using WPFApp.MVVM.Core;

namespace WPFApp.ViewModel.Container.ScheduleEdit.Helpers
{
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleBlockViewModel : ObservableObject` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleBlockViewModel : ObservableObject
    {
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Guid Id { get; } = Guid.NewGuid();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        
        
        
        
        
        
        private ScheduleModel _model = new();
        /// <summary>
        /// Визначає публічний елемент `public ScheduleModel Model` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleSlotModel> Slots { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleSlotModel> Slots { get; } = new();

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleCellStyleModel> CellStyles { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleCellStyleModel> CellStyles { get; } = new();

        
        
        
        
        
        
        
        private int _selectedAvailabilityGroupId;
        /// <summary>
        /// Визначає публічний елемент `public int SelectedAvailabilityGroupId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int SelectedAvailabilityGroupId
        {
            get => _selectedAvailabilityGroupId;
            set => SetProperty(ref _selectedAvailabilityGroupId, value);
        }
    }
}
