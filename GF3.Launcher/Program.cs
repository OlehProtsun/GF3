using System.Threading;
using System.Windows.Forms;

namespace GF3.Launcher;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\GF3.Launcher.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        var options = LauncherOptions.Parse(args);

        using var instanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isPrimaryInstance);
        if (!isPrimaryInstance)
        {
            if (!options.SuppressBrowser && !LauncherStateStore.TryOpenRunningInstance())
            {
                MessageBox.Show(
                    "GF3 is already running, but the browser URL could not be resolved.",
                    "GF3 Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        ApplicationConfiguration.Initialize();

        try
        {
            Application.Run(new LauncherApplicationContext(options));
        }
        finally
        {
            try
            {
                instanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }
    }
}

internal sealed record LauncherOptions(bool SuppressBrowser, LauncherRunMode RunMode)
{
    public static LauncherOptions Parse(string[] args)
    {
        var suppressBrowser = args.Any(static argument =>
            string.Equals(argument, "--no-browser", StringComparison.OrdinalIgnoreCase));
        var runMode = LauncherRunMode.Auto;

        if (args.Any(static argument => string.Equals(argument, "--workspace", StringComparison.OrdinalIgnoreCase)))
        {
            runMode = LauncherRunMode.Workspace;
        }
        else if (args.Any(static argument => string.Equals(argument, "--bundle", StringComparison.OrdinalIgnoreCase)))
        {
            runMode = LauncherRunMode.Bundle;
        }

        return new LauncherOptions(suppressBrowser, runMode);
    }
}
