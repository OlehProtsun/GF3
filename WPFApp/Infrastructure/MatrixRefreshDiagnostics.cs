using System;
using System.Diagnostics;
using System.Threading;

namespace WPFApp.Infrastructure
{
    public static class MatrixRefreshDiagnostics
    {
        public static bool Enabled { get; set; }

        private static int _scheduleMatrixBuilds;
        private static int _availabilityPreviewBuilds;
        private static int _matrixChangedEvents;

        public static void RecordScheduleMatrixBuild(TimeSpan duration, int rows, int columns)
        {
            if (!Enabled) return;
            var count = Interlocked.Increment(ref _scheduleMatrixBuilds);
            Trace.WriteLine($"[MatrixRefresh] Schedule matrix build #{count} ({rows}x{columns}) in {duration.TotalMilliseconds:0.0} ms.");
        }

        public static void RecordAvailabilityPreviewBuild(TimeSpan duration, int rows, int columns)
        {
            if (!Enabled) return;
            var count = Interlocked.Increment(ref _availabilityPreviewBuilds);
            Trace.WriteLine($"[MatrixRefresh] Availability preview build #{count} ({rows}x{columns}) in {duration.TotalMilliseconds:0.0} ms.");
        }

        public static void RecordMatrixChanged(string source)
        {
            if (!Enabled) return;
            var count = Interlocked.Increment(ref _matrixChangedEvents);
            Trace.WriteLine($"[MatrixRefresh] MatrixChanged #{count} from {source}.");
        }

        public static void RecordAvailabilityPreviewRequest(string previewKey, bool skipped)
        {
            if (!Enabled) return;
            Trace.WriteLine(skipped
                ? $"[MatrixRefresh] Availability preview request skipped (key: {previewKey})."
                : $"[MatrixRefresh] Availability preview request queued (key: {previewKey}).");
        }
    }
}
