using BusinessLogicLayer.Options;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Resolves physical Excel template files used by export services.
/// The lookup order is deliberate:
/// <list type="number">
/// <item><description>Explicit environment override for local debugging or CI.</description></item>
/// <item><description>Configured directory from options.</description></item>
/// <item><description>Default LocalAppData location used by the running application.</description></item>
/// <item><description>Packaged fallback resources that can seed the preferred local directory when needed.</description></item>
/// </list>
/// This keeps exports flexible in development while still behaving predictably in production deployments.
/// </summary>
public sealed class ExcelTemplateLocator : IExcelTemplateLocator
{
    private const string TemplatesDirectoryEnvironmentVariable = "GF3_EXPORT_TEMPLATES_DIR";
    private readonly ExportTemplatesOptions _options;

    public ExcelTemplateLocator(IOptions<ExportTemplatesOptions> options)
    {
        _options = options.Value;
    }

    public string GetScheduleTemplatePath() => GetTemplatePath(_options.ScheduleTemplateFile);

    public string GetContainerTemplatePath() => GetTemplatePath(_options.ContainerTemplateFile);

    private string GetTemplatePath(string fileName)
    {
        var targetDirectory = ResolveBaseDir();
        var preferredPath = Path.Combine(targetDirectory, fileName);
        if (File.Exists(preferredPath))
        {
            return preferredPath;
        }

        var fallbackPath = ResolveFallbackPath(fileName);
        if (!string.IsNullOrWhiteSpace(fallbackPath) && File.Exists(fallbackPath))
        {
            return MaterializePreferredCopy(targetDirectory, preferredPath, fallbackPath);
        }

        throw new FileNotFoundException($"template not found path: {preferredPath}", preferredPath);
    }

    private string ResolveBaseDir()
    {
        var environmentDirectory = Environment.GetEnvironmentVariable(TemplatesDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentDirectory))
        {
            return environmentDirectory;
        }

        if (!string.IsNullOrWhiteSpace(_options.TemplateDirectory))
        {
            return _options.TemplateDirectory;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GF3",
            "source",
            "ExcelTemplate");
    }

    private string MaterializePreferredCopy(string targetDirectory, string preferredPath, string fallbackPath)
    {
        if (!_options.SeedToLocalAppData)
        {
            return fallbackPath;
        }

        Directory.CreateDirectory(targetDirectory);
        File.Copy(fallbackPath, preferredPath, overwrite: true);
        return preferredPath;
    }

    private static string? ResolveFallbackPath(string fileName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Resources", "Excel", fileName),
            Path.Combine(baseDirectory, "Resources", "ExcelTemplate", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "GF3.WebApi", "Resources", "ExcelTemplate", fileName),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
