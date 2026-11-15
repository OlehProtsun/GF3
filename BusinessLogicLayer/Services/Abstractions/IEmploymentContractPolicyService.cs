using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

public interface IEmploymentContractPolicyService
{
    Task<EmploymentContractSettingsSnapshot> GetSettingsAsync(CancellationToken ct = default);
    Task<string> GetContractTypeAsync(int employeeId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, string>> GetContractTypesAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default);
    Task<MinimumHoursResult> GetShopMonthMinimumAsync(int employeeId, int shopId, int year, int month, CancellationToken ct = default);
    IReadOnlyList<AvailabilityGroupDayModel> BuildAutomaticWeekdayDays(int year, int month);
    Task<bool> IsRuleActiveAsync(int employeeId, EmploymentContractRule rule, CancellationToken ct = default);
    Task<IReadOnlyCollection<int>> GetAutomaticAvailabilityEmployeeIdsAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default);
}
