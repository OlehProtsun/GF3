using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Generators;

/// <summary>
/// Scheduling engine contract that generates concrete graph slots from graph settings, availability,
/// and employee assignments.
/// </summary>
public interface IScheduleGenerator
{
    /// <summary>
    /// Generates slots for the supplied graph definition.
    /// </summary>
    Task<IList<ScheduleSlotModel>> GenerateAsync(
        ScheduleModel schedule,
        IEnumerable<AvailabilityGroupModel> availabilities,
        IEnumerable<ScheduleEmployeeModel> employees,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}
