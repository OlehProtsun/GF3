/*
  Опис файлу: цей модуль містить реалізацію компонента CustomMessageBoxViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFApp.ViewModel.Dialogs
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public enum CustomMessageBoxIcon` та контракт його використання у шарі WPFApp.
    /// </summary>
    public enum CustomMessageBoxIcon
    {
        Info,
        Warning,
        Error
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class CustomMessageBoxViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class CustomMessageBoxViewModel
    {
        
        
        

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public event Action<bool?>? RequestClose;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event Action<bool?>? RequestClose;

        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public string Title { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Title { get; }

        
        /// <summary>
        /// Визначає публічний елемент `public string Message { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Message { get; }

        
        /// <summary>
        /// Визначає публічний елемент `public string Details { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Details { get; }

        
        /// <summary>
        /// Визначає публічний елемент `public string OkText { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string OkText { get; }

        
        /// <summary>
        /// Визначає публічний елемент `public string CancelText { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string CancelText { get; }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool IsCancelVisible => !string.IsNullOrWhiteSpace(CancelText);` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsCancelVisible => !string.IsNullOrWhiteSpace(CancelText);

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool HasDetails => !string.IsNullOrWhiteSpace(Details);` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

        
        
        

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Geometry IconGeometry { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Geometry IconGeometry { get; }

        
        private const string IconWarnKey = "IconWarn";
        private const string IconErrorKey = "IconError";
        private const string IconInfoKey = "IconInfo";

        
        
        

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ICommand OkCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand OkCommand { get; }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ICommand CancelCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand CancelCommand { get; }

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public CustomMessageBoxViewModel(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public CustomMessageBoxViewModel(
            string title,
            string message,
            CustomMessageBoxIcon icon,
            string okText = "OK",
            string cancelText = "",
            string details = "")
        {
            
            
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Details = details ?? string.Empty;

            
            OkText = string.IsNullOrWhiteSpace(okText) ? "OK" : okText;
            CancelText = cancelText ?? string.Empty;

            
            
            var resourceKey = icon switch
            {
                CustomMessageBoxIcon.Warning => IconWarnKey,
                CustomMessageBoxIcon.Error => IconErrorKey,
                _ => IconInfoKey
            };

            
            
            IconGeometry = TryGetGeometryResource(resourceKey);

            
            
            OkCommand = new RelayCommand(
                execute: () => RequestClose?.Invoke(true));

            CancelCommand = new RelayCommand(
                execute: () => RequestClose?.Invoke(false),
                canExecute: () => IsCancelVisible); 
        }

        
        
        

        private static Geometry TryGetGeometryResource(string resourceKey)
        {
            
            var app = Application.Current;
            if (app is null)
                return Geometry.Empty;

            
            
            if (!app.Resources.Contains(resourceKey))
                return Geometry.Empty;

            
            return app.Resources[resourceKey] as Geometry ?? Geometry.Empty;
        }
    }

    
    
    
    
    
    
    
    
    
    
    
    
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
        /// Визначає публічний елемент `public event EventHandler? CanExecuteChanged` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void RaiseCanExecuteChanged()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            
            
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
