using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// In-memory live-presence registry keyed by SignalR connection id.
/// Multiple browser tabs for the same employee are tracked independently so the employee stays
/// online until the final active connection closes.
/// </summary>
public sealed class EmployeePresenceService : IEmployeePresenceService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, int> _employeeIdByConnectionId = new(StringComparer.Ordinal);
    private readonly Dictionary<int, HashSet<string>> _connectionIdsByEmployeeId = [];

    public bool IsEmployeeOnline(int employeeId)
    {
        if (employeeId <= 0)
        {
            return false;
        }

        lock (_syncRoot)
        {
            return _connectionIdsByEmployeeId.TryGetValue(employeeId, out var connectionIds) && connectionIds.Count > 0;
        }
    }

    public IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> employeeIds)
    {
        var idList = employeeIds
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
                employeeId => employeeId,
                employeeId => _connectionIdsByEmployeeId.TryGetValue(employeeId, out var connectionIds) && connectionIds.Count > 0);
        }
    }

    public EmployeePresenceChange ConnectEmployee(int employeeId, string connectionId, DateTimeOffset connectedAtUtc)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId));
        }

        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("Connection id is required.", nameof(connectionId));
        }

        lock (_syncRoot)
        {
            if (_employeeIdByConnectionId.TryGetValue(connectionId, out var existingEmployeeId))
            {
                return new EmployeePresenceChange(
                    existingEmployeeId,
                    true,
                    false,
                    connectedAtUtc);
            }

            var wasOnline = _connectionIdsByEmployeeId.TryGetValue(employeeId, out var existingConnections) && existingConnections.Count > 0;
            var connectionIds = existingConnections ?? [];
            connectionIds.Add(connectionId);
            _connectionIdsByEmployeeId[employeeId] = connectionIds;
            _employeeIdByConnectionId[connectionId] = employeeId;

            return new EmployeePresenceChange(
                employeeId,
                true,
                !wasOnline,
                connectedAtUtc);
        }
    }

    public EmployeePresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return null;
        }

        lock (_syncRoot)
        {
            if (!_employeeIdByConnectionId.Remove(connectionId, out var employeeId))
            {
                return null;
            }

            if (!_connectionIdsByEmployeeId.TryGetValue(employeeId, out var connectionIds))
            {
                return new EmployeePresenceChange(
                    employeeId,
                    false,
                    true,
                    disconnectedAtUtc);
            }

            connectionIds.Remove(connectionId);
            var isStillOnline = connectionIds.Count > 0;
            if (!isStillOnline)
            {
                _connectionIdsByEmployeeId.Remove(employeeId);
            }

            return new EmployeePresenceChange(
                employeeId,
                isStillOnline,
                !isStillOnline,
                disconnectedAtUtc);
        }
    }
}
