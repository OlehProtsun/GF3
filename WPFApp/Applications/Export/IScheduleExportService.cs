/*
  Опис файлу: цей модуль містить реалізацію компонента IScheduleExportService у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.Applications.Export
{
    /// <summary>
    /// Визначає публічний елемент `public interface IScheduleExportService` та контракт його використання у шарі WPFApp.
    /// </summary>
    public interface IScheduleExportService
    {
        Task ExportToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default);
        Task ExportToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default);
        Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = default);
        Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = default);
    }
}
