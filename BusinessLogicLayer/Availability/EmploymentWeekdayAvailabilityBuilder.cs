using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Availability;

public static class EmploymentWeekdayAvailabilityBuilder
{
    public static AvailabilityGroupModel Build(int year, int month, IReadOnlyCollection<int> employeeIds,
        IEmploymentContractPolicyService policy)
        => new()
        {
            IsEmploymentCalendar = true, Name = "Employment weekday calendar", Year = year, Month = month,
            Members = employeeIds.Select(employeeId => new AvailabilityGroupMemberModel
            {
                EmployeeId = employeeId,
                Days = policy.BuildAutomaticWeekdayDays(year, month).ToList(),
            }).ToList(),
        };
}
