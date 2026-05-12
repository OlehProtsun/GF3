using BusinessLogicLayer.Services;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class PresenceAndLockServiceTests
{
    [Fact]
    public void EmployeePresenceService_TracksMultipleConnectionsAndDisconnectTransitions()
    {
        var service = new EmployeePresenceService();
        var now = DateTimeOffset.UtcNow;

        var firstConnection = service.ConnectEmployee(7, "connection-a", now);
        var secondConnection = service.ConnectEmployee(7, "connection-b", now.AddSeconds(1));
        var duplicateConnection = service.ConnectEmployee(99, "connection-a", now.AddSeconds(2));
        var firstDisconnect = service.DisconnectConnection("connection-a", now.AddSeconds(3));
        var finalDisconnect = service.DisconnectConnection("connection-b", now.AddSeconds(4));
        var missingDisconnect = service.DisconnectConnection("missing", now.AddSeconds(5));

        Assert.False(service.IsEmployeeOnline(0));
        Assert.Equal(7, firstConnection.EmployeeId);
        Assert.True(firstConnection.IsOnline);
        Assert.True(firstConnection.StateChanged);
        Assert.False(secondConnection.StateChanged);
        Assert.Equal(7, duplicateConnection.EmployeeId);
        Assert.True(duplicateConnection.IsOnline);
        Assert.False(duplicateConnection.StateChanged);
        Assert.NotNull(firstDisconnect);
        Assert.True(firstDisconnect!.IsOnline);
        Assert.False(firstDisconnect.StateChanged);
        Assert.NotNull(finalDisconnect);
        Assert.False(finalDisconnect!.IsOnline);
        Assert.True(finalDisconnect.StateChanged);
        Assert.Null(missingDisconnect);
        Assert.False(service.IsEmployeeOnline(7));
    }

    [Fact]
    public void EmployeePresenceService_GetOnlineStates_FiltersInvalidIdsAndDeduplicates()
    {
        var service = new EmployeePresenceService();
        service.ConnectEmployee(2, "connection-a", DateTimeOffset.UtcNow);

        var states = service.GetOnlineStates([2, 2, 3, -1, 0]);

        Assert.Equal(2, states.Count);
        Assert.True(states[2]);
        Assert.False(states[3]);
    }

    [Fact]
    public void EmployeePresenceService_RejectsInvalidConnections()
    {
        var service = new EmployeePresenceService();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.ConnectEmployee(0, "connection-a", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            service.ConnectEmployee(1, " ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ManagerPresenceService_TracksMultipleConnectionsAndDisconnectTransitions()
    {
        var service = new ManagerPresenceService();
        var now = DateTimeOffset.UtcNow;

        var firstConnection = service.ConnectManager(7, "connection-a", now);
        var secondConnection = service.ConnectManager(7, "connection-b", now.AddSeconds(1));
        var duplicateConnection = service.ConnectManager(99, "connection-a", now.AddSeconds(2));
        var firstDisconnect = service.DisconnectConnection("connection-a", now.AddSeconds(3));
        var finalDisconnect = service.DisconnectConnection("connection-b", now.AddSeconds(4));
        var missingDisconnect = service.DisconnectConnection("missing", now.AddSeconds(5));

        Assert.False(service.IsManagerOnline(0));
        Assert.Equal(7, firstConnection.ManagerId);
        Assert.True(firstConnection.IsOnline);
        Assert.True(firstConnection.StateChanged);
        Assert.False(secondConnection.StateChanged);
        Assert.Equal(7, duplicateConnection.ManagerId);
        Assert.True(duplicateConnection.IsOnline);
        Assert.False(duplicateConnection.StateChanged);
        Assert.NotNull(firstDisconnect);
        Assert.True(firstDisconnect!.IsOnline);
        Assert.False(firstDisconnect.StateChanged);
        Assert.NotNull(finalDisconnect);
        Assert.False(finalDisconnect!.IsOnline);
        Assert.True(finalDisconnect.StateChanged);
        Assert.Null(missingDisconnect);
        Assert.False(service.IsManagerOnline(7));
    }

    [Fact]
    public void ManagerPresenceService_GetOnlineStates_FiltersInvalidIdsAndDeduplicates()
    {
        var service = new ManagerPresenceService();
        service.ConnectManager(2, "connection-a", DateTimeOffset.UtcNow);

        var states = service.GetOnlineStates([2, 2, 3, -1, 0]);

        Assert.Equal(2, states.Count);
        Assert.True(states[2]);
        Assert.False(states[3]);
    }

    [Fact]
    public void ScheduleEditLockService_TracksLocksAcrossMultipleConnections()
    {
        var service = new ManagerEditLockService();
        var target = ManagerEditLockTargets.Schedule(3, 9);

        var firstAcquire = service.SetLocks("connection-a", 1, "Zoe", [target]);
        var repeatedAcquire = service.SetLocks("connection-a", 1, "Zoe", [target]);
        var secondAcquire = service.SetLocks("connection-b", 2, "Amy", [target]);
        var activeLock = Assert.Single(service.GetActiveLocks());
        var isLockedByAnother = service.IsLockedByAnotherManager(target, 2);
        var firstRelease = service.ReleaseConnection("connection-a");

        var firstState = Assert.Single(firstAcquire.ChangedStates);
        Assert.True(firstState.IsLocked);
        Assert.Equal("Zoe", firstState.LockedBy);
        Assert.Empty(repeatedAcquire.ChangedStates);
        Assert.Empty(secondAcquire.ChangedStates);
        Assert.Single(secondAcquire.RequestedStates);
        Assert.Equal("Zoe", secondAcquire.RequestedStates[0].LockedBy);
        Assert.True(isLockedByAnother);
        Assert.Equal("Zoe", activeLock.LockedBy);

        var releasedState = Assert.Single(firstRelease);
        Assert.False(releasedState.IsLocked);
        Assert.Null(releasedState.LockedBy);
        Assert.False(service.GetLockState(target)!.IsLocked);
    }

    [Fact]
    public void ScheduleEditLockService_FiltersInvalidLocksAndReplacesConnectionLockSet()
    {
        var service = new ManagerEditLockService();
        var firstTarget = ManagerEditLockTargets.Schedule(1, 2);
        var secondTarget = ManagerEditLockTargets.Schedule(2, 3);

        var firstChanges = service.SetLocks(
            "connection-a",
            1,
            "Manager",
            [new ManagerEditLockTarget(ManagerEditResourceTypes.Schedule, "bad", 0, 2), new ManagerEditLockTarget(ManagerEditResourceTypes.Schedule, "bad", 1, 0), firstTarget]);
        var replacementChanges = service.SetLocks("connection-a", 1, "Manager", [secondTarget]);
        var activeLocks = service.GetActiveLocks();

        Assert.Single(firstChanges.ChangedStates);
        Assert.Null(service.GetLockState(new ManagerEditLockTarget(ManagerEditResourceTypes.Schedule, "bad", 0, 2)));
        Assert.Null(service.GetLockState(new ManagerEditLockTarget(ManagerEditResourceTypes.Schedule, "bad", 1, 0)));
        Assert.False(service.GetLockState(firstTarget)!.IsLocked);
        Assert.True(service.GetLockState(secondTarget)!.IsLocked);
        Assert.Equal(2, replacementChanges.ChangedStates.Count);
        Assert.Single(activeLocks);
        Assert.Equal(2, activeLocks[0].ContainerId);
        Assert.Equal(3, activeLocks[0].GraphId);
    }
}
