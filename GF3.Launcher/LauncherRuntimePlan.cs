namespace GF3.Launcher;

internal enum LauncherRunMode
{
    Auto,
    Workspace,
    Bundle,
}

internal sealed record ProcessLaunchConfiguration(
    string DisplayName,
    string FilePath,
    string WorkingDirectory,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> EnvironmentVariables,
    string StdoutLogFileName,
    string StderrLogFileName);

internal sealed record LauncherRuntimePlan(
    string ModeLabel,
    string BaseUrl,
    ProcessLaunchConfiguration Backend,
    ProcessLaunchConfiguration? Frontend);
