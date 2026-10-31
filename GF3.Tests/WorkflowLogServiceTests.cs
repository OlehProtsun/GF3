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
        var user = CreateUser(AuthRoles.Manager, "  Schedule Lead  ");

        var entry = await service.LogAsync(user, "  Saved schedule matrix.  ");
        var storedEntry = await context.WorkflowLogEntries.AsNoTracking().SingleAsync();

        Assert.Equal(storedEntry.Id, entry.Id);
        Assert.Equal(AuthRoles.Manager, storedEntry.ActorRole);
        Assert.Equal("Schedule Lead", storedEntry.ActorName);
        Assert.Equal("Saved schedule matrix.", storedEntry.Action);
        Assert.Null(storedEntry.ActorEmployeeId);
        Assert.Single(notifier.WorkflowLogEntries);
    }

    [Fact]
    public async Task LogAsync_RespectsGlobalSwitchAndAudience()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var notifier = new RecordingRealtimeNotifier();
        var service = new WorkflowLogService(context, notifier);

        await service.UpdateSettingsAsync(false, WorkflowLogSettingsModel.AllAudience);
        var skippedWhileDisabled = await service.LogAsync(CreateUser(AuthRoles.Manager, "Manager"), "Disabled action.");

        await service.UpdateSettingsAsync(true, WorkflowLogSettingsModel.EmployeesAudience);
        var skippedManager = await service.LogAsync(CreateUser(AuthRoles.Manager, "Manager"), "Manager action.");
        var storedEmployee = await service.LogAsync(CreateUser(AuthRoles.Employee, "Worker", 42), "Employee action.");

        Assert.Equal(0, skippedWhileDisabled.Id);
        Assert.Equal(0, skippedManager.Id);
        Assert.True(storedEmployee.Id > 0);
        Assert.Equal(AuthRoles.Employee, storedEmployee.ActorRole);
        Assert.Equal(42, storedEmployee.ActorEmployeeId);
        Assert.Equal("Employee action.", Assert.Single(await context.WorkflowLogEntries.AsNoTracking().ToListAsync()).Action);
        Assert.Single(notifier.WorkflowLogEntries);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsAllEntriesNewestFirst()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.WorkflowLogEntries.AddRange(
            CreateEntry(1, DateTimeOffset.Parse("2026-05-08T09:00:00Z")),
            CreateEntry(2, DateTimeOffset.Parse("2026-05-10T09:00:00Z")),
            CreateEntry(3, DateTimeOffset.Parse("2026-05-09T09:00:00Z")));
        await context.SaveChangesAsync();
        var service = new WorkflowLogService(context, new RecordingRealtimeNotifier());

        var entries = await service.GetRecentAsync();

        Assert.Equal([2, 3, 1], entries.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public async Task UpdateSettingsAsync_PersistsSingletonSettings()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new WorkflowLogService(context, new RecordingRealtimeNotifier());

        await service.UpdateSettingsAsync(false, "MANAGERS");
        var settings = await service.GetSettingsAsync();

        Assert.False(settings.IsEnabled);
        Assert.Equal(WorkflowLogSettingsModel.ManagersAudience, settings.Audience);
        Assert.Equal(WorkflowLogSettingsModel.SingletonId, settings.Id);
        Assert.Single(await context.WorkflowLogSettings.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task DeleteOperations_RemoveOneRangeAndAllWithoutTouchingSettings()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.WorkflowLogEntries.AddRange(
            CreateEntry(1, DateTimeOffset.Parse("2026-05-09T09:00:00Z")),
            CreateEntry(2, DateTimeOffset.Parse("2026-05-10T09:00:00Z")),
            CreateEntry(3, DateTimeOffset.Parse("2026-05-11T09:00:00Z")),
            CreateEntry(4, DateTimeOffset.Parse("2026-05-12T09:00:00Z")));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new WorkflowLogService(context, new RecordingRealtimeNotifier());

        Assert.True(await service.DeleteAsync(1));
        Assert.False(await service.DeleteAsync(999));
        Assert.Equal(2, await service.DeleteRangeAsync(
            DateTimeOffset.Parse("2026-05-10T00:00:00Z"),
            DateTimeOffset.Parse("2026-05-12T00:00:00Z")));
        Assert.Equal(1, await service.DeleteAllAsync());

        Assert.Empty(await context.WorkflowLogEntries.AsNoTracking().ToListAsync());
        Assert.Single(await context.WorkflowLogSettings.AsNoTracking().ToListAsync());
    }

    private static ClaimsPrincipal CreateUser(string role, string name, int? employeeId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("display_name", name),
        };
        if (employeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", employeeId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
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

        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => Task.CompletedTask;

        public Task NotifyManagerDataChangedAsync(
            string resourceType,
            string? resourceId,
            string reason,
            int? containerId = null,
            int? graphId = null) => Task.CompletedTask;

        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
            => Task.CompletedTask;

        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)
        {
            WorkflowLogEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;

        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;
    }
}
