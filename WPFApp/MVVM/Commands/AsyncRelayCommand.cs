/*
  Опис файлу: цей модуль містить реалізацію компонента AsyncRelayCommand у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WPFApp.MVVM.Commands
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class AsyncRelayCommand : ICommand` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        
        private readonly Func<CancellationToken, Task> _execute;

        
        private readonly Func<bool>? _canExecute;

        
        private readonly Action<Exception>? _onException;

        
        private int _isRunning;

        
        private CancellationTokenSource? _executionCts;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler<Exception>? ExecutionFailed;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler<Exception>? ExecutionFailed;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? CanExecuteChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool IsRunning => Volatile.Read(ref _isRunning) != 0;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsRunning => Volatile.Read(ref _isRunning) != 0;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onException = null)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onException = null)
            : this(
                execute: ct => execute(), 
                canExecute: canExecute,
                onException: onException)
        {
            
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null, Action<Exception>? onExce` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null, Action<Exception>? onException = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onException = onException;
        }

        /// <summary>
        /// Визначає публічний елемент `public bool CanExecute(object? parameter)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            
            if (IsRunning)
                return false;

            
            return _canExecute?.Invoke() ?? true;
        }

        /// <summary>
        /// Визначає публічний елемент `public async void Execute(object? parameter)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public async void Execute(object? parameter)
        {
            
            if (!CanExecute(parameter))
                return;

            
            
            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
                return;

            
            RaiseCanExecuteChanged();

            
            
            _executionCts?.Dispose();
            _executionCts = new CancellationTokenSource();

            try
            {
                
                
                await _execute(_executionCts.Token);
            }
            catch (OperationCanceledException) when (_executionCts.IsCancellationRequested)
            {
                
                
            }
            catch (Exception ex)
            {
                
                
                
                _onException?.Invoke(ex);
                ExecutionFailed?.Invoke(this, ex);
            }
            finally
            {
                
                Interlocked.Exchange(ref _isRunning, 0);

                
                RaiseCanExecuteChanged();

                
            }
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void Cancel()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Cancel()
        {
            
            _executionCts?.Cancel();
        }

        
        
        
        
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
}
