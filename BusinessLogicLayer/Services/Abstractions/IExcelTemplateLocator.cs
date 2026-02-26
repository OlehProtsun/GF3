namespace BusinessLogicLayer.Services.Abstractions;

public interface IExcelTemplateLocator
{
    string GetScheduleTemplatePath();
    string GetContainerTemplatePath();
}