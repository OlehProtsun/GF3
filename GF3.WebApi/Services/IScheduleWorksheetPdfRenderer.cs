namespace WebApi.Services;

public interface IScheduleWorksheetPdfRenderer
{
    byte[] Render(byte[] workbookBytes, CancellationToken ct = default);
}
