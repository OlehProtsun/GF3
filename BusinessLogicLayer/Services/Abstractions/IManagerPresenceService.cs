namespace BusinessLogicLayer.Services.Abstractions;

public interface IManagerPresenceService
{
    bool IsManagerOnline(int managerId);

    IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> managerIds);

    ManagerPresenceChange ConnectManager(int managerId, string connectionId, DateTimeOffset connectedAtUtc);

    ManagerPresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc);
}

public sealed record ManagerPresenceChange(
    int ManagerId,
    bool IsOnline,
    bool StateChanged,
    DateTimeOffset OccurredAtUtc);
