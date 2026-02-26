namespace BusinessLogicLayer.Options;

public sealed class ExportTemplatesOptions
{
    public string? TemplateDirectory { get; set; }
    public string ScheduleTemplateFile { get; set; } = "ScheduleTemplate.xlsx";
    public string ContainerTemplateFile { get; set; } = "ContainerTemplate.xlsx";
    public bool SeedToLocalAppData { get; set; } = true;
}