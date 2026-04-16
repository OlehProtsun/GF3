using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace GF3.Launcher;

internal sealed class LauncherApplicationContext : ApplicationContext
{
    private const int PreferredBackendPort = 55706;
    private static readonly TimeSpan BackendStartupTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan FrontendStartupTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan HealthProbeDelay = TimeSpan.FromMilliseconds(500);
    private static readonly HttpClient HealthClient = new()
    {
        Timeout = TimeSpan.FromSeconds(2),
    };

    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _openMenuItem;
    private readonly ToolStripMenuItem _remoteAccessMenuItem;
    private readonly LauncherOptions _options;
    private readonly SynchronizationContext _uiContext;
    private readonly CancellationTokenSource _shutdownCts = new();

    private Process? _frontendProcess;
    private Process? _backendProcess;
    private ProcessJobTracker? _jobTracker;
    private Task? _backendStdoutPumpTask;
    private Task? _backendStderrPumpTask;
    private Task? _frontendStdoutPumpTask;
    private Task? _frontendStderrPumpTask;
    private string? _baseUrl;
    private string? _logDirectory;
    private string? _backendStdoutLogPath;
    private string? _backendStderrLogPath;
    private string? _frontendStdoutLogPath;
    private string? _frontendStderrLogPath;
    private bool _isExiting;
    private bool _cleanupStarted;

    public LauncherApplicationContext(LauncherOptions options)
    {
        _options = options;
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        var contextMenu = new ContextMenuStrip();
        _openMenuItem = new ToolStripMenuItem("Open GF3", null, (_, _) => OpenApplicationInBrowser())
        {
            Enabled = false,
        };
        _remoteAccessMenuItem = new ToolStripMenuItem("Remote Access")
        {
            Enabled = false,
        };

        var exitMenuItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread());
        contextMenu.Items.Add(_openMenuItem);
        contextMenu.Items.Add(_remoteAccessMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "GF3 - Starting",
            Visible = true,
            ContextMenuStrip = contextMenu,
        };

        _notifyIcon.DoubleClick += (_, _) => OpenApplicationInBrowser();

        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            var port = ReserveFreeTcpPort();
            var databasePath = LauncherPaths.ResolveDatabasePath();
            var runtimePlan = LauncherRuntimeResolver.Resolve(_options, AppContext.BaseDirectory, port, databasePath);
            _baseUrl = runtimePlan.BaseUrl;

            LauncherStateStore.Save(_baseUrl);
            PrepareLogFiles(runtimePlan);
            WriteConnectionInfo(runtimePlan);
            _jobTracker = new ProcessJobTracker();

            if (runtimePlan.Frontend is not null)
            {
                _frontendProcess = StartManagedProcess(runtimePlan.Frontend, isBackendProcess: false);
                _jobTracker.AddProcess(_frontendProcess);
            }

            _backendProcess = StartManagedProcess(runtimePlan.Backend, isBackendProcess: true);
            _jobTracker.AddProcess(_backendProcess);

            await WaitForBackendReadyAsync(_baseUrl, _backendProcess, _shutdownCts.Token).ConfigureAwait(false);

            if (_frontendProcess is not null)
            {
                await WaitForUrlReadyAsync(
                    "http://localhost:5173",
                    _frontendProcess,
                    "frontend",
                    FrontendStartupTimeout,
                    _shutdownCts.Token).ConfigureAwait(false);
            }

            _uiContext.Post(_ =>
            {
                _openMenuItem.Enabled = true;
                ConfigureRemoteAccessMenu(runtimePlan.RemoteBaseUrls);
                _notifyIcon.Text = $"GF3 - Running ({runtimePlan.ModeLabel})";
            }, null);

            if (!_options.SuppressBrowser)
            {
                OpenApplicationInBrowser();
            }

