using Xunit;

namespace ArchitectureTests;

public class WpfBoundaryTests
{
    [Fact]
    public void WpfProject_ShouldNotReferenceDalProject()
    {
        var csprojPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../WPFApp/WPFApp.csproj"));
        var text = File.ReadAllText(csprojPath);

        Assert.DoesNotContain("..\\DataAccessLayer\\DataAccessLayer.csproj", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WpfSource_ShouldNotContainDataAccessLayerNamespaceUsage()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../WPFApp"));
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var text = File.ReadAllText(path);
                return text.Contains("using DataAccessLayer", StringComparison.Ordinal)
                    || text.Contains("DataAccessLayer.", StringComparison.Ordinal);
            })
            .ToList();

        Assert.True(offenders.Count == 0, "DAL leakage files:\n" + string.Join("\n", offenders));
    }
}
