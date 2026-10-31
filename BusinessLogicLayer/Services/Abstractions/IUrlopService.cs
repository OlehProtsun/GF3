using BusinessLogicLayer.Contracts.Urlop;

namespace BusinessLogicLayer.Services.Abstractions;

public interface IUrlopService
{
    Task<UrlopEmployeeCalendar> GetEmployeeCalendarAsync(int employeeId, DateOnly currentMonth, CancellationToken ct);
    Task<UrlopManagerMonth> GetManagerMonthAsync(int shopId, int year, int month, CancellationToken ct);
    Task<IReadOnlyList<ApprovedUrlopDay>> GetApprovedDaysAsync(DateOnly from, DateOnly to, int? shopId, CancellationToken ct);
    Task<IReadOnlyList<ApprovedUrlopDay>> GetEmployeeApprovedMonthAsync(int employeeId, int shopId, int year, int month, CancellationToken ct);
    Task<UrlopRequest> SubmitAsync(int employeeId, int shopId, int year, int month, IReadOnlyList<int> days, DateOnly today, CancellationToken ct);
    Task<UrlopMonth> SetMonthOpenAsync(int shopId, int year, int month, bool isOpen, CancellationToken ct);
    Task<UrlopRequest> CreateApprovedAsync(int managerId, int shopId, int employeeId, int year, int month, IReadOnlyList<int> days, CancellationToken ct);
    Task<UrlopRequest> ReplaceDaysAsync(int managerId, Guid requestId, IReadOnlyList<int> days, CancellationToken ct);
    Task<UrlopRequest> ApproveAsync(int managerId, Guid requestId, CancellationToken ct);
    Task<UrlopRequest> RejectAsync(int managerId, Guid requestId, CancellationToken ct);
    Task DeleteAsync(int managerId, Guid requestId, CancellationToken ct);
    Task EnsureAssignmentsAllowedAsync(int shopId, int year, int month, IEnumerable<(int employeeId, int day)> assignments, CancellationToken ct);
}
