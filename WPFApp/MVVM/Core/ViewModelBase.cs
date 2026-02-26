/*
  Опис файлу: цей модуль містить реалізацію компонента ViewModelBase у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WPFApp.MVVM.Core
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public abstract class ViewModelBase : INotifyPropertyChanged` та контракт його використання у шарі WPFApp.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        
        /// <summary>
        /// Визначає публічний елемент `public event PropertyChangedEventHandler? PropertyChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        
        
        private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _argsCache =
            new(StringComparer.Ordinal);

        
        
        
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            
            var handler = PropertyChanged;

            
            if (handler is null)
                return;

            
            
            
            var args = GetArgs(propertyName);

            
            var dispatcher = Application.Current?.Dispatcher;

            
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                
                dispatcher.BeginInvoke(new Action(() => handler(this, args)));
                return;
            }

            
            handler(this, args);
        }

        
        
        
        
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            
            
            
            
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            
            field = value;

            
            OnPropertyChanged(propertyName);

            return true;
        }

        
        
        
        
        
        
        
        protected bool SetProperty<T>(
            ref T field,
            T value,
            Action afterChange,
            [CallerMemberName] string? propertyName = null)
        {
            
            if (!SetProperty(ref field, value, propertyName))
                return false;

            
            afterChange?.Invoke();

            return true;
        }

        
        
        
        
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            
            if (propertyNames is null || propertyNames.Length == 0)
                return;

            
            for (int i = 0; i < propertyNames.Length; i++)
                OnPropertyChanged(propertyNames[i]);
        }

        private static PropertyChangedEventArgs GetArgs(string? propertyName)
        {
            
            
            if (string.IsNullOrEmpty(propertyName))
                return new PropertyChangedEventArgs(propertyName);

            
            return _argsCache.GetOrAdd(propertyName, static n => new PropertyChangedEventArgs(n));
        }
    }
}
