using System.Net;
using System.Net.Http.Json;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebApi.Auth;
using WebApi.Contracts.Containers.Graphs.CellStyles;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class GraphCellStyleBatchControllerTests
{
    private const string Graph = "/api/containers/1/graphs/1";
    private const string Batch = Graph + "/cell-styles/batch";

    [Fact]
    public async Task Batch_ValidatesRequests_AndLogsAndNotifiesOnlyOnce()
    {
        var notifier = new RecordingNotifier();
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(
            configureServices: services => services.Replace(ServiceDescriptor.Singleton<IRealtimeNotifier>(notifier)));
        await host.Database.SeedAsync(async db =>
        {
            db.Employees.Add(TestDataFactory.CreateDalEmployee("Second", "Employee"));
            await db.SaveChangesAsync();
            db.ScheduleEmployees.AddRange(
                new ScheduleEmployeeModel { ScheduleId = 1, EmployeeId = 1, DisplayOrder = 0 },
                new ScheduleEmployeeModel { ScheduleId = 1, EmployeeId = 2, DisplayOrder = 1 });
        });

        var patch = new { employeeId = 1, dayOfMonth = 1, changeBackgroundColor = true, backgroundColorArgb = 10 };
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PutAsJsonAsync(Batch, new { changes = new[] { patch } })).StatusCode);
        host.Bearer(await host.SignedToken(null, AuthRoles.Employee));
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync(Batch, new { changes = new[] { patch } })).StatusCode);
        host.Bearer(await host.SignedToken("pc"));
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(Batch, new { changes = new[] { patch, patch } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(Batch, new { changes = new[] { new { employeeId = 1, dayOfMonth = 32 } } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(Batch, new { changes = new[] { new { employeeId = 1, dayOfMonth = 1 } } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(Batch, new { changes = new object?[] { null } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PutAsJsonAsync(Batch.Replace("/containers/1", "/containers/999"), new { changes = new[] { patch } })).StatusCode);
        var locks = host.App.Services.GetRequiredService<IManagerEditLockService>();
        locks.SetLocks("batch-test", 999, "Other synthetic manager", [ManagerEditLockTargets.Schedule(1, 1)]);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync(Batch, new { changes = new[] { patch } })).StatusCode);
        locks.ReleaseConnection("batch-test");

        (await host.Client.GetAsync(Graph + "/versions")).EnsureSuccessStatusCode();
        (await host.Client.GetAsync(Graph + "/versions")).EnsureSuccessStatusCode();
        var changes = new[] { 1, 2 }.SelectMany(employeeId => Enumerable.Range(1, 25)
            .Select(day => new GraphCellStyleFieldPatchDto
            {
                EmployeeId = employeeId, DayOfMonth = day, ChangeBackgroundColor = true, BackgroundColorArgb = 10,
            })).ToList();
        var request = new BatchUpdateGraphCellStylesRequest { Changes = changes };
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(Batch, request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(Batch, request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(Batch, new { changes = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync(Batch, new
        {
            changes = new[] { new { employeeId = 1, dayOfMonth = 1, changeBackgroundColor = true, backgroundColorArgb = 11 } },
        })).StatusCode);

        await using var db = host.Database.CreateContext();
        Assert.Equal(50, (await host.Client.GetFromJsonAsync<GraphCellStyleDto[]>(Graph + "/cell-styles"))!.Length);
        Assert.Single(await db.WorkflowLogEntries.Where(entry => entry.Action.StartsWith("Updated cell styles")).ToListAsync());
        Assert.Equal(new[] { "manager", "schedule", "swaps" }, notifier.StyleEvents);
        Assert.Single(await db.ScheduleVersions.AsNoTracking().ToListAsync());
        (await host.Client.PostAsync(Graph + "/versions", null)).EnsureSuccessStatusCode();
        (await host.Client.GetAsync(Graph + "/versions")).EnsureSuccessStatusCode();
        var versions = await db.ScheduleVersions.AsNoTracking().OrderBy(version => version.VersionNumber).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.Equal(50, versions[1].CellStyleCount);
    }

    [Fact]
    public async Task Batch_RespectsAuthenticatedRateLimit()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        host.Bearer(await host.SignedToken("pc"));
        for (var i = 0; i < 240; i++)
            (await host.Client.GetAsync("/api/rate-probe")).EnsureSuccessStatusCode();

        var response = await host.Client.PutAsJsonAsync(Batch, new { changes = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed class RecordingNotifier : IRealtimeNotifier
    {
        public List<string> StyleEvents { get; } = [];
        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => Record(reason, "schedule");
        public Task NotifyManagerDataChangedAsync(string type, string? id, string reason, int? containerId = null, int? graphId = null)
            => Record(reason, "manager");
        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
            => Record(reason, "swaps");
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry) => Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;

        private Task Record(string reason, string stream)
        {
            if (reason == "manager-schedule-style-updated") StyleEvents.Add(stream);
            return Task.CompletedTask;
        }
    }
}
