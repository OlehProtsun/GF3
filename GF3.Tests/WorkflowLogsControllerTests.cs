using System.Security.Claims;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.WorkflowLogs;
using WebApi.Controllers;
using WebApi.Services;

namespace GF3.Tests;

public sealed class WorkflowLogsControllerTests
{
    [Fact]
    public async Task GetRecent_MapsEntriesAndPassesLimit()
    {
        var occurredAtUtc = new DateTimeOffset(2026, 5, 13, 15, 0, 0, TimeSpan.Zero);
        var service = new RecordingWorkflowLogService
        {
            Entries =
            [
                new WorkflowLogEntryModel
                {
                    Id = 7,
                    OccurredAtUtc = occurredAtUtc,
                    ActorRole = "Employee",
                    ActorEmployeeId = 12,
                    ActorName = "Worker Bee",
                    Action = "Updated availability.",
                },
            ],
        };
        var controller = new WorkflowLogsController(service);

        var result = await controller.GetRecent(limit: 25, CancellationToken.None);

        var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<WorkflowLogDto>>(
            Assert.IsType<OkObjectResult>(result.Result).Value));
        Assert.Equal(25, service.LastLimit);
        Assert.Equal(7, dto.Id);
        Assert.Equal(occurredAtUtc, dto.OccurredAtUtc);
        Assert.Equal("Employee", dto.ActorRole);
        Assert.Equal(12, dto.ActorEmployeeId);
        Assert.Equal("Worker Bee", dto.ActorName);
        Assert.Equal("Updated availability.", dto.Action);
    }

    private sealed class RecordingWorkflowLogService : IWorkflowLogService
    {
        public IReadOnlyList<WorkflowLogEntryModel> Entries { get; init; } = [];
        public int? LastLimit { get; private set; }

        public Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
        {
            LastLimit = limit;
            return Task.FromResult(Entries);
        }

        public Task<WorkflowLogEntryModel> LogAsync(ClaimsPrincipal user, string action, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WorkflowLogEntryModel> LogAsync(
            string actorRole,
            string actorName,
            int? actorEmployeeId,
            string action,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
