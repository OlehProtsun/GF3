namespace WebApi.Realtime;

public sealed record ScheduleEditLockTarget(int ContainerId, int GraphId);

public sealed record ScheduleEditLockState(
    int ContainerId,
    int GraphId,
    bool IsLocked,
    string? LockedBy,
    DateTimeOffset ChangedAtUtc);

public interface IScheduleEditLockService
{
    IReadOnlyList<ScheduleEditLockState> SetLocks(string connectionId, string lockedBy, IEnumerable<ScheduleEditLockTarget> locks);

    IReadOnlyList<ScheduleEditLockState> ReleaseConnection(string connectionId);

    bool IsLocked(int containerId, int graphId);

    IReadOnlyList<ScheduleEditLockState> GetActiveLocks();
}

public sealed class ScheduleEditLockService : IScheduleEditLockService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, HashSet<ScheduleEditLockTarget>> _locksByConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<ScheduleEditLockTarget, Dictionary<string, string>> _holdersByLock = new();

    public IReadOnlyList<ScheduleEditLockState> SetLocks(
        string connectionId,
        string lockedBy,
        IEnumerable<ScheduleEditLockTarget> locks)
    {
        var requestedLocks = locks
            .Where(item => item.ContainerId > 0 && item.GraphId > 0)
            .ToHashSet();
        var changes = new List<ScheduleEditLockState>();
        var nowUtc = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            var currentLocks = _locksByConnection.GetValueOrDefault(connectionId) ?? [];
            var toRelease = currentLocks.Except(requestedLocks).ToList();
            var toAcquire = requestedLocks.Except(currentLocks).ToList();

            foreach (var target in toRelease)
            {
                var wasLocked = IsLockedCore(target);
                if (_holdersByLock.TryGetValue(target, out var holders))
                {
                    holders.Remove(connectionId);
                    if (holders.Count == 0)
                    {
                        _holdersByLock.Remove(target);
                    }
                }

                currentLocks.Remove(target);
                if (wasLocked != IsLockedCore(target))
                {
                    changes.Add(BuildState(target, nowUtc));
                }
            }

            foreach (var target in toAcquire)
            {
                var wasLocked = IsLockedCore(target);
                if (!_holdersByLock.TryGetValue(target, out var holders))
                {
                    holders = new Dictionary<string, string>(StringComparer.Ordinal);
                    _holdersByLock[target] = holders;
                }

                holders[connectionId] = lockedBy;
                currentLocks.Add(target);
                if (wasLocked != IsLockedCore(target))
                {
                    changes.Add(BuildState(target, nowUtc));
                }
            }

            if (currentLocks.Count > 0)
            {
                _locksByConnection[connectionId] = currentLocks;
            }
            else
            {
                _locksByConnection.Remove(connectionId);
            }
        }

        return changes;
    }

    public IReadOnlyList<ScheduleEditLockState> ReleaseConnection(string connectionId)
    {
        var changes = new List<ScheduleEditLockState>();
        var nowUtc = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            if (!_locksByConnection.Remove(connectionId, out var currentLocks))
            {
                return changes;
            }

            foreach (var target in currentLocks)
            {
                var wasLocked = IsLockedCore(target);
                if (_holdersByLock.TryGetValue(target, out var holders))
                {
                    holders.Remove(connectionId);
                    if (holders.Count == 0)
                    {
                        _holdersByLock.Remove(target);
                    }
                }

                if (wasLocked != IsLockedCore(target))
                {
                    changes.Add(BuildState(target, nowUtc));
                }
            }
        }

        return changes;
    }

    public bool IsLocked(int containerId, int graphId)
    {
        lock (_gate)
        {
            return IsLockedCore(new ScheduleEditLockTarget(containerId, graphId));
        }
    }

    public IReadOnlyList<ScheduleEditLockState> GetActiveLocks()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            return _holdersByLock.Keys
                .Select(target => BuildState(target, nowUtc))
                .Where(state => state.IsLocked)
                .ToList();
        }
    }

    private bool IsLockedCore(ScheduleEditLockTarget target)
        => _holdersByLock.TryGetValue(target, out var holders) && holders.Count > 0;

    private ScheduleEditLockState BuildState(ScheduleEditLockTarget target, DateTimeOffset changedAtUtc)
    {
        var lockedBy = _holdersByLock.TryGetValue(target, out var holders)
            ? holders.Values.Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal).FirstOrDefault()
            : null;

        return new ScheduleEditLockState(
            target.ContainerId,
            target.GraphId,
            IsLockedCore(target),
            lockedBy,
            changedAtUtc);
    }
}
