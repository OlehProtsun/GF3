using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WPFApp.Infrastructure
{
    public static class MatrixRefreshDiagnostics
    {
        // --- Public API (compatible) ---
        public static bool Enabled { get; private set; }
        public static string? LogFilePath { get; private set; }

        // Existing counters (keep)
        private static int _scheduleMatrixBuilds;
        private static int _availabilityPreviewBuilds;
        private static int _matrixChangedEvents;

        // --- Writer infra ---
        private static readonly object _gate = new();
        private static BlockingCollection<string>? _queue;
        private static Task? _writerTask;
        private static StreamWriter? _writer;

        // --- Session & ring buffer ---
        private static readonly string _sessionId = Guid.NewGuid().ToString("N")[..8];
        private static readonly ConcurrentQueue<string> _recent = new();
        private const int RecentLimit = 300;

        // --- UI stall monitor ---
        private static Dispatcher? _uiDispatcher;
        private static DispatcherTimer? _uiTimer;
        private static Stopwatch? _uiWatch;
        private static TimeSpan _uiTick = TimeSpan.FromMilliseconds(50);
        private static TimeSpan _uiStallThreshold = TimeSpan.FromMilliseconds(200);

        private static readonly ConcurrentDictionary<string, long> _counters = new();

        // --- Enable/Disable ---
        public static void EnableToFile(
            string? logFilePath = null,
            Dispatcher? uiDispatcher = null,
            bool enableUiStallMonitor = true,
            TimeSpan? uiTick = null,
            TimeSpan? uiStallThreshold = null)
        {
            lock (_gate)
            {
                if (Enabled) return;

                LogFilePath = logFilePath ?? BuildDefaultLogPath();
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);

                _queue = new BlockingCollection<string>(boundedCapacity: 10_000);
                _writer = new StreamWriter(new FileStream(LogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };

                _writerTask = Task.Factory.StartNew(WriterLoop, TaskCreationOptions.LongRunning);

                Enabled = true;

                WriteHeader();

                _uiDispatcher = uiDispatcher;
                if (enableUiStallMonitor && _uiDispatcher != null)
                {
                    _uiTick = uiTick ?? _uiTick;
                    _uiStallThreshold = uiStallThreshold ?? _uiStallThreshold;
                    StartUiStallMonitor();
                }

                AppDomain.CurrentDomain.ProcessExit += (_, __) => Disable();
                AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                {
                    try { RecordException("UnhandledException", e.ExceptionObject as Exception); }
                    catch { /* ignore */ }
                };
                TaskScheduler.UnobservedTaskException += (_, e) =>
                {
                    try
                    {
                        RecordException("UnobservedTaskException", e.Exception);
                        e.SetObserved();
                    }
                    catch { /* ignore */ }
                };

                Step("Diagnostics ENABLED");
                Snapshot("Initial snapshot");
            }
        }

        public static void Disable()
        {
            lock (_gate)
            {
                if (!Enabled) return;

                try { Step("Diagnostics DISABLING"); } catch { }

                try { StopUiStallMonitor(); } catch { }

                Enabled = false;

                try { _queue?.CompleteAdding(); } catch { }

                try { _writerTask?.Wait(1500); } catch { /* ignore */ }

                try { _writer?.Flush(); } catch { }
                try { _writer?.Dispose(); } catch { }

                _writer = null;
                _writerTask = null;
                _queue = null;
            }
        }

        // --- Existing methods (expanded) ---
        public static void RecordScheduleMatrixBuild(TimeSpan duration, int rows, int columns)
        {
            if (!Enabled) return;
            var count = Interlocked.Increment(ref _scheduleMatrixBuilds);
            Log("BUILD", $"ScheduleMatrix #{count} {rows}x{columns} duration={duration.TotalMilliseconds:0.0}ms");
            SnapshotIfSlow(duration, $"Slow ScheduleMatrix build #{count}");
        }

        public static void RecordAvailabilityPreviewBuild(TimeSpan duration, int rows, int columns)
        {
            if (!Enabled) return;
            var count = Interlocked.Increment(ref _availabilityPreviewBuilds);
            Log("BUILD", $"AvailabilityPreview #{count} {rows}x{columns} duration={duration.TotalMilliseconds:0.0}ms");
            SnapshotIfSlow(duration, $"Slow AvailabilityPreview build #{count}");
        }

        public static void RecordMatrixChanged(string source)
        {
            if (!Enabled) return;
            var count = Interlocked.Increment(ref _matrixChangedEvents);
            Log("EVENT", $"MatrixChanged #{count} source={source}");
        }

        public static void RecordAvailabilityPreviewRequest(string previewKey, bool skipped)
        {
            if (!Enabled) return;
            Log("REQ", skipped
                ? $"AvailabilityPreview SKIP key='{previewKey}'"
                : $"AvailabilityPreview QUEUE key='{previewKey}'");
        }

        // --- New helpers you can call from anywhere ---
        public static void Step(string message)
        {
            if (!Enabled) return;
            Log("STEP", message);
        }

        public static void Snapshot(string reason)
        {
            if (!Enabled) return;
            Log("SNAP", reason + " | " + BuildRuntimeSnapshot());
        }

        public static void RecordException(string where, Exception? ex)
        {
            if (!Enabled) return;
            if (ex == null)
            {
                Log("EX", $"{where}: <null exception>");
                return;
            }

            Log("EX", $"{where}: {ex.GetType().Name}: {ex.Message}");
            Log("EX", ex.StackTrace ?? "<no stack>");
            Snapshot($"After exception: {where}");
        }

        public static void RecordUiRefresh(string what, string? extra = null)
        {
            if (!Enabled) return;
            Log("UI", extra == null ? what : $"{what} | {extra}");
        }

        // --- Internal logging ---
        private static void Log(string tag, string message)
        {
            var line = FormatLine(tag, message);

            // ring buffer
            _recent.Enqueue(line);
            while (_recent.Count > RecentLimit && _recent.TryDequeue(out _)) { }

            // file queue
            try
            {
                _queue?.Add(line);
            }
            catch { /* ignore if shutting down */ }
        }

        private static string FormatLine(string tag, string message)
        {
            var now = DateTime.Now;
            var tid = Environment.CurrentManagedThreadId;
            var isTp = Thread.CurrentThread.IsThreadPoolThread;
            var taskId = Task.CurrentId?.ToString() ?? "-";
            var ui = _uiDispatcher != null ? (_uiDispatcher.CheckAccess() ? "UI" : "BG") : "?";

            return $"{now:HH:mm:ss.fff} [{_sessionId}] [{tag}] [t={tid} tp={isTp} task={taskId} {ui}] {message}";
        }

        private static void WriterLoop()
        {
            try
            {
                foreach (var line in _queue!.GetConsumingEnumerable())
                {
                    _writer!.WriteLine(line);
                }
            }
            catch
            {
                // do not throw from background writer
            }
        }

        private static void WriteHeader()
        {
            Log("HDR", $"LogFile='{LogFilePath}'");
            Log("HDR", $"Process='{Process.GetCurrentProcess().ProcessName}' pid={Process.GetCurrentProcess().Id}");
            Log("HDR", $".NET='{Environment.Version}' OS='{Environment.OSVersion}' 64bitProc={Environment.Is64BitProcess}");
        }

        private static string BuildDefaultLogPath()
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WPFApp",
                "Logs");

            var file = $"matrix_refresh_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            return Path.Combine(baseDir, file);
        }

        private static string BuildRuntimeSnapshot()
        {
            try
            {
                var p = Process.GetCurrentProcess();
                var info = GC.GetGCMemoryInfo();
                ThreadPool.GetAvailableThreads(out var wAvail, out var ioAvail);
                ThreadPool.GetMaxThreads(out var wMax, out var ioMax);

                var sb = new StringBuilder();
                sb.Append($"WS={p.WorkingSet64 / (1024 * 1024)}MB ");
                sb.Append($"PM={p.PrivateMemorySize64 / (1024 * 1024)}MB ");
                sb.Append($"Handles={p.HandleCount} Threads={p.Threads.Count} ");
                sb.Append($"GC0={GC.CollectionCount(0)} GC1={GC.CollectionCount(1)} GC2={GC.CollectionCount(2)} ");
                sb.Append($"Managed={GC.GetTotalMemory(false) / (1024 * 1024)}MB ");
                sb.Append($"TP={wAvail}/{wMax} IO={ioAvail}/{ioMax}");
                sb.Append($" Heap={info.HeapSizeBytes / (1024 * 1024)}MB");
                sb.Append($" Frag={info.FragmentedBytes / (1024 * 1024)}MB");
                sb.Append($" Committed={info.TotalCommittedBytes / (1024 * 1024)}MB");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"<snapshot failed: {ex.GetType().Name}: {ex.Message}>";
            }
        }

        private static void SnapshotIfSlow(TimeSpan duration, string reason)
        {
            // підкрути поріг як треба
            if (duration.TotalMilliseconds >= 120)
                Snapshot(reason);
        }

        // --- UI stall monitoring ---
        private static void StartUiStallMonitor()
        {
            if (_uiDispatcher == null) return;

            _uiWatch = Stopwatch.StartNew();
            var last = _uiWatch.Elapsed;

            _uiTimer = new DispatcherTimer(_uiTick, DispatcherPriority.Background, (_, __) =>
            {
                var now = _uiWatch.Elapsed;
                var delta = now - last;
                last = now;

                if (delta > _uiStallThreshold)
                {
                    Log("STALL", $"UI STALL delta={delta.TotalMilliseconds:0}ms threshold={_uiStallThreshold.TotalMilliseconds:0}ms | {BuildRuntimeSnapshot()}");
                    DumpRecent("Recent events around UI stall");
                }

            }, _uiDispatcher);

            _uiTimer.Start();
            Log("UI", $"UI StallMonitor START tick={_uiTick.TotalMilliseconds:0}ms threshold={_uiStallThreshold.TotalMilliseconds:0}ms");
        }

        private static void StopUiStallMonitor()
        {
            if (_uiTimer == null) return;
            try { _uiTimer.Stop(); } catch { }
            _uiTimer = null;
            _uiWatch = null;
            Log("UI", "UI StallMonitor STOP");
        }

        private static void DumpRecent(string title)
        {
            Log("DUMP", title);
            try
            {
                foreach (var line in _recent)
                    _queue?.Add(line);
                _queue?.Add(FormatLine("DUMP", "---- END ----"));
            }
            catch { /* ignore */ }
        }

        public static void RecordParamEvent(string title, string? details = null)
        {
            if (!Enabled) return;
            Log("PARAM", details == null ? title : $"{title} | {details}");
        }

        public static void RecordEmployeesSync(string details)
        {
            if (!Enabled) return;
            Log("EMP_SYNC", details);
        }

        /// <summary>Increments a named counter and optionally logs it.</summary>
        public static long Count(string name, long delta = 1, bool log = false, string? extra = null)
        {
            if (!Enabled) return 0;
            var v = _counters.AddOrUpdate(name, delta, (_, old) => old + delta);
            if (log)
                Log("CNT", extra == null ? $"{name}={v}" : $"{name}={v} | {extra}");
            return v;
        }

        /// <summary>Take lightweight allocation snapshot.</summary>
        public static (long allocBytes, long managedBytes) AllocSnapshot()
        {
            try
            {
                long alloc = 0;
                try
                {
#if NET5_0_OR_GREATER
                    alloc = GC.GetTotalAllocatedBytes(precise: false);
#endif
                }
                catch { /* ignore */ }

                long managed = 0;
                try { managed = GC.GetTotalMemory(false); } catch { /* ignore */ }

                return (alloc, managed);
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>Log allocation delta between snapshots.</summary>
        public static void RecordAllocDelta(string tag, (long allocBytes, long managedBytes) before, string? extra = null)
        {
            if (!Enabled) return;

            var after = AllocSnapshot();
            long dAlloc = after.allocBytes - before.allocBytes;
            long dManaged = after.managedBytes - before.managedBytes;

            string fmt(long b) => $"{b / (1024.0 * 1024.0):0.00}MB";

            Log("ALLOC", extra == null
                ? $"{tag} | dAlloc={fmt(dAlloc)} dManaged={fmt(dManaged)}"
                : $"{tag} | dAlloc={fmt(dAlloc)} dManaged={fmt(dManaged)} | {extra}");
        }

        /// <summary>Optional stack for "who called setter/pipeline". Turn on only when needed.</summary>
        public static bool IncludeStacks { get; set; } = false;

        public static string? ShortStack(int skipFrames = 2, int maxLines = 6)
        {
            if (!Enabled || !IncludeStacks) return null;
            try
            {
                var st = new StackTrace(skipFrames, fNeedFileInfo: false);
                var frames = st.GetFrames();
                if (frames == null || frames.Length == 0) return "<no-frames>";

                var sb = new StringBuilder();
                int take = Math.Min(maxLines, frames.Length);
                for (int i = 0; i < take; i++)
                {
                    var m = frames[i].GetMethod();
                    if (m == null) continue;
                    sb.Append(m.DeclaringType?.Name).Append(".").Append(m.Name);
                    if (i != take - 1) sb.Append(" <- ");
                }
                return sb.ToString();
            }
            catch
            {
                return "<stack-failed>";
            }
        }

        public static int IdOf(object? o) => o == null ? 0 : RuntimeHelpers.GetHashCode(o);

    }
}
