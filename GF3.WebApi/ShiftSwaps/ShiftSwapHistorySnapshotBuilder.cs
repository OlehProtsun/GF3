using System.Text.Json;
using DataAccessLayer.Models;
using WebApi.Contracts.ShiftSwaps;

namespace WebApi.ShiftSwaps;

internal static class ShiftSwapHistorySnapshotBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string BuildJson(
        ScheduleModel schedule,
        IEnumerable<ScheduleSlotModel> slots,
        ShiftSwapRequestModel swap,
        int acceptedEmployeeId,
        string acceptedEmployeeName)
    {
        var snapshot = new ShiftSwapScheduleSnapshotDto();

        if (swap.IsManagerCreated)
        {
            var manualColumnId = swap.ManualColumnId ?? 0;
            snapshot.Rows.Add(new ShiftSwapScheduleSnapshotRowDto
            {
                EmployeeId = manualColumnId > 0 ? -manualColumnId : -1,
                EmployeeName = GraphManualColumnLabelResolver.Resolve(schedule.Note, swap.ManualColumnId),
                Kind = "manual",
                DayValues = GraphManualColumnLabelResolver.ResolveCells(schedule.Note, swap.ManualColumnId),
            });
        }
        else if (swap.FromEmployeeId.HasValue)
        {
            snapshot.Rows.Add(new ShiftSwapScheduleSnapshotRowDto
            {
                EmployeeId = swap.FromEmployeeId.Value,
                EmployeeName = GetEmployeeName(swap.FromEmployee, swap.FromEmployeeId.Value),
                DayValues = BuildEmployeeDayValues(slots, swap.FromEmployeeId.Value),
            });
        }

        snapshot.Rows.Add(new ShiftSwapScheduleSnapshotRowDto
        {
            EmployeeId = acceptedEmployeeId,
            EmployeeName = acceptedEmployeeName,
            DayValues = BuildEmployeeDayValues(slots, acceptedEmployeeId),
        });

        return JsonSerializer.Serialize(snapshot, SerializerOptions);
    }

    public static ShiftSwapScheduleSnapshotDto Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ShiftSwapScheduleSnapshotDto();
        }

        try
        {
            return JsonSerializer.Deserialize<ShiftSwapScheduleSnapshotDto>(value, SerializerOptions)
                ?? new ShiftSwapScheduleSnapshotDto();
        }
        catch (JsonException)
        {
            return new ShiftSwapScheduleSnapshotDto();
        }
    }

    private static Dictionary<int, string> BuildEmployeeDayValues(IEnumerable<ScheduleSlotModel> slots, int employeeId)
        => slots
            .Where(slot => slot.EmployeeId == employeeId)
            .GroupBy(slot => slot.DayOfMonth)
            .ToDictionary(
                group => group.Key,
                group => string.Join(
                    ", ",
                    group
                        .OrderBy(slot => slot.FromTime, StringComparer.Ordinal)
                        .ThenBy(slot => slot.ToTime, StringComparer.Ordinal)
                        .Select(slot => $"{slot.FromTime} - {slot.ToTime}")));

    private static string GetEmployeeName(EmployeeModel? employee, int employeeId)
    {
        var fullName = $"{employee?.FirstName} {employee?.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? $"Employee #{employeeId}" : fullName;
    }
}
