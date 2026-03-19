/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.Export у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Applications.Export;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
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
