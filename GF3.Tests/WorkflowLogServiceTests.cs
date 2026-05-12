using System.Security.Claims;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Realtime;
using WebApi.Services;

namespace GF3.Tests;

public sealed class WorkflowLogServiceTests
{
    [Fact]
    public async Task LogAsync_WithClaims_PersistsTrimmedEntryAndNotifiesRealtime()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var notifier = new RecordingRealtimeNotifier();
        var service = new WorkflowLogService(context, notifier);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, AuthRoles.Manager),
                new Claim("display_name", "  Schedule Lead  "),
            ],
            "test"));

        var entry = await service.LogAsync(user, "  Saved schedule matrix.  ");
        var storedEntry = await context.WorkflowLogEntries.AsNoTracking().SingleAsync();

        Assert.Equal(storedEntry.Id, entry.Id);
        Assert.Equal(AuthRoles.Manager, storedEntry.ActorRole);
        Assert.Equal("Schedule Lead", storedEntry.ActorName);
        Assert.Equal("Saved schedule matrix.", storedEntry.Action);
        Assert.Null(storedEntry.ActorEmployeeId);
        Assert.Single(notifier.WorkflowLogEntries);
        Assert.Equal(storedEntry.Id, notifier.WorkflowLogEntries[0].Id);
    }

    [Fact]
    public async Task LogAsync_EmployeeClaims_CapturesEmployeeIdAndNormalizesRole()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new WorkflowLogService(context, new RecordingRealtimeNotifier());
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "EMPLOYEE"),
                new Claim("employee_id", "42"),
                new Claim(ClaimTypes.Name, "worker.login"),
            ],
            "test"));

        var entry = await service.LogAsync(user, "Accepted open shift.");

        Assert.Equal(AuthRoles.Employee, entry.ActorRole);
        Assert.Equal("worker.login", entry.ActorName);
        Assert.Equal(42, entry.ActorEmployeeId);
    }

    [Fact]
    public async Task GetRecentAsync_OrdersNewestFirstAndClampsLimit()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.WorkflowLogEntries.AddRange(
            CreateEntry(1, DateTimeOffset.Parse("2026-05-08T09:00:00Z")),
            CreateEntry(2, DateTimeOffset.Parse("2026-05-10T09:00:00Z")),
            CreateEntry(3, DateTimeOffset.Parse("2026-05-09T09:00:00Z")));
        await context.SaveChangesAsync();
        var service = new WorkflowLogService(context, new RecordingRealtimeNotifier());

        var latestTwo = await service.GetRecentAsync(2);
        var clampedToOne = await service.GetRecentAsync(0);

        Assert.Equal([2, 3], latestTwo.Select(entry => entry.Id).ToArray());
        Assert.Single(clampedToOne);
        Assert.Equal(2, clampedToOne[0].Id);
    }

    private static WorkflowLogEntryModel CreateEntry(int id, DateTimeOffset occurredAtUtc) => new()
    {
        Id = id,
        OccurredAtUtc = occurredAtUtc,
        ActorRole = AuthRoles.Manager,
        ActorName = $"Manager {id}",
        Action = $"Action {id}",
    };

    private sealed class RecordingRealtimeNotifier : IRealtimeNotifier
    {
        public List<WorkflowLogEntryModel> WorkflowLogEntries { get; } = [];

        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason)
            => Task.CompletedTask;

        public Task NotifyManagerDataChangedAsync(
            string resourceType,
            string? resourceId,
            string reason,
            int? containerId = null,
            int? graphId = null)
            => Task.CompletedTask;

        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
            => Task.CompletedTask;

        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)
        {
            WorkflowLogEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state)
            => Task.CompletedTask;

        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state)
            => Task.CompletedTask;
    }
}
