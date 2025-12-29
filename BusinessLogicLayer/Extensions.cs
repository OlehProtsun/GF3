using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer
{
    public static class Extensions
    {
        public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IContainerService, ContainerService>();
            serviceCollection.AddScoped<IEmployeeService, EmployeeService>();
            serviceCollection.AddScoped<IScheduleService, ScheduleService>();
            serviceCollection.AddScoped<IScheduleEmployeeService, ScheduleEmployeeService>();
            serviceCollection.AddScoped<IScheduleSlotService, ScheduleSlotService>();
            serviceCollection.AddScoped<IBindService, BindService>();
            serviceCollection.AddScoped<IAvailabilityGroupService, AvailabilityGroupService>();

            return serviceCollection;
        }

    }
}
