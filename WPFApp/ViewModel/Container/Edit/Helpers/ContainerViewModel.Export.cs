using DataAccessLayer.Models;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Applications.Export;

namespace WPFApp.ViewModel.Container.Edit
{
    public sealed partial class ContainerViewModel
    {
        internal Task ExportScheduleToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default)
            => _scheduleExportService.ExportToExcelAsync(context, filePath, ct);

        internal Task ExportScheduleToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default)
            => _scheduleExportService.ExportToSqlAsync(context, filePath, ct);

        internal Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = default)
            => _scheduleExportService.ExportContainerToExcelAsync(context, filePath, ct);

        internal Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = default)
            => _scheduleExportService.ExportContainerToSqlAsync(context, filePath, ct);

        internal Task<ScheduleModel?> LoadScheduleDetailsForExportAsync(int scheduleId, CancellationToken ct = default)
            => GetScheduleDetailsCachedAsync(scheduleId, ct);

        internal async Task<AvailabilityGroupExportData?> LoadAvailabilityGroupExportDataAsync(int? availabilityGroupId, CancellationToken ct = default)
        {
            if (!availabilityGroupId.HasValue || availabilityGroupId.Value <= 0)
                return null;

            var (group, members, days) = await _availabilityGroupService.LoadFullAsync(availabilityGroupId.Value, ct).ConfigureAwait(false);
            return new AvailabilityGroupExportData(group, members, days);
        }
    }
}
