/*
  Опис файлу: цей модуль містить реалізацію компонента BindRow у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using BusinessLogicLayer.Contracts.Models;
using WPFApp.MVVM.Core;

namespace WPFApp.ViewModel.Availability.Helpers
{
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class BindRow : ObservableObject` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class BindRow : ObservableObject
    {
        
        private int _id;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public int Id` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Id
        {
            get => _id;
            set
            {
                
                
                
                
                SetProperty(ref _id, value);
            }
        }

        
        
        private string _key = string.Empty;

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public string Key` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Key
        {
            get => _key;
            set
            {
                
                
                var safe = value ?? string.Empty;

                
                SetProperty(ref _key, safe);
            }
        }

        
        
        private string _value = string.Empty;

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public string Value` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                
                var safe = value ?? string.Empty;

                
                SetProperty(ref _value, safe);
            }
        }

        
        private bool _isActive = true;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool IsActive` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                
                SetProperty(ref _isActive, value);
            }
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool IsBlank` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsBlank
        {
            get
            {
                
                
                return string.IsNullOrWhiteSpace(Key) && string.IsNullOrWhiteSpace(Value);
            }
        }

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public BindModel ToModel()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public BindModel ToModel()
        {
            
            return new BindModel
            {
                
                Id = Id,

                
                Key = Key,
                Value = Value,

                
                IsActive = IsActive
            };
        }

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void UpdateFromModel(BindModel model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void UpdateFromModel(BindModel model)
        {
            
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            
            
            
            Id = model.Id;

            
            Key = model.Key ?? string.Empty;

            
            Value = model.Value ?? string.Empty;

            
            IsActive = model.IsActive;
        }

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static BindRow FromModel(BindModel model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static BindRow FromModel(BindModel model)
        {
            
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            
            var row = new BindRow();

            
            row.UpdateFromModel(model);

            
            return row;
        }
    }
}
