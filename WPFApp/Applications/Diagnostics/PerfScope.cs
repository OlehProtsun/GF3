using System;
using System.Diagnostics;

namespace WPFApp.Applications.Diagnostics
{
    public sealed class PerfScope : IDisposable
    {
        private readonly string _area;
        private readonly string _operation;
        private readonly ILoggerService? _logger;
        private readonly Stopwatch _stopwatch;

        public PerfScope(string area, string operation, ILoggerService? logger = null)
        {
            _area = area;
            _operation = operation;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();

            var startMessage = $"START {_operation}";
            _logger?.LogPerf(_area, startMessage);
            Debug.WriteLine($"[PERF][{_area}] {startMessage}");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var msg = $"END {_operation} in {_stopwatch.ElapsedMilliseconds} ms";
            _logger?.LogPerf(_area, msg);
            Debug.WriteLine($"[PERF][{_area}] {msg}");
        }
    }
}
