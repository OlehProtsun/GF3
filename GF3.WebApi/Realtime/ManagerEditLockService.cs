namespace WebApi.Realtime;

public static class ManagerEditResourceTypes
{
    public const string Schedule = "schedule";
    public const string AvailabilityGroup = "availability-group";
    public const string Employee = "employee";
    public const string Shop = "shop";
    public const string Container = "container";
    public const string AvailabilityBind = "availability-bind";
    public const string ManagerProfile = "manager-profile";
}

public static class ManagerEditLockTargets
{
    public static ManagerEditLockTarget Schedule(int containerId, int graphId)
        => new(
            ManagerEditResourceTypes.Schedule,
            $"{containerId}:{graphId}",
            containerId,
            graphId);

    public static ManagerEditLockTarget AvailabilityGroup(int groupId)
        => ById(ManagerEditResourceTypes.AvailabilityGroup, groupId);

    public static ManagerEditLockTarget Employee(int employeeId)
        => ById(ManagerEditResourceTypes.Employee, employeeId);

    public static ManagerEditLockTarget Shop(int shopId)
        => ById(ManagerEditResourceTypes.Shop, shopId);

    public static ManagerEditLockTarget Container(int containerId)
        => ById(ManagerEditResourceTypes.Container, containerId);

    public static ManagerEditLockTarget AvailabilityBind(int bindId)
        => ById(ManagerEditResourceTypes.AvailabilityBind, bindId);

    public static ManagerEditLockTarget ManagerProfile(int managerId)
        => ById(ManagerEditResourceTypes.ManagerProfile, managerId);

    private static ManagerEditLockTarget ById(string resourceType, int resourceId)
        => new(resourceType, resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture));
}

public sealed record ManagerEditLockTarget(
    string ResourceType,
    string ResourceId,
    int? ContainerId = null,
    int? GraphId = null);

public sealed record ManagerEditLockState(
    string ResourceType,
    string ResourceId,
    int? ContainerId,
    int? GraphId,
    bool IsLocked,
    string? LockedBy,
    int? LockedByManagerId,
    DateTimeOffset ChangedAtUtc);

public sealed record ManagerEditLockSetResult(
    IReadOnlyList<ManagerEditLockState> RequestedStates,
    IReadOnlyList<ManagerEditLockState> ChangedStates);

public sealed record ScheduleEditLockTarget(int ContainerId, int GraphId);

public sealed record ScheduleEditLockState(
    int ContainerId,
    int GraphId,
    bool IsLocked,
    string? LockedBy,
    DateTimeOffset ChangedAtUtc,
    int? LockedByManagerId = null);

public interface IManagerEditLockService
{
    ManagerEditLockSetResult SetLocks(
        string connectionId,
        int? managerId,
        string lockedBy,
        IEnumerable<ManagerEditLockTarget> locks);

    IReadOnlyList<ManagerEditLockState> ReleaseConnection(string connectionId);

    ManagerEditLockState? GetLockState(ManagerEditLockTarget target);

    IReadOnlyList<ManagerEditLockState> GetActiveLocks();

    bool IsLockedByAnotherManager(ManagerEditLockTarget target, int? managerId);
}

public interface IScheduleEditLockService
{
    IReadOnlyList<ScheduleEditLockState> SetLocks(string connectionId, string lockedBy, IEnumerable<ScheduleEditLockTarget> locks);

    IReadOnlyList<ScheduleEditLockState> ReleaseConnection(string connectionId);

    bool IsLocked(int containerId, int graphId);

    IReadOnlyList<ScheduleEditLockState> GetActiveLocks();
}

public sealed class ManagerEditLockService : IManagerEditLockService, IScheduleEditLockService
{
    private sealed record LockOwner(string ConnectionId, int? ManagerId, string DisplayName)
    {
        public string OwnerKey => ManagerId.HasValue
            ? $"manager:{ManagerId.Value}"
            : $"connection:{ConnectionId}";
    }