            _uiContext.Post(_ => ShowRemoteAccessHint(runtimePlan), null);
        }
        catch (Exception exception)
        {
            _uiContext.Post(_ => HandleStartupFailure(exception), null);
        }
    }

    private static int ReserveFreeTcpPort()
    {
        if (TryReservePort(PreferredBackendPort, out var preferredPort))
        {
            return preferredPort;
        }

        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static bool TryReservePort(int port, out int reservedPort)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            try
            {
                reservedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                return true;
            }
            finally
            {
                listener.Stop();
            }
        }
        catch (SocketException)
        {
            reservedPort = 0;
            return false;
        }
    }

    private void PrepareLogFiles()
    {
        var logDirectory = Path.Combine(
            LauncherPaths.GetLauncherLogsRoot(),
            DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        Directory.CreateDirectory(logDirectory);
        _logDirectory = logDirectory;
    }

    private void PrepareLogFiles(LauncherRuntimePlan runtimePlan)
    {
        PrepareLogFiles();
        if (string.IsNullOrWhiteSpace(_logDirectory))
        {
            return;
        }

        _backendStdoutLogPath = Path.Combine(_logDirectory, runtimePlan.Backend.StdoutLogFileName);
        _backendStderrLogPath = Path.Combine(_logDirectory, runtimePlan.Backend.StderrLogFileName);

        if (runtimePlan.Frontend is not null)
        {
            _frontendStdoutLogPath = Path.Combine(_logDirectory, runtimePlan.Frontend.StdoutLogFileName);
            _frontendStderrLogPath = Path.Combine(_logDirectory, runtimePlan.Frontend.StderrLogFileName);
        }
    }

    private void WriteConnectionInfo(LauncherRuntimePlan runtimePlan)
    {
        if (string.IsNullOrWhiteSpace(_logDirectory))
        {
            return;
        }

        var lines = new List<string>
        {
            $"Local URL: {runtimePlan.BaseUrl}",
            string.Empty,
            "Remote URLs:",
        };

        if (runtimePlan.RemoteBaseUrls.Count == 0)
        {
            lines.Add("  (none detected)");
        }
        else
        {
            foreach (var remoteUrl in runtimePlan.RemoteBaseUrls)
            {
                lines.Add($"  {remoteUrl}");
            }
        }

        File.WriteAllLines(Path.Combine(_logDirectory, "connection-info.txt"), lines);
    }

    private Process StartManagedProcess(ProcessLaunchConfiguration launchConfiguration, bool isBackendProcess)
    {
        var processStartInfo = CreateProcessStartInfo(launchConfiguration);

        var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true,
        };

        process.Exited += (_, _) => OnManagedProcessExited(
            launchConfiguration.DisplayName,
            isBackendProcess ? _backendStderrLogPath : _frontendStderrLogPath,
            isBackendProcess ? _backendProcess?.ExitCode : _frontendProcess?.ExitCode);

        if (!process.Start())
        {
            throw new InvalidOperationException($"The GF3 {launchConfiguration.DisplayName} process did not start.");
        }

        BeginProcessLogging(process, isBackendProcess);
        return process;
    }

    private static ProcessStartInfo CreateProcessStartInfo(ProcessLaunchConfiguration launchConfiguration)
    {
        var commandBuilder = new System.Text.StringBuilder("/c ");
        foreach (var pair in launchConfiguration.EnvironmentVariables)
        {
            commandBuilder.Append("set \"")
                .Append(pair.Key)
                .Append('=')
                .Append(EscapeForCmdValue(pair.Value))
                .Append("\" && ");
        }

        AppendCommandToken(commandBuilder, launchConfiguration.FilePath);

        foreach (var argument in launchConfiguration.Arguments)
        {
            commandBuilder.Append(' ');
            AppendCommandToken(commandBuilder, argument);
        }

        return new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = commandBuilder.ToString(),
            WorkingDirectory = launchConfiguration.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
    }

    private static string EscapeForCmdValue(string value)
        => value.Replace("\"", "\"\"", StringComparison.Ordinal);

    private static void AppendCommandToken(System.Text.StringBuilder builder, string token)
    {
        if (token.Contains(' ') || token.Contains('\t'))
        {
            builder.Append('"')
                .Append(token)
                .Append('"');
            return;
        }

        builder.Append(token);
    }

    private void BeginProcessLogging(Process process, bool isBackendProcess)
    {
        var stdoutLogPath = isBackendProcess ? _backendStdoutLogPath : _frontendStdoutLogPath;
        var stderrLogPath = isBackendProcess ? _backendStderrLogPath : _frontendStderrLogPath;

        if (string.IsNullOrWhiteSpace(stdoutLogPath) || string.IsNullOrWhiteSpace(stderrLogPath))
        {
            return;
        }

        var stdoutTask = PumpProcessStreamAsync(process.StandardOutput, stdoutLogPath);
        var stderrTask = PumpProcessStreamAsync(process.StandardError, stderrLogPath);

        if (isBackendProcess)
        {
            _backendStdoutPumpTask = stdoutTask;
            _backendStderrPumpTask = stderrTask;
            return;
        }

        _frontendStdoutPumpTask = stdoutTask;
        _frontendStderrPumpTask = stderrTask;
    }

    private static async Task PumpProcessStreamAsync(StreamReader reader, string outputPath)
    {
        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(fileStream);

        while (true)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            await writer.WriteLineAsync(line).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
    }

    private static async Task WaitForBackendReadyAsync(
        string baseUrl,
        Process backendProcess,
        CancellationToken cancellationToken)
    {
        using var startupTimeoutCts = new CancellationTokenSource(BackendStartupTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, startupTimeoutCts.Token);

        while (true)
        {
            linkedCts.Token.ThrowIfCancellationRequested();

            if (backendProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"The GF3 backend exited before startup completed. Exit code: {backendProcess.ExitCode}.");
            }

            try
            {
                var health = await HealthClient
                    .GetFromJsonAsync<HealthResponse>($"{baseUrl}/api/health", linkedCts.Token)
                    .ConfigureAwait(false);

                if (health is not null
                    && string.Equals(health.Status, "ok", StringComparison.OrdinalIgnoreCase)
                    && health.CanConnect)
                {
                    return;
                }
            }
            catch (OperationCanceledException) when (startupTimeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
            }

            try
            {
                await Task.Delay(HealthProbeDelay, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (startupTimeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException("Timed out waiting for the GF3 backend to become ready.");
    }

    private static async Task WaitForUrlReadyAsync(
        string url,
        Process process,
        string serviceName,
        TimeSpan startupTimeout,
        CancellationToken cancellationToken)
    {
        using var startupTimeoutCts = new CancellationTokenSource(startupTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, startupTimeoutCts.Token);

        while (true)
        {
            linkedCts.Token.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The GF3 {serviceName} process exited before startup completed. Exit code: {process.ExitCode}.");
            }

            try
            {
                using var response = await HealthClient.GetAsync(url, linkedCts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (OperationCanceledException) when (startupTimeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
            }

            try
            {
                await Task.Delay(HealthProbeDelay, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (startupTimeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException($"Timed out waiting for the GF3 {serviceName} to become ready.");
    }

    private void OpenApplicationInBrowser()
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_baseUrl)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"The browser could not be opened automatically.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "GF3 Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ConfigureRemoteAccessMenu(IReadOnlyList<string> remoteBaseUrls)
    {
        _remoteAccessMenuItem.DropDownItems.Clear();

        if (remoteBaseUrls.Count == 0)
        {
            _remoteAccessMenuItem.DropDownItems.Add(new ToolStripMenuItem("No LAN address detected")
            {
                Enabled = false,
            });
            _remoteAccessMenuItem.Enabled = false;
            return;
        }

        foreach (var remoteUrl in remoteBaseUrls)
        {
            var url = remoteUrl;
            _remoteAccessMenuItem.DropDownItems.Add(
                new ToolStripMenuItem(url, null, (_, _) => CopyRemoteUrl(url)));
        }

        _remoteAccessMenuItem.Enabled = true;
    }

    private void CopyRemoteUrl(string remoteUrl)
    {
        try
        {
            Clipboard.SetText(remoteUrl);
            _notifyIcon.ShowBalloonTip(
                3000,
                "GF3 Remote Access",
                $"Remote URL copied:{Environment.NewLine}{remoteUrl}",
                ToolTipIcon.Info);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"The remote URL could not be copied.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "GF3 Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ShowRemoteAccessHint(LauncherRuntimePlan runtimePlan)
    {
        var preferredRemoteUrl = runtimePlan.RemoteBaseUrls.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(preferredRemoteUrl))
        {
            return;
        }

        _notifyIcon.ShowBalloonTip(
            5000,
            "GF3 Remote Access",
            $"Open from another machine:{Environment.NewLine}{preferredRemoteUrl}",
            ToolTipIcon.Info);
    }

    private void OnManagedProcessExited(string processName, string? stderrLogPath, int? exitCode)
    {
        _uiContext.Post(_ =>
        {
            if (_isExiting)
            {
                return;
            }

            var details = !string.IsNullOrWhiteSpace(stderrLogPath)
                ? $"{Environment.NewLine}{Environment.NewLine}Error log: {stderrLogPath}"
                : string.Empty;

            MessageBox.Show(
                $"The GF3 {processName} stopped unexpectedly (exit code {exitCode}).{details}",
                "GF3 Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            ExitThread();
        }, null);
    }

    private void HandleStartupFailure(Exception exception)
    {
        var logHint = !string.IsNullOrWhiteSpace(_logDirectory)
            ? $"{Environment.NewLine}{Environment.NewLine}Log directory: {_logDirectory}"
            : string.Empty;

        MessageBox.Show(
            $"GF3 could not be started.{Environment.NewLine}{Environment.NewLine}{exception.Message}{logHint}",
            "GF3 Launcher",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        if (_cleanupStarted)
        {
            base.ExitThreadCore();
            return;
        }

        _cleanupStarted = true;
        _isExiting = true;
        LauncherStateStore.Clear();
        _shutdownCts.Cancel();

        try
        {
            if (_frontendProcess is { HasExited: false })
            {
                _frontendProcess.Kill(entireProcessTree: true);
                _frontendProcess.WaitForExit(3000);
            }
        }
        catch
        {
        }

        try
        {
            if (_backendProcess is { HasExited: false })
            {
                _backendProcess.Kill(entireProcessTree: true);
                _backendProcess.WaitForExit(3000);
            }
        }
        catch
        {
        }

        try
        {
            Task.WaitAll(
                new[]
                {
                    _backendStdoutPumpTask,
                    _backendStderrPumpTask,
                    _frontendStdoutPumpTask,
                    _frontendStderrPumpTask,
                }.Where(static task => task is not null).Cast<Task>().ToArray(),
                millisecondsTimeout: 2000);
        }
        catch
        {
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        _jobTracker?.Dispose();
        _frontendProcess?.Dispose();
        _backendProcess?.Dispose();
        _shutdownCts.Dispose();

        base.ExitThreadCore();
    }

    private sealed class HealthResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("canConnect")]
        public bool CanConnect { get; init; }
    }
}
