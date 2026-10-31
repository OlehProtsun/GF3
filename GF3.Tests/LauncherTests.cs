using System.Text.Json;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

[Trait("Category", "LocalOnly")]
public sealed class LauncherTests
{
    [Fact]
    public void LauncherOptions_Parse_RecognizesFlags()
    {
        var optionsType = ReflectionTestHelper.GetType("GF3.Launcher", "GF3.Launcher.LauncherOptions");
        var result = ReflectionTestHelper.InvokeStatic(optionsType, "Parse", new object[] { new[] { "--no-browser", "--workspace" } });

        Assert.NotNull(result);
        Assert.True(ReflectionTestHelper.GetPropertyValue<bool>(result!, "SuppressBrowser"));
        Assert.Equal("Workspace", ReflectionTestHelper.GetPropertyValue<object>(result!, "RunMode").ToString());
    }

    [Fact]
    public void BackendLocator_Resolve_FindsBackendDll()
    {
        var root = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var backendDirectory = Path.Combine(root, "backend");
        Directory.CreateDirectory(backendDirectory);
        File.WriteAllText(Path.Combine(backendDirectory, "WebApi.dll"), string.Empty);

        var locatorType = ReflectionTestHelper.GetType("GF3.Launcher", "GF3.Launcher.BackendLocator");
        var configuration = ReflectionTestHelper.InvokeStatic(locatorType, "Resolve", root);

        Assert.NotNull(configuration);
        Assert.Equal("dotnet", ReflectionTestHelper.GetPropertyValue<string>(configuration!, "FilePath"));
        Assert.Equal(backendDirectory, ReflectionTestHelper.GetPropertyValue<string>(configuration!, "WorkingDirectory"));

        var arguments = ReflectionTestHelper.GetPropertyValue<IReadOnlyList<string>>(configuration!, "Arguments");
        Assert.Single(arguments);
        Assert.Equal(Path.Combine(backendDirectory, "WebApi.dll"), arguments[0]);
    }

    [Fact]
    public void LauncherPaths_ResolveDatabasePath_UsesEnvironmentValue()
    {
        var previousValue = Environment.GetEnvironmentVariable("GF3_DATABASE_PATH");
        var databasePath = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"), "launcher.db");
        Environment.SetEnvironmentVariable("GF3_DATABASE_PATH", databasePath);

        try
        {
            var pathsType = ReflectionTestHelper.GetType("GF3.Launcher", "GF3.Launcher.LauncherPaths");
            var resolved = (string)ReflectionTestHelper.InvokeStatic(pathsType, "ResolveDatabasePath")!;

            Assert.Equal(Path.GetFullPath(databasePath), resolved);
            Assert.True(Directory.Exists(Path.GetDirectoryName(resolved)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_DATABASE_PATH", previousValue);
        }
    }

    [Fact]
    public void LauncherDatabaseSelectionStore_ResolveDatabasePath_UsesSavedSelection()
    {
        var root = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var statePath = Path.Combine(root, "database-selection.json");
        var databasePath = Path.Combine(root, "custom.db");

        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(databasePath, "db");
            File.WriteAllText(
                statePath,
                JsonSerializer.Serialize(new
                {
                    databasePath,
                    updatedAtUtc = DateTimeOffset.UtcNow,
                }));

            var storeType = ReflectionTestHelper.GetType("GF3.Launcher", "GF3.Launcher.LauncherDatabaseSelectionStore");
            var resolved = (string)ReflectionTestHelper.InvokeStatic(
                storeType,
                "ResolveDatabasePath",
                Path.Combine(root, "fallback.db"),
                root)!;

            Assert.Equal(Path.GetFullPath(databasePath), resolved);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
    [Fact]
    public void LauncherRuntimeResolver_ResolveWorkspaceMode_BuildsExpectedPlan()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"), "Workspace");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "GF3.WebApi", "Resources", "ExcelTemplate"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "FrontEnd"));
        File.WriteAllText(Path.Combine(workspaceRoot, "GF3.WebApi", "WebApi.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(workspaceRoot, "FrontEnd", "package.json"), "{}");

        var optionsType = ReflectionTestHelper.GetType("GF3.Launcher", "GF3.Launcher.LauncherOptions");
        var runModeType = ReflectionTestHelper.GetType("GF3.Launcher", "GF3.Launcher.LauncherRunMode");
        var options = ReflectionTestHelper.Create(
            optionsType,
            false,
            ReflectionTestHelper.ParseEnum(runModeType, "Workspace"));

        var resolverType = ReflectionTestHelper.GetType("GF3.Launcher", "GF3.Launcher.LauncherRuntimeResolver");
        var plan = ReflectionTestHelper.InvokeStatic(
            resolverType,
            "Resolve",
            options,
            workspaceRoot,
            5055,
            Path.Combine(workspaceRoot, "data.db"));

        Assert.NotNull(plan);
        Assert.Equal("workspace", ReflectionTestHelper.GetPropertyValue<string>(plan!, "ModeLabel"));
        Assert.Equal("http://127.0.0.1:5055", ReflectionTestHelper.GetPropertyValue<string>(plan!, "BaseUrl"));

        var backend = ReflectionTestHelper.GetPropertyValue<object>(plan!, "Backend");
        var frontend = ReflectionTestHelper.GetPropertyValue<object>(plan!, "Frontend");

        Assert.Equal("dotnet", ReflectionTestHelper.GetPropertyValue<string>(backend, "FilePath"));
        Assert.Equal("npm", ReflectionTestHelper.GetPropertyValue<string>(frontend, "FilePath"));

        var backendEnvironment = ReflectionTestHelper.GetPropertyValue<IReadOnlyDictionary<string, string>>(backend, "EnvironmentVariables");
        Assert.Equal("Data Source=" + Path.Combine(workspaceRoot, "data.db"), backendEnvironment["GF3_CONNECTION_STRING"]);
        Assert.Equal("Development", backendEnvironment["ASPNETCORE_ENVIRONMENT"]);
        Assert.True(backendEnvironment.ContainsKey("GF3_EXPORT_TEMPLATES_DIR"));
    }
}
