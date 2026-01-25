using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace WPFApp.Service
{
    public interface ILoggerService
    {
        void Log(string message);
        void LogPerf(string area, string message);
    }

    public sealed class LoggerService : ILoggerService
    {
        private static readonly Lazy<LoggerService> LazyInstance = new(() => new LoggerService());
        public static LoggerService Instance => LazyInstance.Value;

        private readonly string _logPath;
        private readonly object _lock = new();

        private LoggerService()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GF3",
                "logs");
            Directory.CreateDirectory(root);
            _logPath = Path.Combine(root, "ui-performance.log");
        }

        public void Log(string message)
        {
            WriteLine("INFO", message);
        }

        public void LogPerf(string area, string message)
        {
            WriteLine("PERF", $"[{area}] {message}");
        }

        private void WriteLine(string level, string message)
        {
            var stamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var mem = GC.GetTotalMemory(false);
            var line = new StringBuilder()
                .Append(stamp)
                .Append(" [")
                .Append(level)
                .Append("] [T")
                .Append(threadId)
                .Append("] [Mem=")
                .Append(mem)
                .Append("] ")
                .Append(message)
                .ToString();

            lock (_lock)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
    }
}
