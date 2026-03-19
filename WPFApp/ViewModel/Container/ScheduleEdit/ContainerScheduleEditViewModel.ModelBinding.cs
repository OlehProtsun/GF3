/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.ModelBinding у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFApp.MVVM.Validation.Rules;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private bool SetScheduleValue<T>(
            T value,
            Func<ScheduleModel, T> get,
            Action<ScheduleModel, T> set,
            bool clearErrors = true,
            bool invalidateGenerated = false,
            [CallerMemberName] string? propertyName = null)
        {
            
            if (SelectedBlock?.Model is not { } model)
                return false;

            
            var current = get(model);
            if (EqualityComparer<T>.Default.Equals(current, value))
                return false;

            
            set(model, value);

            
            if (propertyName != null)
                OnPropertyChanged(propertyName);

            
            
            if (clearErrors && propertyName != null)
                ClearValidationErrors(propertyName);

            
            if (invalidateGenerated)
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);

            
            
            if (propertyName != null)
            {
                var msg = ScheduleValidationRules.ValidateProperty(model, propertyName);
                if (!string.IsNullOrWhiteSpace(msg))
                    AddValidationError(propertyName, msg);
            }

            return true;
        }
    }
}
