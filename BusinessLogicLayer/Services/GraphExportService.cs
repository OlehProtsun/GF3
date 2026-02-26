using System.Globalization;
using System.Text;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Services;

public sealed class GraphExportService : IGraphExportService
{
    private readonly IContainerService _containerService;
    private readonly IAvailabilityGroupService _availabilityGroupService;

    public GraphExportService(
        IContainerService containerService,
        IAvailabilityGroupService availabilityGroupService)
    {
        _containerService = containerService;
        _availabilityGroupService = availabilityGroupService;
    }

    public async Task<byte[]> ExportGraphSqlAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
    {
        var graph = await _containerService.GetGraphByIdAsync(containerId, graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        var employees = includeEmployees
            ? await _containerService.GetGraphEmployeesAsync(containerId, graphId, ct).ConfigureAwait(false)
            : [];

        var slots = await _containerService.GetGraphSlotsAsync(containerId, graphId, ct).ConfigureAwait(false);
        var styles = includeStyles
            ? await _containerService.GetGraphCellStylesAsync(containerId, graphId, ct).ConfigureAwait(false)
            : [];

        AvailabilityGroupModel? group = null;
        List<AvailabilityGroupMemberModel> members = [];
        List<AvailabilityGroupDayModel> days = [];

        if (graph.AvailabilityGroupId is int availabilityGroupId && availabilityGroupId > 0)
        {
            var full = await _availabilityGroupService.LoadFullAsync(availabilityGroupId, ct).ConfigureAwait(false);
            group = full.group;
            members = full.members;
            days = full.days;
        }

        var script = BuildSqlScript(graph, employees, slots, styles, group, members, days, includeEmployees, includeStyles);
        return Encoding.UTF8.GetBytes(script);
    }

    public async Task<byte[]> ExportGraphExcelCsvAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
    {
        var graph = await _containerService.GetGraphByIdAsync(containerId, graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        var slots = await _containerService.GetGraphSlotsAsync(containerId, graphId, ct).ConfigureAwait(false);
        var employees = includeEmployees
            ? await _containerService.GetGraphEmployeesAsync(containerId, graphId, ct).ConfigureAwait(false)
            : [];
        var styles = includeStyles
            ? await _containerService.GetGraphCellStylesAsync(containerId, graphId, ct).ConfigureAwait(false)
            : [];

        var employeeById = employees
            .Where(x => x.Employee is not null)
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.First().Employee!);

        var styleByKey = styles.ToDictionary(x => (x.DayOfMonth, x.EmployeeId), x => x);

        var sb = new StringBuilder(16_384);
        sb.AppendLine("ScheduleId,ScheduleName,Year,Month,Day,SlotNo,FromTime,ToTime,EmployeeId,EmployeeName,Status,BackgroundColorArgb,TextColorArgb");

        foreach (var slot in slots.OrderBy(s => s.DayOfMonth).ThenBy(s => s.SlotNo).ThenBy(s => s.FromTime))
        {
            var employeeName = string.Empty;
            if (slot.EmployeeId is int employeeId && employeeById.TryGetValue(employeeId, out var employee))
            {
                employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
            }

            styleByKey.TryGetValue((slot.DayOfMonth, slot.EmployeeId ?? 0), out var style);

            sb.Append(EscapeCsv(graph.Id)).Append(',')
                .Append(EscapeCsv(graph.Name)).Append(',')
                .Append(EscapeCsv(graph.Year)).Append(',')
                .Append(EscapeCsv(graph.Month)).Append(',')
                .Append(EscapeCsv(slot.DayOfMonth)).Append(',')
                .Append(EscapeCsv(slot.SlotNo)).Append(',')
                .Append(EscapeCsv(slot.FromTime)).Append(',')
                .Append(EscapeCsv(slot.ToTime)).Append(',')
                .Append(EscapeCsv(slot.EmployeeId)).Append(',')
                .Append(EscapeCsv(employeeName)).Append(',')
                .Append(EscapeCsv(slot.Status.ToString())).Append(',')
                .Append(EscapeCsv(style?.BackgroundColorArgb)).Append(',')
                .Append(EscapeCsv(style?.TextColorArgb))
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string BuildSqlScript(
        ScheduleModel graph,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<ScheduleSlotModel> slots,
        IReadOnlyList<ScheduleCellStyleModel> styles,
        AvailabilityGroupModel? group,
        IReadOnlyList<AvailabilityGroupMemberModel> members,
        IReadOnlyList<AvailabilityGroupDayModel> days,
        bool includeEmployees,
        bool includeStyles)
    {
        var sb = new StringBuilder(16_384);
        sb.AppendLine("-- GF3 Graph export");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine();

        sb.AppendLine(SqlInsert("schedule",
            ("id", graph.Id),
            ("container_id", graph.ContainerId),
            ("shop_id", graph.ShopId),
            ("name", graph.Name),
            ("year", graph.Year),
            ("month", graph.Month),
            ("people_per_shift", graph.PeoplePerShift),
            ("shift1_time", graph.Shift1Time),
            ("shift2_time", graph.Shift2Time),
            ("max_hours_per_emp_month", graph.MaxHoursPerEmpMonth),
            ("max_consecutive_days", graph.MaxConsecutiveDays),
            ("max_consecutive_full", graph.MaxConsecutiveFull),
            ("max_full_per_month", graph.MaxFullPerMonth),
            ("note", graph.Note),
            ("availability_group_id", graph.AvailabilityGroupId)));

        if (includeEmployees)
        {
            foreach (var scheduleEmployee in employees.OrderBy(x => x.Id))
            {
                sb.AppendLine(SqlInsert("schedule_employee",
                    ("id", scheduleEmployee.Id),
                    ("schedule_id", scheduleEmployee.ScheduleId),
                    ("employee_id", scheduleEmployee.EmployeeId),
                    ("min_hours_month", scheduleEmployee.MinHoursMonth)));
            }
        }

        foreach (var slot in slots.OrderBy(x => x.Id))
        {
            sb.AppendLine(SqlInsert("schedule_slot",
                ("id", slot.Id),
                ("schedule_id", slot.ScheduleId),
                ("day_of_month", slot.DayOfMonth),
                ("slot_no", slot.SlotNo),
                ("employee_id", slot.EmployeeId),
                ("status", slot.Status.ToString()),
                ("from_time", slot.FromTime),
                ("to_time", slot.ToTime)));
        }

        if (includeStyles)
        {
            foreach (var style in styles.OrderBy(x => x.Id))
            {
                sb.AppendLine(SqlInsert("schedule_cell_style",
                    ("id", style.Id),
                    ("schedule_id", style.ScheduleId),
                    ("day_of_month", style.DayOfMonth),
                    ("employee_id", style.EmployeeId),
                    ("background_color_argb", style.BackgroundColorArgb),
                    ("text_color_argb", style.TextColorArgb)));
            }
        }

        if (group is not null)
        {
            sb.AppendLine(SqlInsert("availability_group",
                ("id", group.Id),
                ("name", group.Name),
                ("year", group.Year),
                ("month", group.Month)));

            foreach (var member in members.OrderBy(x => x.Id))
            {
                sb.AppendLine(SqlInsert("availability_group_member",
                    ("id", member.Id),
                    ("availability_group_id", member.AvailabilityGroupId),
                    ("employee_id", member.EmployeeId)));
            }

            foreach (var day in days.OrderBy(x => x.Id))
            {
                sb.AppendLine(SqlInsert("availability_group_day",
                    ("id", day.Id),
                    ("availability_group_member_id", day.AvailabilityGroupMemberId),
                    ("day_of_month", day.DayOfMonth),
                    ("kind", day.Kind.ToString()),
                    ("interval_str", day.IntervalStr)));
            }
        }

        sb.AppendLine();
        sb.AppendLine("COMMIT;");
        return sb.ToString();
    }

    private static string SqlInsert(string table, params (string Column, object? Value)[] values)
    {
        var columns = string.Join(", ", values.Select(v => v.Column));
        var vals = string.Join(", ", values.Select(v => ToSqlLiteral(v.Value)));
        return $"INSERT OR IGNORE INTO {table} ({columns}) VALUES ({vals});";
    }

    private static string ToSqlLiteral(object? value)
    {
        if (value is null)
        {
            return "NULL";
        }

        if (value is string s)
        {
            return $"'{s.Replace("'", "''")}'";
        }

        if (value is bool b)
        {
            return b ? "1" : "0";
        }

        if (value is DateTime dt)
        {
            return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        }

        if (value is Enum)
        {
            return $"'{value}'";
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? "NULL";
        }

        return $"'{value.ToString()?.Replace("'", "''")}'";
    }

    private static string EscapeCsv(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        if (text.Contains([',', '"', '\n', '\r']))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }
}
