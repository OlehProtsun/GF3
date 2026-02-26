using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Generators;
using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using BusinessLogicLayer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer
{
    public static class Extensions
    {
        public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IContainerService, ContainerService>();
            serviceCollection.AddScoped<IEmployeeService, EmployeeService>();
            serviceCollection.AddScoped<IShopService, ShopService>();
            serviceCollection.AddScoped<IScheduleService, ScheduleService>();
            serviceCollection.AddScoped<IScheduleEmployeeService, ScheduleEmployeeService>();
            serviceCollection.AddScoped<IScheduleSlotService, ScheduleSlotService>();
            serviceCollection.AddScoped<IBindService, BindService>();
            serviceCollection.AddScoped<IAvailabilityGroupService, AvailabilityGroupService>();
            serviceCollection.AddScoped<IShopFacade, ShopFacade>();
            serviceCollection.AddScoped<IEmployeeFacade, EmployeeFacade>();
            serviceCollection.AddScoped<IScheduleExportDataBuilder, Services.Export.ScheduleExportDataBuilder>();
            serviceCollection.AddScoped<IScheduleExcelContextBuilder, Services.Export.ScheduleExcelContextBuilder>();
            serviceCollection.AddScoped<IGraphExportService, GraphExportService>();
            serviceCollection.AddScoped<IGraphTemplateExportService, GraphTemplateExportService>();
            serviceCollection.AddSingleton<IExcelTemplateLocator, ExcelTemplateLocator>();
            serviceCollection.AddScoped<ISqliteAdminFacade, SqliteAdminFacade>();
            serviceCollection.AddScoped<IAdminDbService, AdminDbService>();
            serviceCollection.AddTransient<IScheduleGenerator, ScheduleGenerator>();

            return serviceCollection;
        }

        public static IServiceCollection AddBusinessLogicStack(this IServiceCollection serviceCollection)
        {
            var connectionString = Environment.GetEnvironmentVariable("GF3_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GF3");

                Directory.CreateDirectory(root);
                var databasePath = Path.Combine(root, "SQLite.db");
                connectionString = $"Data Source={databasePath}";
            }

            return serviceCollection.AddBusinessLogicStack(connectionString);
        }

        public static IServiceCollection AddBusinessLogicStack(this IServiceCollection serviceCollection, string connectionString)
        {
            var databasePath = ResolveDatabasePath(connectionString);
            return serviceCollection.AddBusinessLogicStack(connectionString, databasePath);
        }

        public static IServiceCollection AddBusinessLogicStack(this IServiceCollection serviceCollection, string connectionString, string databasePath)
        {
            serviceCollection.AddDataAccess(connectionString);
            serviceCollection.AddBusinessLogicLayer();
            serviceCollection.AddSingleton<ISqliteAdminService>(_ => new SqliteAdminService(connectionString, databasePath));
            serviceCollection.AddOptions<ExportTemplatesOptions>();

            return serviceCollection;
        }

        private static string ResolveDatabasePath(string connectionString)
        {
            var marker = "Data Source=";
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var dataSourcePart = parts.FirstOrDefault(x => x.StartsWith(marker, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(dataSourcePart))
            {
                return dataSourcePart.Substring(marker.Length);
            }

            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GF3");

            Directory.CreateDirectory(root);
            return Path.Combine(root, "SQLite.db");
        }
    }
}
