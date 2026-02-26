/*
  Опис файлу: цей модуль містить реалізацію компонента ValidationErrors у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;                         
using System.Collections;             
using System.Collections.Generic;     
using System.ComponentModel;          
using System.Linq;                    

namespace WPFApp.MVVM.Validation
{
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ValidationErrors : INotifyDataErrorInfo` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ValidationErrors : INotifyDataErrorInfo
    {
        
        
        
        
        
        private readonly Dictionary<string, List<string>> _errors = new();

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        
        
        /// <summary>
        /// Визначає публічний елемент `public bool HasErrors => _errors.Count > 0;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool HasErrors => _errors.Count > 0;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public IEnumerable GetErrors(string? propertyName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IEnumerable GetErrors(string? propertyName)
        {
            
            
            if (string.IsNullOrWhiteSpace(propertyName))
                return _errors.SelectMany(x => x.Value); 

            
            
            
            
            
            return _errors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void ClearAll()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ClearAll()
        {
            
            
            var keys = _errors.Keys.ToList();

            
            _errors.Clear();

            
            
            foreach (var k in keys)
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(k));
        }

        
        /// <summary>
        /// Визначає публічний елемент `public void Clear(string propertyName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Clear(string propertyName)
        {
            
            
            if (_errors.Remove(propertyName))
                
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        
        /// <summary>
        /// Визначає публічний елемент `public void Add(string propertyName, string message)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Add(string propertyName, string message)
        {
            
            
            if (!_errors.TryGetValue(propertyName, out var list))
                _errors[propertyName] = list = new List<string>();

            
            
            if (!list.Contains(message))
            {
                
                list.Add(message);

                
                
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetMany(IReadOnlyDictionary<string, string> errors)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetMany(IReadOnlyDictionary<string, string> errors)
        {
            
            
            ClearAll();

            
            
            
            foreach (var kv in errors)
                
                
                Add(kv.Key, kv.Value);
        }
    }
}
