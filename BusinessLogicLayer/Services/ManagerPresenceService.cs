using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// In-memory live-presence registry keyed by SignalR connection id for manager accounts.
/// </summary>
public sealed class ManagerPresenceService : IManagerPresenceService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, int> _managerIdByConnectionId = new(StringComparer.Ordinal);
    private readonly Dictionary<int, HashSet<string>> _connectionIdsByManagerId = [];

    public bool IsManagerOnline(int managerId)
    {
        if (managerId <= 0)
        {
            return false;
        }

        lock (_syncRoot)
        {
            return _connectionIdsByManagerId.TryGetValue(managerId, out var connectionIds) && connectionIds.Count > 0;
        }
    }

    public IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> managerIds)
    {
        var idList = managerIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (idList.Count == 0)
        {
            return new Dictionary<int, bool>();
        }

        lock (_syncRoot)
        {
            return idList.ToDictionary(
                managerId => managerId,
                managerId => _connectionIdsByManagerId.TryGetValue(managerId, out var connectionIds) && connectionIds.Count > 0);
        }
    }

    public ManagerPresenceChange ConnectManager(int managerId, string connectionId, DateTimeOffset connectedAtUtc)
    {
        if (managerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(managerId));
        }

        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("Connection id is required.", nameof(connectionId));
        }

        lock (_syncRoot)
        {
            if (_managerIdByConnectionId.TryGetValue(connectionId, out var existingManagerId))
            {
                return new ManagerPresenceChange(
                    existingManagerId,
                    true,
                    false,
                    connectedAtUtc);
            }

            var wasOnline = _connectionIdsByManagerId.TryGetValue(managerId, out var existingConnections) && existingConnections.Count > 0;
            var connectionIds = existingConnections ?? [];
            connectionIds.Add(connectionId);
            _connectionIdsByManagerId[managerId] = connectionIds;
            _managerIdByConnectionId[connectionId] = managerId;

            return new ManagerPresenceChange(
                managerId,
                true,
                !wasOnline,
                connectedAtUtc);
        }
    }

    public ManagerPresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return null;
        }

        lock (_syncRoot)
        {
            if (!_managerIdByConnectionId.Remove(connectionId, out var managerId))
            {
                return null;
            }

            if (!_connectionIdsByManagerId.TryGetValue(managerId, out var connectionIds))
            {
                return new ManagerPresenceChange(
                    managerId,
                    false,
                    true,
                    disconnectedAtUtc);
            }

            connectionIds.Remove(connectionId);
            var isStillOnline = connectionIds.Count > 0;
            if (!isStillOnline)
            {
                _connectionIdsByManagerId.Remove(managerId);
            }

            return new ManagerPresenceChange(
                managerId,
                isStillOnline,
                !isStillOnline,
                disconnectedAtUtc);
        }
    }
}
