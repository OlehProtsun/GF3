namespace BusinessLogicLayer.Options;

/// <summary>
/// Configures where export services resolve Excel templates from.
/// These values are deployment concerns rather than domain rules, so the options class stays intentionally simple.
/// </summary>
public sealed class ExportTemplatesOptions
{
    /// <summary>
    /// Optional explicit directory that contains the template files.
    /// When omitted, the application falls back to the default LocalAppData location.
    /// </summary>
    public string? TemplateDirectory { get; set; }

    /// <summary>
    /// File name of the schedule export template inside the resolved template directory.
    /// </summary>
    public string ScheduleTemplateFile { get; set; } = "ScheduleTemplate.xlsx";

    /// <summary>
    /// File name of the container export template inside the resolved template directory.
    /// </summary>
    public string ContainerTemplateFile { get; set; } = "ContainerTemplate.xlsx";

    /// <summary>
    /// When enabled, packaged fallback templates are copied into the preferred local directory before use.
    /// This keeps the runtime path stable for operators and future template updates.
    /// </summary>
    public bool SeedToLocalAppData { get; set; } = true;
}
