/*
  Опис файлу: цей модуль містить реалізацію компонента UiDebouncedAction у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.MVVM.Threading
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class UiDebouncedAction : IDisposable` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class UiDebouncedAction : IDisposable
    {
        
        
        
        
        private readonly Func<Action, Task> _runOnUiThreadAsync;

        
        
        
        
        private readonly TimeSpan _delay;

        
        
        
        
        
        private CancellationTokenSource? _cts;

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public UiDebouncedAction(Func<Action, Task> runOnUiThreadAsync, TimeSpan delay)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public UiDebouncedAction(Func<Action, Task> runOnUiThreadAsync, TimeSpan delay)
        {
            _runOnUiThreadAsync = runOnUiThreadAsync ?? throw new ArgumentNullException(nameof(runOnUiThreadAsync));
            _delay = delay;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void Schedule(Action action)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Schedule(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            
            
            var prev = Interlocked.Exchange(ref _cts, null);
            CancelAndDispose(prev);

            
            var localCts = new CancellationTokenSource();
            _cts = localCts;

            
            
            _ = RunAsync(localCts, action);
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void Cancel()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Cancel()
        {
            var prev = Interlocked.Exchange(ref _cts, null);
            CancelAndDispose(prev);
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void Dispose()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Dispose()
        {
            Cancel();
        }

        
        
        
        
        
        
        
        private async Task RunAsync(CancellationTokenSource localCts, Action action)
        {
            try
            {
                var token = localCts.Token;

                
                await Task.Delay(_delay, token).ConfigureAwait(false);

                
                if (token.IsCancellationRequested)
                    return;

                
                await _runOnUiThreadAsync(() =>
                {
                    

                    
                    if (token.IsCancellationRequested)
                        return;

                    
                    
                    if (!ReferenceEquals(_cts, localCts))
                        return;

                    
                    action();
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                
            }
            finally
            {
                
                
                if (ReferenceEquals(_cts, localCts))
                    _cts = null;

                
                
                try { localCts.Dispose(); } catch {  }
            }
        }

        
        
        
        
        private static void CancelAndDispose(CancellationTokenSource? cts)
        {
            if (cts is null) return;

            try { cts.Cancel(); } catch {  }
            try { cts.Dispose(); } catch {  }
        }
    }
}
