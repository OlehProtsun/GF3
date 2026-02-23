using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.Service
{
    public sealed class DatabaseChangedEventArgs : EventArgs
    {
        public DateTime ChangedAtUtc { get; init; } = DateTime.UtcNow;
        public string Source { get; init; } = "Unknown";
    }

    public interface IDatabaseChangeNotifier
    {
        event EventHandler<DatabaseChangedEventArgs>? DatabaseChanged;
        void NotifyDatabaseChanged(string source);
    }

    public sealed class DatabaseChangeNotifier : IDatabaseChangeNotifier, IDisposable
    {
        private readonly ILoggerService _logger;
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(250);
        private readonly object _gate = new();
        private CancellationTokenSource? _pendingCts;

        public DatabaseChangeNotifier(ILoggerService logger)
        {
            _logger = logger;
        }

        public event EventHandler<DatabaseChangedEventArgs>? DatabaseChanged;

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

            _logger.Log($"[DB-CHANGE] Notification queued. Source={source}.");
            _ = PublishDebouncedAsync(source, cts);
        }

        private async Task PublishDebouncedAsync(string source, CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(_debounceDelay, cts.Token); // без ConfigureAwait(false)

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
                // ignore
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
