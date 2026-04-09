namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Resolves physical file paths for Excel export templates.
/// </summary>
public interface IExcelTemplateLocator
{
    /// <summary>
    /// Returns the template path used for single-graph Excel exports.
    /// </summary>
    string GetScheduleTemplatePath();

    /// <summary>
    /// Returns the template path used for container-level Excel exports.
    /// </summary>
    string GetContainerTemplatePath();
}