    private readonly object _gate = new();
    private readonly Dictionary<string, HashSet<ManagerEditLockTarget>> _locksByConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<ManagerEditLockTarget, Dictionary<string, LockOwner>> _holdersByLock = new();

    public ManagerEditLockSetResult SetLocks(
        string connectionId,
        int? managerId,
        string lockedBy,
        IEnumerable<ManagerEditLockTarget> locks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var owner = new LockOwner(connectionId, managerId, NormalizeDisplayName(lockedBy));
        var requestedLocks = locks
            .Select(NormalizeTarget)
            .Where(target => target is not null)
            .Select(target => target!)
            .ToHashSet();
        var requestedStates = new List<ManagerEditLockState>();
        var changes = new List<ManagerEditLockState>();
        var nowUtc = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            var currentLocks = _locksByConnection.GetValueOrDefault(connectionId) ?? [];
            var toRelease = currentLocks.Except(requestedLocks).ToList();
            var toAcquire = requestedLocks.Except(currentLocks).ToList();

            foreach (var target in toRelease)
            {
                var before = BuildState(target, nowUtc);
                RemoveHolder(target, connectionId);
                var after = BuildState(target, nowUtc);
                currentLocks.Remove(target);
                if (HasExternallyVisibleChange(before, after))
                {
                    changes.Add(after);
                }
            }

            foreach (var target in toAcquire)
            {
                var before = BuildState(target, nowUtc);
                if (!IsHeldByAnotherOwner(target, owner.OwnerKey))
                {
                    if (!_holdersByLock.TryGetValue(target, out var holders))
                    {
                        holders = new Dictionary<string, LockOwner>(StringComparer.Ordinal);
                        _holdersByLock[target] = holders;
                    }

                    holders[connectionId] = owner;
                    currentLocks.Add(target);
                }

                var after = BuildState(target, nowUtc);
                if (HasExternallyVisibleChange(before, after))
                {
                    changes.Add(after);
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

            requestedStates.AddRange(requestedLocks.Select(target => BuildState(target, nowUtc)));
        }

        return new ManagerEditLockSetResult(requestedStates, changes);
    }

    public IReadOnlyList<ManagerEditLockState> ReleaseConnection(string connectionId)
    {
        var changes = new List<ManagerEditLockState>();
        var nowUtc = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            if (!_locksByConnection.Remove(connectionId, out var currentLocks))
            {
                return changes;
            }

            foreach (var target in currentLocks)
            {
                var before = BuildState(target, nowUtc);
                RemoveHolder(target, connectionId);
                var after = BuildState(target, nowUtc);
                if (HasExternallyVisibleChange(before, after))
                {
                    changes.Add(after);
                }
            }
        }

        return changes;
    }

    public ManagerEditLockState? GetLockState(ManagerEditLockTarget target)
    {
        var normalizedTarget = NormalizeTarget(target);
        if (normalizedTarget is null)
        {
            return null;
        }

        lock (_gate)
        {
            return BuildState(normalizedTarget, DateTimeOffset.UtcNow);
        }
    }

    public IReadOnlyList<ManagerEditLockState> GetActiveLocks()
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

    public bool IsLockedByAnotherManager(ManagerEditLockTarget target, int? managerId)
    {
        var normalizedTarget = NormalizeTarget(target);
        if (normalizedTarget is null)
        {
            return false;
        }

        var ownerKey = managerId.HasValue ? $"manager:{managerId.Value}" : null;
        lock (_gate)
        {
            return _holdersByLock.TryGetValue(normalizedTarget, out var holders) &&
                   holders.Count > 0 &&
                   (ownerKey is null || holders.Values.Any(owner => !string.Equals(owner.OwnerKey, ownerKey, StringComparison.Ordinal)));
        }
    }

    IReadOnlyList<ScheduleEditLockState> IScheduleEditLockService.SetLocks(
        string connectionId,
        string lockedBy,
        IEnumerable<ScheduleEditLockTarget> locks)
    {
        var result = SetLocks(
            connectionId,
            managerId: null,
            lockedBy,
            locks.Select(target => ManagerEditLockTargets.Schedule(target.ContainerId, target.GraphId)));

        return result.ChangedStates.Select(ToScheduleState).Where(state => state is not null).Select(state => state!).ToList();
    }

