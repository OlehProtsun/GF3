using System.Security.Claims;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.EmployeeSchedules;

namespace WebApi.Controllers;

/// <summary>
/// Employee self-service endpoints for published schedules.
/// </summary>
[ApiController]
[Route("api/employee-schedules")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeSchedulesController(IContainerService containerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmployeeScheduleDto>>> GetVisible(CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var schedules = await containerService
            .GetPublishedGraphsForEmployeeAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(schedules.Select(ToApiDto));
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

    private static EmployeeScheduleDto ToApiDto(ScheduleModel model)
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
        };
    }

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
