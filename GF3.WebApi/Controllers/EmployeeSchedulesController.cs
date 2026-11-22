using System.Security.Claims;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.EmployeeSchedules;
using WebApi.Services;
using StoredSchedule = DataAccessLayer.Models.ScheduleModel;
using StoredPublicationStatus = DataAccessLayer.Models.Enums.SchedulePublicationStatus;

namespace WebApi.Controllers;

/// <summary>
/// Employee self-service endpoints for published schedules.
/// </summary>
[ApiController]
[Route("api/employee-schedules")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeSchedulesController(
    IContainerService containerService,
    IScheduleLastUpdateService? scheduleLastUpdateService = null) : ControllerBase
{
    [HttpGet("public")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IReadOnlyList<PublicScheduleSummaryDto>>> GetPublicSchedules(
        [FromServices] AppDbContext db, CancellationToken cancellationToken)
    {
        var schedules = await GetBrowsableSchedules(db, GetRequiredEmployeeId())
            .OrderByDescending(schedule => schedule.Year)
            .ThenByDescending(schedule => schedule.Month)
            .ThenBy(schedule => schedule.Name)
            .ThenBy(schedule => schedule.Id)
            .Select(schedule => new { schedule.Id, schedule.Name, schedule.Year, schedule.Month, schedule.PublishedAtUtc })
            .ToListAsync(cancellationToken);
        var updates = scheduleLastUpdateService is null
            ? new Dictionary<int, DateTimeOffset>()
            : await scheduleLastUpdateService.GetLastUpdatesAsync(schedules.Select(schedule => schedule.Id), cancellationToken);

        return Ok(schedules.Select(schedule => new PublicScheduleSummaryDto(
            schedule.Id, schedule.Name, schedule.Year, schedule.Month, "public",
            updates.TryGetValue(schedule.Id, out var updated) ? updated : schedule.PublishedAtUtc)).ToList());
    }

    [HttpGet("public/{scheduleId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<EmployeeScheduleDto>> GetPublicSchedule(
        int scheduleId, [FromServices] AppDbContext db, CancellationToken cancellationToken)
    {
        var schedule = await GetBrowsableSchedules(db, GetRequiredEmployeeId())
            .Where(item => item.Id == scheduleId)
            .AsSingleQuery()
            .Select(item => new EmployeeScheduleDto
            {
                Id = item.Id,
                Name = item.Name,
                ContainerId = item.ContainerId,
                ShopId = item.ShopId,
                Year = item.Year,
                Month = item.Month,
                PublicationStatus = "public",
                PublishedAtUtc = item.PublishedAtUtc,
                Note = item.Note ?? string.Empty,
                AllowSwap = false,
                Employees = item.Employees.OrderBy(employee => employee.DisplayOrder).ThenBy(employee => employee.EmployeeId)
                    .Select(employee => new EmployeeScheduleEmployeeDto
                    {
                        Id = employee.Id,
                        EmployeeId = employee.EmployeeId,
                        FirstName = employee.Employee.FirstName,
                        LastName = employee.Employee.LastName,
                        DisplayOrder = employee.DisplayOrder,
                    }).ToList(),
                Slots = item.Slots.Where(slot => slot.EmployeeId > 0)
                    .OrderBy(slot => slot.DayOfMonth).ThenBy(slot => slot.SlotNo)
                    .Select(slot => new EmployeeScheduleSlotDto
                    {
                        Id = slot.Id,
                        EmployeeId = slot.EmployeeId,
                        DayOfMonth = slot.DayOfMonth,
                        SlotNo = slot.SlotNo,
                        FromTime = slot.FromTime,
                        ToTime = slot.ToTime,
                        Status = slot.Status.ToString(),
                    }).ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (schedule is null) return NotFound();

        var previewSchedule = new ScheduleModel
        {
            Id = schedule.Id,
            ContainerId = schedule.ContainerId,
            Year = schedule.Year,
            Month = schedule.Month,
            Employees = schedule.Employees.Select(employee => new ScheduleEmployeeModel
            {
                EmployeeId = employee.EmployeeId,
            }).ToList(),
        };
        var relatedPublishedSchedules = await GetRelatedPublishedSchedulesAsync([previewSchedule], cancellationToken);
        schedule.RelatedScheduleAssignments = BuildRelatedScheduleAssignments(previewSchedule, relatedPublishedSchedules);

        schedule.LastUpdatedAtUtc = schedule.PublishedAtUtc;
        if (scheduleLastUpdateService is not null)
        {
            var updates = await scheduleLastUpdateService.GetLastUpdatesAsync([schedule.Id], cancellationToken);
            if (updates.TryGetValue(schedule.Id, out var updated)) schedule.LastUpdatedAtUtc = updated;
        }

        return Ok(schedule);
    }

    // A published assignment grants related schedule visibility for its container and period.
    // Shops represented by the other published schedules in that scope need no separate assignment.
    internal static IQueryable<StoredSchedule> GetBrowsableSchedules(AppDbContext db, int employeeId)
        => db.Schedules.AsNoTracking().Where(schedule =>
            schedule.PublicationStatus == StoredPublicationStatus.Public &&
            !schedule.Employees.Any(employee => employee.EmployeeId == employeeId) &&
            db.Schedules.Any(assigned =>
                assigned.PublicationStatus == StoredPublicationStatus.Public &&
                assigned.ContainerId == schedule.ContainerId &&
                assigned.Year == schedule.Year &&
                assigned.Month == schedule.Month &&
                assigned.Employees.Any(employee => employee.EmployeeId == employeeId && !employee.Employee.IsDeleted)));

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmployeeScheduleDto>>> GetVisible(CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var schedules = await containerService
            .GetPublishedGraphsForEmployeeAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);
        var lastUpdates = scheduleLastUpdateService is null
            ? new Dictionary<int, DateTimeOffset>()
            : await scheduleLastUpdateService
                .GetLastUpdatesAsync(schedules.Select(schedule => schedule.Id), cancellationToken)
                .ConfigureAwait(false);

        var relatedPublishedSchedules = await GetRelatedPublishedSchedulesAsync(schedules, cancellationToken)
            .ConfigureAwait(false);

        return Ok(schedules.Select(schedule => ToApiDto(
            schedule,
            lastUpdates.GetValueOrDefault(schedule.Id),
            relatedPublishedSchedules)));
    }

    private async Task<IReadOnlyList<ScheduleModel>> GetRelatedPublishedSchedulesAsync(
        IReadOnlyCollection<ScheduleModel> visibleSchedules,
        CancellationToken cancellationToken)
    {
        if (visibleSchedules.Count == 0)
        {
            return [];
        }

        var visiblePeriodsByContainer = visibleSchedules
            .GroupBy(schedule => schedule.ContainerId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(schedule => (schedule.Year, schedule.Month)).ToHashSet());
        var summaryGroups = new List<List<ScheduleModel>>();
        foreach (var entry in visiblePeriodsByContainer)
        {
            var graphs = await containerService.GetGraphsAsync(entry.Key, cancellationToken).ConfigureAwait(false) ?? [];
            summaryGroups.Add(graphs
                .Where(graph =>
                    graph.PublicationStatus == SchedulePublicationStatus.Public &&
                    entry.Value.Contains((graph.Year, graph.Month)))
                .ToList());
        }
        var visibleKeys = visibleSchedules
            .Select(schedule => (schedule.ContainerId, schedule.Id))
            .ToHashSet();
        var relatedSummaries = summaryGroups
            .SelectMany(group => group)
            .GroupBy(schedule => (schedule.ContainerId, schedule.Id))
            .Select(group => group.First())
            .Where(schedule => !visibleKeys.Contains((schedule.ContainerId, schedule.Id)))
            .ToList();
        foreach (var schedule in relatedSummaries)
        {
            schedule.Slots = await containerService
                .GetGraphSlotsAsync(schedule.ContainerId, schedule.Id, cancellationToken)
                .ConfigureAwait(false) ?? [];

        }

        return visibleSchedules
            .Concat(relatedSummaries)
            .ToList();
    }
    private int GetRequiredEmployeeId()
    {
        var employeeIdValue = User.FindFirstValue("employee_id");
        if (!int.TryParse(employeeIdValue, out var employeeId) || employeeId <= 0)
        {
            throw new BadHttpRequestException("The current employee session is invalid.");
        }

        return employeeId;
    }

    private static EmployeeScheduleDto ToApiDto(
        ScheduleModel model,
        DateTimeOffset? lastUpdatedAtUtc,
        IReadOnlyCollection<ScheduleModel> relatedPublishedSchedules)
    {
        var displayOrderByEmployeeId = model.Employees
            .GroupBy(employee => employee.EmployeeId)
            .ToDictionary(group => group.Key, group => group.Min(employee => employee.DisplayOrder));

        return new()
        {
            Id = model.Id,
            ContainerId = model.ContainerId,
            ContainerName = model.Container?.Name ?? string.Empty,
            ShopId = model.ShopId,
            ShopName = model.Shop?.Name ?? string.Empty,
            Name = model.Name,
            Note = model.Note ?? string.Empty,
            Year = model.Year,
            Month = model.Month,
            PublicationStatus = model.PublicationStatus == SchedulePublicationStatus.Public ? "public" : "private",
            AllowSwap = model.AllowSwap,
            LastUpdatedAtUtc = lastUpdatedAtUtc,
            PublishedAtUtc = model.PublishedAtUtc,
            Employees = model.Employees
                .OrderBy(employee => employee.DisplayOrder)
                .ThenBy(employee => employee.Employee?.FirstName)
                .ThenBy(employee => employee.Employee?.LastName)
                .ThenBy(employee => employee.EmployeeId)
                .Select(ToEmployeeDto)
                .ToList(),
            Slots = model.Slots
                .Where(slot => slot.EmployeeId is > 0)
                .OrderBy(slot => slot.DayOfMonth)
                .ThenBy(slot => GetEmployeeDisplayOrder(slot, displayOrderByEmployeeId))
                .ThenBy(slot => slot.SlotNo)
                .Select(ToSlotDto)
                .ToList(),
            RelatedScheduleAssignments = BuildRelatedScheduleAssignments(model, relatedPublishedSchedules),
        };
    }

    private static IReadOnlyList<EmployeeScheduleRelatedAssignmentDto> BuildRelatedScheduleAssignments(
        ScheduleModel currentSchedule,
        IReadOnlyCollection<ScheduleModel> relatedPublishedSchedules)
    {
        var currentEmployeeIds = currentSchedule.Employees
            .Where(employee => employee.EmployeeId > 0)
            .Select(employee => employee.EmployeeId)
            .ToHashSet();

        return relatedPublishedSchedules
            .Where(schedule =>
                schedule.Id != currentSchedule.Id &&
                schedule.ContainerId == currentSchedule.ContainerId &&
                schedule.Year == currentSchedule.Year &&
                schedule.Month == currentSchedule.Month &&
                schedule.PublicationStatus == SchedulePublicationStatus.Public)
            .SelectMany(schedule => schedule.Slots
                .Where(slot =>
                    slot.EmployeeId is > 0 &&
                    currentEmployeeIds.Contains(slot.EmployeeId.Value))
                .Select(slot => new { Schedule = schedule, Slot = slot }))
            .GroupBy(item => new
            {
                EmployeeId = item.Slot.EmployeeId!.Value,
                item.Slot.DayOfMonth,
                ScheduleId = item.Schedule.Id,
            })
            .Select(group => new EmployeeScheduleRelatedAssignmentDto
            {
                EmployeeId = group.Key.EmployeeId,
                DayOfMonth = group.Key.DayOfMonth,
                ScheduleId = group.Key.ScheduleId,
                ScheduleName = GetScheduleName(group.First().Schedule),
            })
            .OrderBy(assignment => assignment.EmployeeId)
            .ThenBy(assignment => assignment.DayOfMonth)
            .ThenBy(assignment => assignment.ScheduleName)
            .ThenBy(assignment => assignment.ScheduleId)
            .ToList();
    }

    private static string GetScheduleName(ScheduleModel schedule)
        => string.IsNullOrWhiteSpace(schedule.Name)
            ? $"Schedule #{schedule.Id}"
            : schedule.Name.Trim();
    private static EmployeeScheduleSlotDto ToSlotDto(ScheduleSlotModel model) => new()
    {
        Id = model.Id,
        DayOfMonth = model.DayOfMonth,
        SlotNo = model.SlotNo,
        EmployeeId = model.EmployeeId,
        FromTime = model.FromTime,
        ToTime = model.ToTime,
        Status = model.Status.ToString(),
    };

    private static EmployeeScheduleEmployeeDto ToEmployeeDto(ScheduleEmployeeModel model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        FirstName = model.Employee?.FirstName ?? string.Empty,
        LastName = model.Employee?.LastName ?? string.Empty,
        DisplayName = GetEmployeeDisplayName(model),
        MinHoursMonth = model.MinHoursMonth,
        DisplayOrder = model.DisplayOrder,
    };

    private static int GetEmployeeDisplayOrder(
        ScheduleSlotModel model,
        IReadOnlyDictionary<int, int> displayOrderByEmployeeId)
        => model.EmployeeId is { } employeeId && displayOrderByEmployeeId.TryGetValue(employeeId, out var displayOrder)
            ? displayOrder
            : int.MaxValue;

    private static string GetEmployeeDisplayName(ScheduleEmployeeModel model)
    {
        var fullName = $"{model.Employee?.FirstName} {model.Employee?.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? $"Employee #{model.EmployeeId}" : fullName;
    }
}