    IReadOnlyList<ScheduleEditLockState> IScheduleEditLockService.ReleaseConnection(string connectionId)
        => ReleaseConnection(connectionId).Select(ToScheduleState).Where(state => state is not null).Select(state => state!).ToList();

    bool IScheduleEditLockService.IsLocked(int containerId, int graphId)
    {
        var state = GetLockState(ManagerEditLockTargets.Schedule(containerId, graphId));
        return state?.IsLocked == true;
    }

    IReadOnlyList<ScheduleEditLockState> IScheduleEditLockService.GetActiveLocks()
        => GetActiveLocks().Select(ToScheduleState).Where(state => state is not null).Select(state => state!).ToList();

    private static ManagerEditLockTarget? NormalizeTarget(ManagerEditLockTarget target)
    {
        var resourceType = target.ResourceType.Trim().ToLowerInvariant();
        var resourceId = target.ResourceId.Trim();
        if (resourceType.Length == 0 || resourceId.Length == 0)
        {
            return null;
        }

        if (resourceType == ManagerEditResourceTypes.Schedule)
        {
            if (target.ContainerId is not > 0 || target.GraphId is not > 0)
            {
                return null;
            }

            return ManagerEditLockTargets.Schedule(target.ContainerId.Value, target.GraphId.Value);
        }

        return new ManagerEditLockTarget(resourceType, resourceId);
    }

    private static string NormalizeDisplayName(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length > 0 ? trimmed : "Manager";
    }

    private bool IsHeldByAnotherOwner(ManagerEditLockTarget target, string ownerKey)
        => _holdersByLock.TryGetValue(target, out var holders) &&
           holders.Values.Any(owner => !string.Equals(owner.OwnerKey, ownerKey, StringComparison.Ordinal));

    private void RemoveHolder(ManagerEditLockTarget target, string connectionId)
    {
        if (!_holdersByLock.TryGetValue(target, out var holders))
        {
            return;
        }

        holders.Remove(connectionId);
        if (holders.Count == 0)
        {
            _holdersByLock.Remove(target);
        }
    }

    private ManagerEditLockState BuildState(ManagerEditLockTarget target, DateTimeOffset changedAtUtc)
    {
        if (!_holdersByLock.TryGetValue(target, out var holders) || holders.Count == 0)
        {
            return new ManagerEditLockState(
                target.ResourceType,
                target.ResourceId,
                target.ContainerId,
                target.GraphId,
                IsLocked: false,
                LockedBy: null,
                LockedByManagerId: null,
                changedAtUtc);
        }

        var visibleOwner = holders.Values
            .OrderBy(owner => owner.DisplayName, StringComparer.Ordinal)
            .ThenBy(owner => owner.ConnectionId, StringComparer.Ordinal)
            .First();

        return new ManagerEditLockState(
            target.ResourceType,
            target.ResourceId,
            target.ContainerId,
            target.GraphId,
            IsLocked: true,
            visibleOwner.DisplayName,
            visibleOwner.ManagerId,
            changedAtUtc);
    }

    private static bool HasExternallyVisibleChange(ManagerEditLockState before, ManagerEditLockState after)
        => before.IsLocked != after.IsLocked ||
           !string.Equals(before.LockedBy, after.LockedBy, StringComparison.Ordinal) ||
           before.LockedByManagerId != after.LockedByManagerId;

    private static ScheduleEditLockState? ToScheduleState(ManagerEditLockState state)
    {
        if (state.ResourceType != ManagerEditResourceTypes.Schedule ||
            state.ContainerId is not > 0 ||
            state.GraphId is not > 0)
        {
            return null;
        }

        return new ScheduleEditLockState(
            state.ContainerId.Value,
            state.GraphId.Value,
            state.IsLocked,
            state.LockedBy,
            state.ChangedAtUtc,
            state.LockedByManagerId);
    }
}
