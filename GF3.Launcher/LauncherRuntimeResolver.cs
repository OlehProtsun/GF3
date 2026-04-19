namespace GF3.Launcher;

internal static class LauncherRuntimeResolver
{
    private const int FrontendDevPort = 5173;

    public static LauncherRuntimePlan Resolve(
        LauncherOptions options,
        string startingDirectory,
        int backendPort,
        string databasePath)
    {
        var connectionString = $"Data Source={databasePath}";
        var networkBinding = LauncherNetworkResolver.CreateBinding(backendPort);

        return options.RunMode switch
        {
            LauncherRunMode.Workspace => CreateWorkspacePlan(
                ResolveWorkspaceRoot(startingDirectory),
                networkBinding,
                databasePath,
                connectionString),
            LauncherRunMode.Bundle => CreateBundlePlan(
                startingDirectory,
                networkBinding,
                databasePath,
                connectionString),
            _ => TryFindWorkspaceRoot(startingDirectory, out var workspaceRoot)
                ? CreateWorkspacePlan(workspaceRoot, networkBinding, databasePath, connectionString)
                : CreateBundlePlan(startingDirectory, networkBinding, databasePath, connectionString),
        };
    }

    private static LauncherRuntimePlan CreateWorkspacePlan(
        string workspaceRoot,
        LauncherNetworkBinding networkBinding,
        string databasePath,
        string connectionString)
    {
        var backendProjectPath = Path.Combine(workspaceRoot, "GF3.WebApi", "WebApi.csproj");
        var frontendRoot = Path.Combine(workspaceRoot, "FrontEnd");
        var templatesDirectory = Path.Combine(workspaceRoot, "GF3.WebApi", "Resources", "ExcelTemplate");
        var backendEnvironment = BuildBackendEnvironmentVariables(
            networkBinding.ListenUrl,
            databasePath,
            connectionString,
            environmentName: "Development",
            templatesDirectory);

        var backend = new ProcessLaunchConfiguration(
            DisplayName: "backend",
            FilePath: "dotnet",
            WorkingDirectory: workspaceRoot,
            Arguments: new[] { "run", "--project", backendProjectPath, "--no-launch-profile" },
            EnvironmentVariables: backendEnvironment,
            StdoutLogFileName: "backend.stdout.log",
            StderrLogFileName: "backend.stderr.log");

        var frontend = new ProcessLaunchConfiguration(
            DisplayName: "frontend",
            FilePath: "npm",
            WorkingDirectory: frontendRoot,
            Arguments: new[] { "run", "dev", "--", "--host", "localhost", "--port", FrontendDevPort.ToString(), "--strictPort" },
            EnvironmentVariables: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            StdoutLogFileName: "frontend.stdout.log",
            StderrLogFileName: "frontend.stderr.log");

        return new LauncherRuntimePlan(
            ModeLabel: "workspace",
            BaseUrl: networkBinding.LocalBaseUrl,
            RemoteBaseUrls: networkBinding.RemoteBaseUrls,
            Backend: backend,
            Frontend: frontend);
    }

    private static LauncherRuntimePlan CreateBundlePlan(
        string startingDirectory,
        LauncherNetworkBinding networkBinding,
        string databasePath,
        string connectionString)
    {
        var launchConfiguration = BackendLocator.Resolve(startingDirectory);
        var templatesDirectory = Path.Combine(launchConfiguration.WorkingDirectory, "Resources", "ExcelTemplate");
        var backendEnvironment = BuildBackendEnvironmentVariables(
            networkBinding.ListenUrl,
            databasePath,
            connectionString,
            environmentName: "Production",
            templatesDirectory);

        var backend = new ProcessLaunchConfiguration(
            DisplayName: "backend",
            FilePath: launchConfiguration.FilePath,
            WorkingDirectory: launchConfiguration.WorkingDirectory,
            Arguments: launchConfiguration.Arguments,
            EnvironmentVariables: backendEnvironment,
            StdoutLogFileName: "backend.stdout.log",
            StderrLogFileName: "backend.stderr.log");

        return new LauncherRuntimePlan(
            ModeLabel: "bundle",
            BaseUrl: networkBinding.LocalBaseUrl,
            RemoteBaseUrls: networkBinding.RemoteBaseUrls,
            Backend: backend,
            Frontend: null);
    }

    private static Dictionary<string, string> BuildBackendEnvironmentVariables(
        string listenUrl,
        string databasePath,
        string connectionString,
        string environmentName,
        string templatesDirectory)
    {
        var environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_ENVIRONMENT"] = environmentName,
            ["DOTNET_ENVIRONMENT"] = environmentName,
            ["ASPNETCORE_URLS"] = listenUrl,
            // Launcher runs only the local desktop instance, so keep admin/database tooling fully
            // available even when the UI is opened from another machine on the same LAN.
            ["GF3_ADMIN_ENABLED"] = bool.TrueString,
            ["GF3_ADMIN_ALLOW_REMOTE"] = bool.TrueString,
            ["GF3_ADMIN_ALLOW_WRITE"] = bool.TrueString,
            ["GF3_DATABASE_PATH"] = databasePath,
            ["GF3_CONNECTION_STRING"] = connectionString,
            ["ConnectionStrings__Default"] = connectionString,
            ["Logging__EventLog__LogLevel__Default"] = "None",
        };

        if (Directory.Exists(templatesDirectory))
        {
            environmentVariables["GF3_EXPORT_TEMPLATES_DIR"] = templatesDirectory;
        }

        return environmentVariables;
    }

    private static string ResolveWorkspaceRoot(string startingDirectory)
    {
        if (TryFindWorkspaceRoot(startingDirectory, out var workspaceRoot))
        {
            return workspaceRoot;
        }

        throw new DirectoryNotFoundException(
            "The GF3 workspace root was not found. Expected GF3.WebApi/WebApi.csproj and FrontEnd/package.json.");
    }

    private static bool TryFindWorkspaceRoot(string startingDirectory, out string workspaceRoot)
    {
        foreach (var searchRoot in EnumerateSearchRoots(startingDirectory))
        {
            var backendProjectPath = Path.Combine(searchRoot, "GF3.WebApi", "WebApi.csproj");
            var frontendPackageJsonPath = Path.Combine(searchRoot, "FrontEnd", "package.json");

            if (File.Exists(backendProjectPath) && File.Exists(frontendPackageJsonPath))
            {
                workspaceRoot = searchRoot;
                return true;
            }
        }

        workspaceRoot = string.Empty;
        return false;
    }

    private static IEnumerable<string> EnumerateSearchRoots(string startingDirectory)
    {
        var current = new DirectoryInfo(startingDirectory);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }
}
