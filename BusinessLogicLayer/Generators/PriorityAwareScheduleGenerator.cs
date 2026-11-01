using BusinessLogicLayer.Contracts.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Generators;

public sealed class PriorityAwareScheduleGenerator(
    ScheduleGenerator legacy, PriorityScheduleGenerator priority, AppDbContext db) : IScheduleGenerator
{
    public async Task<IList<ScheduleSlotModel>> GenerateAsync(ScheduleModel schedule,
        IEnumerable<AvailabilityGroupModel> availabilities, IEnumerable<ScheduleEmployeeModel> employees,
        IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var employeeList = employees.ToList();
        if (!employeeList.Any(employee => employee.IsPriority))
            return await legacy.GenerateAsync(schedule, availabilities, employeeList, progress, ct).ConfigureAwait(false);

        var settings = await db.PriorityGenerationSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1, ct).ConfigureAwait(false);
        return await priority.GenerateAsync(schedule, availabilities.ToList(), employeeList,
            settings?.ShiftHours ?? 8, settings?.WeekendsOff ?? true, progress, ct,
            respectAvailability: settings?.RespectAvailability ?? true).ConfigureAwait(false);
    }
}
