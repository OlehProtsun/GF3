using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models.DataBaseContext
{
    public static class Extensions
    {
        public static IServiceCollection AddDataAccess(this IServiceCollection serviceCollection, string connectionString)
        {
            serviceCollection.AddScoped<IContainerRepository, ContainerRepository>();
            serviceCollection.AddScoped<IEmployeeRepository, EmployeeRepository>();
            serviceCollection.AddScoped<IShopRepository, ShopRepository>();
            serviceCollection.AddScoped<IScheduleRepository, ScheduleRepository>();
            serviceCollection.AddScoped<IScheduleEmployeeRepository, ScheduleEmployeeRepository>();
            serviceCollection.AddScoped<IScheduleSlotRepository, ScheduleSlotRepository>();
            serviceCollection.AddScoped<IScheduleCellStyleRepository, ScheduleCellStyleRepository>();
            serviceCollection.AddScoped<IBindRepository, BindRepository>();
            serviceCollection.AddScoped<IAvailabilityGroupRepository, AvailabilityGroupRepository>();
            serviceCollection.AddScoped<IAvailabilityGroupMemberRepository, AvailabilityGroupMemberRepository>();
            serviceCollection.AddScoped<IAvailabilityGroupDayRepository, AvailabilityGroupDayRepository>();



            serviceCollection.AddDbContext<AppDbContext>(x =>
            {
                x.UseSqlite(connectionString);
            });      
            return serviceCollection;
        }
    }
}
