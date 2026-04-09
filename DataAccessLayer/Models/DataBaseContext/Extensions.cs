using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessLayer.Models.DataBaseContext;

/// <summary>
/// Dependency-injection helpers for composing the data-access layer.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers repositories and the EF Core <see cref="AppDbContext"/> for SQLite access.
    /// </summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection serviceCollection, string connectionString)
    {
        serviceCollection.AddScoped<IContainerRepository, ContainerRepository>();
        serviceCollection.AddScoped<IEmployeeRepository, EmployeeRepository>();
        serviceCollection.AddScoped<IShopRepository, ShopRepository>();
        serviceCollection.AddScoped<IScheduleRepository, ScheduleRepository>();
        serviceCollection.AddScoped<ISchedulePresetRepository, SchedulePresetRepository>();
        serviceCollection.AddScoped<IScheduleEmployeeRepository, ScheduleEmployeeRepository>();
        serviceCollection.AddScoped<IScheduleSlotRepository, ScheduleSlotRepository>();
        serviceCollection.AddScoped<IScheduleCellStyleRepository, ScheduleCellStyleRepository>();
        serviceCollection.AddScoped<IBindRepository, BindRepository>();
        serviceCollection.AddScoped<IAvailabilityGroupRepository, AvailabilityGroupRepository>();
        serviceCollection.AddScoped<IAvailabilityGroupMemberRepository, AvailabilityGroupMemberRepository>();
        serviceCollection.AddScoped<IAvailabilityGroupDayRepository, AvailabilityGroupDayRepository>();

        serviceCollection.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        return serviceCollection;
    }
}
