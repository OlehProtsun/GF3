using System.Security.Claims;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.WorkflowLogs;
using WebApi.Controllers;
using WebApi.Services;

namespace GF3.Tests;

public sealed class WorkflowLogsControllerTests
{
    [Fact]
    public async Task GetRecent_MapsAllEntries()
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

        var result = await controller.GetRecent(CancellationToken.None);

        var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<WorkflowLogDto>>(
            Assert.IsType<OkObjectResult>(result.Result).Value));
        Assert.Equal(7, dto.Id);
        Assert.Equal(occurredAtUtc, dto.OccurredAtUtc);
        Assert.Equal("Employee", dto.ActorRole);
        Assert.Equal(12, dto.ActorEmployeeId);
        Assert.Equal("Worker Bee", dto.ActorName);
        Assert.Equal("Updated availability.", dto.Action);
    }

    [Fact]
    public async Task SettingsAndBulkDelete_MapManagementRequests()
    {
        var service = new RecordingWorkflowLogService { DeletedCount = 14 };
        var controller = new WorkflowLogsController(service);

        var settingsResult = await controller.UpdateSettings(
            new UpdateWorkflowLogSettingsRequest { IsEnabled = false, Audience = "employees" },
            CancellationToken.None);
        var bulkResult = await controller.BulkDelete(
            new DeleteWorkflowLogsRequest
            {
                FromUtc = DateTimeOffset.Parse("2026-05-10T00:00:00Z"),
                ToUtc = DateTimeOffset.Parse("2026-05-13T00:00:00Z"),
            },
            CancellationToken.None);

        var settings = Assert.IsType<WorkflowLogSettingsDto>(Assert.IsType<OkObjectResult>(settingsResult.Result).Value);
        var deleted = Assert.IsType<WorkflowLogDeleteResultDto>(Assert.IsType<OkObjectResult>(bulkResult.Result).Value);
        Assert.False(settings.IsEnabled);
        Assert.Equal("employees", settings.Audience);
        Assert.Equal(14, deleted.DeletedCount);
        Assert.Equal(DateTimeOffset.Parse("2026-05-10T00:00:00Z"), service.LastFromUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-05-13T00:00:00Z"), service.LastToUtc);
    }

    private sealed class RecordingWorkflowLogService : IWorkflowLogService
    {
        public IReadOnlyList<WorkflowLogEntryModel> Entries { get; init; } = [];
        public int DeletedCount { get; init; }
        public DateTimeOffset? LastFromUtc { get; private set; }
        public DateTimeOffset? LastToUtc { get; private set; }
        public WorkflowLogSettingsModel Settings { get; private set; } = new();

        public Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entries);
        }

        public Task<WorkflowLogSettingsModel> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Settings);

        public Task<WorkflowLogSettingsModel> UpdateSettingsAsync(
            bool isEnabled,
            string audience,
            CancellationToken cancellationToken = default)
        {
            Settings = new WorkflowLogSettingsModel { IsEnabled = isEnabled, Audience = audience };
            return Task.FromResult(Settings);
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<int> DeleteRangeAsync(
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            LastFromUtc = fromUtc;
            LastToUtc = toUtc;
            return Task.FromResult(DeletedCount);
        }

        public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(DeletedCount);

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
