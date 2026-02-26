using BusinessLogicLayer.Options;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace BusinessLogicLayer.Services;

public sealed class ExcelTemplateLocator : IExcelTemplateLocator
{
    private const string EnvVar = "GF3_EXPORT_TEMPLATES_DIR";
    private readonly ExportTemplatesOptions _options;

    public ExcelTemplateLocator(IOptions<ExportTemplatesOptions> options)
    {
        _options = options.Value;
    }

    public string GetScheduleTemplatePath() => GetTemplatePath(_options.ScheduleTemplateFile);

    public string GetContainerTemplatePath() => GetTemplatePath(_options.ContainerTemplateFile);

    private string GetTemplatePath(string fileName)
    {
        var baseDir = ResolveBaseDir();
        var preferred = Path.Combine(baseDir, fileName);
        if (File.Exists(preferred))
            return preferred;

        var fallback = ResolveFallback(fileName);
        if (!string.IsNullOrWhiteSpace(fallback) && File.Exists(fallback))
        {
            if (_options.SeedToLocalAppData)
            {
                Directory.CreateDirectory(baseDir);
                File.Copy(fallback, preferred, overwrite: true);
                return preferred;
            }

            return fallback;
        }

        throw new FileNotFoundException($"template not found path: {preferred}", preferred);
    }

    private string ResolveBaseDir()
    {
        var env = Environment.GetEnvironmentVariable(EnvVar);
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        if (!string.IsNullOrWhiteSpace(_options.TemplateDirectory))
            return _options.TemplateDirectory!;

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GF3", "source", "ExcelTemplate");
    }

    private static string? ResolveFallback(string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Resources", "Excel", fileName),
            Path.Combine(baseDir, "Resources", "ExcelTemplate", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "GF3.WebApi", "Resources", "ExcelTemplate", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "WPFApp", "Resources", "Excel", fileName)
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}