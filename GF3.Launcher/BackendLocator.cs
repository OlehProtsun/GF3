namespace GF3.Launcher;

internal sealed record BackendLaunchConfiguration(
    string FilePath,
    string WorkingDirectory,
    IReadOnlyList<string> Arguments);

internal static class BackendLocator
{
    public static BackendLaunchConfiguration Resolve(string startingDirectory)
    {
        foreach (var candidateDirectory in EnumerateCandidateDirectories(startingDirectory))
        {
            if (TryResolveFromDirectory(candidateDirectory, out var configuration))
            {
                return configuration;
            }
        }

        throw new FileNotFoundException(
            "WebApi executable was not found. Publish the backend into a sibling 'backend' folder or run scripts/publish-local-app.ps1.");
    }

    private static IEnumerable<string> EnumerateCandidateDirectories(string startingDirectory)
    {
        var current = new DirectoryInfo(startingDirectory);
        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "backend");
            yield return current.FullName;
            yield return Path.Combine(current.FullName, "artifacts", "manual-release", "app", "backend");
            yield return Path.Combine(current.FullName, "artifacts", "local-app", "app", "backend");
            yield return Path.Combine(current.FullName, ".codex-temp", "publish", "webapi");
            yield return Path.Combine(current.FullName, "GF3.WebApi", "bin", "Release", "net10.0");
            yield return Path.Combine(current.FullName, "GF3.WebApi", "bin", "Release", "net10.0", "win-x64");
            yield return Path.Combine(current.FullName, "GF3.WebApi", "bin", "Debug", "net10.0");
            yield return Path.Combine(current.FullName, "GF3.WebApi", "bin", "Debug", "net10.0", "win-x64");
            current = current.Parent;
        }
    }

    private static bool TryResolveFromDirectory(string directoryPath, out BackendLaunchConfiguration configuration)
    {
        configuration = default!;

        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        var executablePath = Path.Combine(directoryPath, "WebApi.exe");
        if (File.Exists(executablePath))
        {
            configuration = new BackendLaunchConfiguration(
                executablePath,
                directoryPath,
                Array.Empty<string>());

            return true;
        }

        var dllPath = Path.Combine(directoryPath, "WebApi.dll");
        if (File.Exists(dllPath))
        {
            configuration = new BackendLaunchConfiguration(
                "dotnet",
                directoryPath,
                new[] { dllPath });

            return true;
        }

        return false;
    }
}
