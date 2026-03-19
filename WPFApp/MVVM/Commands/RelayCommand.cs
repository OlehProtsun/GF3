/*
  Опис файлу: цей модуль містить реалізацію компонента RelayCommand у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Windows;
using System.Windows.Input;

namespace WPFApp.MVVM.Commands
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class RelayCommand : ICommand` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        /// <summary>
        /// Визначає публічний елемент `public RelayCommand(Action execute, Func<bool>? canExecute = null)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));

            
            _canExecute = canExecute;
        }

        /// <summary>
        /// Визначає публічний елемент `public bool CanExecute(object? parameter)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            
            
            return _canExecute?.Invoke() ?? true;
        }

        /// <summary>
        /// Визначає публічний елемент `public void Execute(object? parameter)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Execute(object? parameter)
        {
            
            _execute();
        }

        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? CanExecuteChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Визначає публічний елемент `public void RaiseCanExecuteChanged()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            
            var handler = CanExecuteChanged;

            
            if (handler is null)
                return;

            
            var dispatcher = Application.Current?.Dispatcher;

            
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => handler(this, EventArgs.Empty)));
                return;
            }

            
            handler(this, EventArgs.Empty);
        }
    }

    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class RelayCommand<T> : ICommand` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        /// <summary>
        /// Визначає публічний елемент `public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Визначає публічний елемент `public bool CanExecute(object? parameter)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            
            var value = Cast(parameter);

            
            return _canExecute?.Invoke(value) ?? true;
        }

        /// <summary>
        /// Визначає публічний елемент `public void Execute(object? parameter)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Execute(object? parameter)
        {
            
            var value = Cast(parameter);

            
            _execute(value);
        }

        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? CanExecuteChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Визначає публічний елемент `public void RaiseCanExecuteChanged()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler is null)
                return;

            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => handler(this, EventArgs.Empty)));
                return;
            }

            handler(this, EventArgs.Empty);
        }

        private static T? Cast(object? parameter)
        {
            
            if (parameter is null)
                return default;

            
            if (parameter is T t)
                return t;

            
            throw new InvalidCastException(
                $"RelayCommand<{typeof(T).Name}> received parameter of type '{parameter.GetType().Name}', which cannot be cast.");
        }
    }
}
