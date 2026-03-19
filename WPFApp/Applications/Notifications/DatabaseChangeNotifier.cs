/*
  Опис файлу: цей модуль містить реалізацію компонента DatabaseChangeNotifier у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.Applications.Notifications
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class DatabaseChangedEventArgs : EventArgs` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class DatabaseChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Визначає публічний елемент `public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;
        /// <summary>
        /// Визначає публічний елемент `public string Source { get; init; } = "Unknown";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Source { get; init; } = "Unknown";
    }

    /// <summary>
    /// Визначає публічний елемент `public interface IDatabaseChangeNotifier` та контракт його використання у шарі WPFApp.
    /// </summary>
    public interface IDatabaseChangeNotifier
    {
        event EventHandler<DatabaseChangedEventArgs>? DatabaseChanged;
        void NotifyDatabaseChanged(string source);
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class DatabaseChangeNotifier : IDatabaseChangeNotifier, IDisposable` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class DatabaseChangeNotifier : IDatabaseChangeNotifier, IDisposable
    {
                private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(250);
        private readonly object _gate = new();
        private CancellationTokenSource? _pendingCts;

        /// <summary>
        /// Визначає публічний елемент `public DatabaseChangeNotifier()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DatabaseChangeNotifier()
        {
        }

        /// <summary>
        /// Визначає публічний елемент `public event EventHandler<DatabaseChangedEventArgs>? DatabaseChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler<DatabaseChangedEventArgs>? DatabaseChanged;

        /// <summary>
        /// Визначає публічний елемент `public void NotifyDatabaseChanged(string source)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void NotifyDatabaseChanged(string source)
        {
            CancellationTokenSource cts;

            lock (_gate)
            {
                _pendingCts?.Cancel();
                _pendingCts?.Dispose();
                _pendingCts = new CancellationTokenSource();
                cts = _pendingCts;
            }

            _ = PublishDebouncedAsync(source, cts);
        }

        private async Task PublishDebouncedAsync(string source, CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(_debounceDelay, cts.Token); 

                if (cts.Token.IsCancellationRequested)
                    return;

                void Raise()
                {
                    DatabaseChanged?.Invoke(this, new DatabaseChangedEventArgs
                    {
                        Source = source,
                        ChangedAtUtc = DateTime.UtcNow
                    });
                }

                var disp = System.Windows.Application.Current?.Dispatcher;
                if (disp != null && !disp.CheckAccess())
                    disp.Invoke(Raise);
                else
                    Raise();
            }
            catch (OperationCanceledException)
            {
                
            }
            finally
            {
                lock (_gate)
                {
                    if (ReferenceEquals(_pendingCts, cts))
                        _pendingCts = null;
                }
                cts.Dispose();
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public void Dispose()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Dispose()
        {
            lock (_gate)
            {
                _pendingCts?.Cancel();
                _pendingCts?.Dispose();
                _pendingCts = null;
            }
        }
    }
}
