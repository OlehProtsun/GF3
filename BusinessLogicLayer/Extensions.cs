using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Options;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer;

/// <summary>
/// Dependency-injection helpers for composing the business-logic stack.
/// </summary>
public static class Extensions
{
    private const string ConnectionStringEnvironmentVariable = "GF3_CONNECTION_STRING";
    private const string DefaultDatabaseFolderName = "GF3";
    private const string DefaultDatabaseFileName = "SQLite.db";
    private const string DataSourceMarker = "Data Source=";

    /// <summary>
    /// Registers business services/facades without configuring the database stack.
    /// </summary>
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

    /// <summary>
    /// Registers the full business-logic stack using a connection string resolved from the environment
    /// or, when absent, from the default local SQLite location.
    /// </summary>
    public static IServiceCollection AddBusinessLogicStack(this IServiceCollection serviceCollection)
    {
        var connectionString = ResolveConnectionStringFromEnvironmentOrDefault();
        return serviceCollection.AddBusinessLogicStack(connectionString);
    }

    /// <summary>
    /// Registers the full business-logic stack using the supplied connection string.
    /// </summary>
    public static IServiceCollection AddBusinessLogicStack(this IServiceCollection serviceCollection, string connectionString)
    {
        var databasePath = ResolveDatabasePath(connectionString);
        return serviceCollection.AddBusinessLogicStack(connectionString, databasePath);
    }

    /// <summary>
    /// Registers the full business-logic stack using explicit database connection settings.
    /// </summary>
    public static IServiceCollection AddBusinessLogicStack(this IServiceCollection serviceCollection, string connectionString, string databasePath)
    {
        serviceCollection.AddDataAccess(connectionString);
        serviceCollection.AddBusinessLogicLayer();
        serviceCollection.AddSingleton<ISqliteAdminService>(_ => new SqliteAdminService(connectionString, databasePath));
        serviceCollection.AddOptions<ExportTemplatesOptions>();

        return serviceCollection;
    }

    private static string ResolveConnectionStringFromEnvironmentOrDefault()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DefaultDatabaseFolderName);

        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, DefaultDatabaseFileName);
        return $"Data Source={databasePath}";
    }

    private static string ResolveDatabasePath(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var dataSourcePart = parts.FirstOrDefault(part => part.StartsWith(DataSourceMarker, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(dataSourcePart))
        {
            return dataSourcePart[DataSourceMarker.Length..];
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DefaultDatabaseFolderName);

        Directory.CreateDirectory(root);
        return Path.Combine(root, DefaultDatabaseFileName);
    }
}
