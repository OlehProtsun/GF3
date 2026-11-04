namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Tracks live employee connections so manager views can show precise online presence.
/// This service is intentionally singleton-scoped because presence is an application-wide concern.
/// </summary>
public interface IEmployeePresenceService
{
    bool IsEmployeeOnline(int employeeId);

    IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> employeeIds);

    EmployeePresenceChange ConnectEmployee(int employeeId, string connectionId, DateTimeOffset connectedAtUtc);

    EmployeePresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc);
}

/// <summary>
/// Result of a live-presence transition caused by a SignalR connect/disconnect event.
/// </summary>
public sealed record EmployeePresenceChange(
    int EmployeeId,
    bool IsOnline,
    bool StateChanged,
    DateTimeOffset OccurredAtUtc);
