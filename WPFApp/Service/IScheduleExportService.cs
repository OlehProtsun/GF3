using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.Service
{
    public interface IScheduleExportService
    {
        Task ExportToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default);
        Task ExportToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default);
        Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = default);
        Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = default);
    }
}
