using DataAccessLayer.Models;

namespace WebApi.Services;

public interface IEmployeeWebPushSender
{
    Task SendAsync(EmployeePushSubscriptionModel subscription, EmployeePushCandidate candidate, CancellationToken ct);
}
