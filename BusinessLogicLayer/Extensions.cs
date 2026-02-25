using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
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
            serviceCollection.AddScoped<ISqliteAdminFacade, SqliteAdminFacade>();

            return serviceCollection;
        }

        public static IServiceCollection AddBusinessLogicStack(this IServiceCollection serviceCollection, string connectionString, string databasePath)
        {
            serviceCollection.AddDataAccess(connectionString);
            serviceCollection.AddBusinessLogicLayer();
            serviceCollection.AddSingleton<ISqliteAdminService>(_ => new SqliteAdminService(connectionString, databasePath));

            return serviceCollection;
        }
    }
}
