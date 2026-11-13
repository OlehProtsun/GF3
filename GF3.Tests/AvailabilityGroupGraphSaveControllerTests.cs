using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Enums;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class AvailabilityGroupGraphSaveControllerTests
{
    private const string Route = "/api/availability-groups/save-graph";

    [Fact]
    public async Task SaveGraph_UsesManagerAuthorizationValidationAndExistingTargetAndSourceLocks()
    {
        var realtime = new RecordingRealtime();
        await using var host = await CreateHostAsync(realtime);
        var request = Request();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsJsonAsync(Route, request)).StatusCode);
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync(Route, request)).StatusCode);
        host.Bearer(await host.SignedToken("phone"));
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync(Route, request)).StatusCode);
        host.Bearer(await host.SignedToken("pc"));

        var invalid = await host.Client.PostAsJsonAsync(Route, new
        {
            name = "Invalid snapshot", year = 2026, month = 12,
            members = new[] { new { employeeId = 1, days = new[] { new { dayOfMonth = 1, kind = 99 } } } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
        var incomplete = Request();
        incomplete.Members[0].Days.RemoveAt(30);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync(Route, incomplete)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsJsonAsync(Route, Request(id: 999))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsJsonAsync(Route, Request(sourceId: 999))).StatusCode);

        var locks = host.App.Services.GetRequiredService<IManagerEditLockService>();
        locks.SetLocks("other", 42, "Other manager", [ManagerEditLockTargets.AvailabilityGroup(1)]);
        var targetConflict = await host.Client.PostAsJsonAsync(Route, Request(id: 1));
        var sourceConflict = await host.Client.PostAsJsonAsync(Route, Request(sourceId: 1));
        foreach (var conflict in new[] { targetConflict, sourceConflict })
        {
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("edit_lock_conflict", (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString());
        }
        locks.SetLocks("other", 42, "Other manager", [ManagerEditLockTargets.AvailabilityGroup(999)]);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsJsonAsync(Route, Request(sourceId: 999))).StatusCode);

        Assert.Empty(realtime.Changes);
        await using var db = host.Database.CreateContext();
        Assert.Single(await db.AvailabilityGroups.ToListAsync());
        Assert.Empty(await db.AvailabilityGroupDays.ToListAsync());
        Assert.DoesNotContain(await db.WorkflowLogEntries.ToListAsync(), entry => entry.Action.Contains("availability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveGraph_ReturnsCommittedIdsAndDoesNotFailWhenAncillaryServicesThrow()
    {
        var realtime = new RecordingRealtime();
        await using var host = await CreateHostAsync(realtime);
        host.Bearer(await host.SignedToken("pc"));
        await host.Database.SeedAsync(async db =>
        {
            var group = await db.AvailabilityGroups.SingleAsync();
            group.Month = 12;
            db.AvailabilityGroupDays.Add(new AvailabilityGroupDayModel
            {
                AvailabilityGroupMember = new AvailabilityGroupMemberModel { AvailabilityGroup = group, EmployeeId = 1 },
                DayOfMonth = 3,
                Kind = DataAccessLayer.Models.Enums.AvailabilityKind.ANY,
            });
        });

        realtime.ThrowOnWorkflowLog = true;
        realtime.ThrowForGroupId = "2";
        realtime.BeforeNotification = async () =>
        {
            await using var db = host.Database.CreateContext();
            Assert.Equal(31, await db.AvailabilityGroupDays.CountAsync(day => day.AvailabilityGroupMember.AvailabilityGroupId == 2));
            Assert.Single(await db.AvailabilityGroupDayTransfers.ToListAsync());
        };

        var create = await host.Client.PostAsJsonAsync(Route, Request(sourceId: 1, name: "Published snapshot", publish: true));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var json = await create.Content.ReadFromJsonAsync<JsonElement>();
        var saved = json.Deserialize<SaveAvailabilityGroupGraphResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(2, saved.Id);
        Assert.True(saved.Changed);
        Assert.Equal([1], saved.SourceGroupIds);
        Assert.Equal(["changed", "id", "sourceGroupIds"], json.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal([
            ("2", "manager-availability-published"), ("1", "manager-availability-days-transferred"),
        ], realtime.Changes);
        Assert.Single(realtime.WorkflowActions);
        Assert.StartsWith("Published availability", realtime.WorkflowActions[0]);

        realtime.ThrowOnWorkflowLog = false;
        realtime.ThrowForGroupId = null;
        var update = await host.Client.PostAsJsonAsync(Route, Request(id: saved.Id, name: "Edited snapshot", publish: true));
        var updated = (await update.Content.ReadFromJsonAsync<SaveAvailabilityGroupGraphResponse>())!;
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(saved.Id, updated.Id);
        Assert.True(updated.Changed);
        Assert.Empty(updated.SourceGroupIds);
        Assert.Equal(("2", "manager-availability-updated"), realtime.Changes[^1]);
        Assert.Equal(3, realtime.Changes.Count);
        Assert.Equal(2, realtime.WorkflowActions.Count);

        var noOp = await host.Client.PostAsJsonAsync(Route, Request(id: saved.Id, name: "Edited snapshot", publish: true));
        Assert.Equal(HttpStatusCode.OK, noOp.StatusCode);
        Assert.False((await noOp.Content.ReadFromJsonAsync<SaveAvailabilityGroupGraphResponse>())!.Changed);
        Assert.Equal(3, realtime.Changes.Count);
        Assert.Equal(2, realtime.WorkflowActions.Count);

        await using var verification = host.Database.CreateContext();
        Assert.Equal("Edited snapshot", (await verification.AvailabilityGroups.SingleAsync(group => group.Id == saved.Id)).Name);
        Assert.Equal(2, await verification.WorkflowLogEntries.CountAsync());
        Assert.Equal(3, realtime.CompletedCommitChecks);
    }

    private static Task<ManagerWorkspaceTestHost> CreateHostAsync(RecordingRealtime realtime)
        => ManagerWorkspaceTestHost.CreateAsync(configureServices: services =>
            services.Replace(ServiceDescriptor.Singleton<IRealtimeNotifier>(realtime)));

    private static SaveAvailabilityGroupGraphRequest Request(int? id = null, int? sourceId = null, string name = "Snapshot", bool publish = false)
        => new()
        {
            Id = id,
            Name = name,
            Year = 2026,
            Month = 12,
            PublicationStatus = publish ? "public" : "private",
            VisibleFromUtc = publish ? new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero) : null,
            VisibleToUtc = publish ? new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.Zero) : null,
            Members = [new()
            {
                EmployeeId = 1,
                Days = Enumerable.Range(1, 31).Select(day => new SaveAvailabilityGroupGraphDayRequest
                {
                    DayOfMonth = day,
                    Kind = day == 3 ? AvailabilityKind.ANY : AvailabilityKind.NONE,
                }).ToList(),
            }],
            Transfers = sourceId is { } source ? [new() { EmployeeId = 1, SourceGroupId = source, DayOfMonths = [3] }] : [],
        };

    private sealed class RecordingRealtime : IRealtimeNotifier
    {
        public List<(string? Id, string Reason)> Changes { get; } = [];
        public List<string> WorkflowActions { get; } = [];
        public bool ThrowOnWorkflowLog { get; set; }
        public string? ThrowForGroupId { get; set; }
        public Func<Task>? BeforeNotification { get; set; }
        public int CompletedCommitChecks { get; private set; }

        public async Task NotifyManagerDataChangedAsync(string resourceType, string? resourceId, string reason, int? containerId = null, int? graphId = null)
        {
            if (resourceType != ManagerEditResourceTypes.AvailabilityGroup)
                return;
            if (BeforeNotification is not null)
            {
                await BeforeNotification();
                CompletedCommitChecks++;
            }

            Changes.Add((resourceId, reason));
            if (resourceId == ThrowForGroupId)
                throw new InvalidOperationException("Simulated notification failure");
        }

        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)
        {
            WorkflowActions.Add(entry.Action);
            if (ThrowOnWorkflowLog)
                throw new InvalidOperationException("Simulated workflow notification failure");
            return Task.CompletedTask;
        }

        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => Task.CompletedTask;
        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null) => Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;
    }
}
