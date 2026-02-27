using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Schedule;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services.Export;

public sealed class ScheduleExcelContextBuilder : IScheduleExcelContextBuilder
{
    private static readonly Regex TimeRegex = new(@"\b\d{1,2}:\d{2}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly ILogger<ScheduleExcelContextBuilder>? _logger;

    public ScheduleExcelContextBuilder(ILogger<ScheduleExcelContextBuilder>? logger = null)
    {
        _logger = logger;
    }

    public ScheduleExcelContext BuildScheduleContext(ScheduleModel graph, ShopModel? shop, IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)
    {
        var table = ScheduleMatrixEngine.BuildScheduleTable(graph.Year, graph.Month, slots, employees, out var colMap, CancellationToken.None);
        LogDebugDiagnostics(graph, slots, employees, table, colMap);
        var daysInMonth = DateTime.DaysInMonth(graph.Year, graph.Month);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var conflict = ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(slots, day, graph.PeoplePerShift, graph.Shift1Time, graph.Shift2Time);
            table.Rows[day - 1][ScheduleMatrixConstants.ConflictColumnName] = conflict;
        }

        var totals = ScheduleTotalsCalculator.Calculate(employees, slots);
        var summary = BuildSummaryFromMatrix(table, colMap, employees, graph.Year, graph.Month);
        var employeeNames = employees
            .Select(GetEmployeeDisplayName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var workFree = summary.Rows.Select(x => new EmployeeWorkFreeStatRow(x.Employee, x.WorkDays, x.FreeDays)).ToList();

        return new ScheduleExcelContext(
            graph.Name,
            graph.Month,
            graph.Year,
            shop?.Name ?? string.Empty,
            shop?.Address ?? string.Empty,
            ScheduleTotalsCalculator.FormatHoursMinutes(totals.TotalDuration),
            totals.TotalEmployees,
            daysInMonth,
            graph.Shift1Time,
            graph.Shift2Time,
            BuildPreviewList(employeeNames),
            table.DefaultView,
            summary.Headers,
            summary.Rows,
            workFree);
    }

    private void LogDebugDiagnostics(ScheduleModel graph, IReadOnlyList<ScheduleSlotModel> slots, IReadOnlyList<ScheduleEmployeeModel> employees, DataTable table, Dictionary<string, int> colMap)
    {
        var isDebugEnabled = string.Equals(Environment.GetEnvironmentVariable("GF3_EXPORT_DEBUG"), "true", StringComparison.OrdinalIgnoreCase);
        if (!isDebugEnabled)
            return;

        var columnNames = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
        var nonEmptyEmployeeCellCount = 0;
        foreach (DataRow row in table.Rows)
        {
            foreach (var employeeColumn in colMap.Keys)
            {
                if (!table.Columns.Contains(employeeColumn))
                    continue;

                var raw = row[employeeColumn];
                var text = raw is null or DBNull ? string.Empty : raw.ToString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, ScheduleMatrixConstants.EmptyMark, StringComparison.Ordinal))
                    nonEmptyEmployeeCellCount++;
            }
        }

        _logger?.LogInformation(
            "GF3 export debug graphId={GraphId}: slots={SlotsCount}, employees={EmployeesCount}, rows={RowCount}, cols=[{Columns}], nonEmptyEmployeeCells={NonEmptyEmployeeCellCount}",
            graph.Id,
            slots.Count,
            employees.Count,
            table.Rows.Count,
            string.Join(", ", columnNames),
            nonEmptyEmployeeCellCount);

        if (nonEmptyEmployeeCellCount == 0)
        {
            var colMapDump = string.Join(", ", colMap.Select(kv => $"{kv.Key}=>{kv.Value}"));
            var firstSlotsDump = string.Join(
                " | ",
                slots.Take(10).Select(s => $"day={s.DayOfMonth}, from={s.FromTime}, to={s.ToTime}, employeeId={s.EmployeeId}"));

            _logger?.LogWarning(
                "GF3 export debug graphId={GraphId}: empty matrix after build; employeeColumns=[{EmployeeColumns}], colMap=[{ColMap}], firstSlots=[{FirstSlots}]",
                graph.Id,
                string.Join(", ", colMap.Keys),
                colMapDump,
                firstSlotsDump);
        }
    }

    public ContainerExcelContext BuildContainerContext(ContainerModel container, IReadOnlyList<GraphExcelContext> graphs)
    {
        var shops = graphs
            .GroupBy(x => x.Graph.ShopId)
            .Select(g => new ContainerShopHeader(g.First().Graph.ShopId.ToString(CultureInfo.InvariantCulture), g.First().Shop?.Name ?? string.Empty))
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var employeeRows = BuildEmployeeShopHoursRows(graphs, shops);
        var employeeStats = employeeRows
            .Where(x => !string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase))
            .Select(x => new EmployeeWorkFreeStatRow(x.Employee, x.WorkDays, x.FreeDays))
            .ToList();

        var totalDuration = TimeSpan.Zero;
        var totalEmployeeIds = new HashSet<int>();
        foreach (var graph in graphs)
        {
            var totals = ScheduleTotalsCalculator.Calculate(graph.Employees, graph.Slots);
            totalDuration += totals.TotalDuration;
            foreach (var emp in graph.Employees)
                totalEmployeeIds.Add(emp.EmployeeId);
        }

        var shopNames = shops.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var employeeNames = employeeRows
            .Where(x => !string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Employee)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var chartContexts = graphs.Select(x => new ContainerExcelChartContext(x.Graph.Name, x.ScheduleContext)).ToList();

        return new ContainerExcelContext(
            container.Id,
            container.Name,
            container.Note ?? string.Empty,
            totalEmployeeIds.Count,
            shops.Count,
            BuildPreviewList(employeeNames),
            BuildPreviewList(shopNames),
            ScheduleTotalsCalculator.FormatHoursMinutes(totalDuration),
            shops,
            employeeRows,
            employeeStats,
            chartContexts);
    }

    private static List<ContainerEmployeeShopHoursRow> BuildEmployeeShopHoursRows(IReadOnlyList<GraphExcelContext> graphs, IReadOnlyList<ContainerShopHeader> shops)
    {
        var byEmployee = new Dictionary<string, ContainerEmployeeShopHoursRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var graph in graphs)
        {
            var daysInMonth = DateTime.DaysInMonth(graph.Graph.Year, graph.Graph.Month);
            var totals = ScheduleTotalsCalculator.Calculate(graph.Employees, graph.Slots);
            var shopKey = graph.Graph.ShopId.ToString(CultureInfo.InvariantCulture);

            foreach (var emp in graph.Employees)
            {
                var name = GetEmployeeDisplayName(emp);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!byEmployee.TryGetValue(name, out var row))
                {
                    row = new ContainerEmployeeShopHoursRow(name);
                    byEmployee[name] = row;
                }

                var empSlots = graph.Slots.Where(x => x.EmployeeId == emp.EmployeeId && x.Status != SlotStatus.UNFURNISHED).ToList();
                row.WorkDays += empSlots.Select(x => x.DayOfMonth).Distinct().Count();
                row.FreeDays += Math.Max(0, daysInMonth - empSlots.Select(x => x.DayOfMonth).Distinct().Count());

                totals.PerEmployeeDuration.TryGetValue(emp.EmployeeId, out var duration);
                row.HoursByShop[shopKey] = ScheduleTotalsCalculator.FormatHoursMinutes(duration);
                row.TotalDuration += duration;
                row.HoursSum = ScheduleTotalsCalculator.FormatHoursMinutes(row.TotalDuration);
            }
        }

        var rows = byEmployee.Values.OrderBy(x => x.Employee, StringComparer.CurrentCultureIgnoreCase).ToList();
        if (rows.Count == 0)
            return rows;

        var totalRow = new ContainerEmployeeShopHoursRow("TOTAL");
        foreach (var shop in shops)
        {
            var sum = TimeSpan.Zero;
            foreach (var row in rows)
            {
                if (!row.HoursByShop.TryGetValue(shop.Key, out var raw) || !TryParseHoursCell(raw, out var duration))
                    continue;
                sum += duration;
            }

            totalRow.HoursByShop[shop.Key] = ScheduleTotalsCalculator.FormatHoursMinutes(sum);
            totalRow.TotalDuration += sum;
        }

        totalRow.WorkDays = rows.Sum(x => x.WorkDays);
        totalRow.FreeDays = rows.Sum(x => x.FreeDays);
        totalRow.HoursSum = ScheduleTotalsCalculator.FormatHoursMinutes(totalRow.TotalDuration);
        rows.Add(totalRow);
        return rows;
    }

    private static bool TryParseHoursCell(string? value, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (ScheduleMatrixEngine.TryParseTime(value, out duration)) return true;

        var m = Regex.Match(value, @"(?<h>\d+)h\s*(?<m>\d+)m", RegexOptions.CultureInvariant);
        if (m.Success && int.TryParse(m.Groups["h"].Value, out var h) && int.TryParse(m.Groups["m"].Value, out var mm))
        {
            duration = TimeSpan.FromHours(h) + TimeSpan.FromMinutes(mm);
            return true;
        }

        if (int.TryParse(value, out var hours))
        {
            duration = TimeSpan.FromHours(hours);
            return true;
        }

        return false;
    }

    private static (List<SummaryDayHeader> Headers, List<SummaryEmployeeRow> Rows) BuildSummaryFromMatrix(DataTable table, Dictionary<string, int> colMap, IReadOnlyList<ScheduleEmployeeModel> employees, int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var rowsByDay = table.Rows.Cast<DataRow>()
            .ToDictionary(r => Convert.ToInt32(r[ScheduleMatrixConstants.DayColumnName], CultureInfo.InvariantCulture), r => r);

        var headers = new List<SummaryDayHeader>(daysInMonth);
        for (var d = 1; d <= daysInMonth; d++)
        {
            var dt = new DateTime(year, month, d);
            headers.Add(new SummaryDayHeader(d, dt.ToString("dddd(dd.MM.yyyy)", CultureInfo.InvariantCulture)));
        }

        var colByEmpId = colMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        var resultRows = new List<SummaryEmployeeRow>(employees.Count);

        foreach (var emp in employees)
        {
            var empId = (emp.Employee?.Id is int navId && navId > 0) ? navId : emp.EmployeeId; if (!colByEmpId.TryGetValue(empId, out var colName)) continue;

            var displayName = GetEmployeeDisplayName(emp);
            var dayCells = new List<SummaryDayCell>(daysInMonth);
            var sum = TimeSpan.Zero;

            for (var d = 1; d <= daysInMonth; d++)
            {
                if (!rowsByDay.TryGetValue(d, out var dr))
                {
                    dayCells.Add(new SummaryDayCell());
                    continue;
                }

                var raw = dr[colName] is null or DBNull ? string.Empty : dr[colName]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw) || raw == ScheduleMatrixConstants.EmptyMark)
                {
                    dayCells.Add(new SummaryDayCell());
                    continue;
                }

                if (raw.IndexOf(':') < 0)
                {
                    dayCells.Add(new SummaryDayCell(raw, string.Empty, string.Empty));
                    continue;
                }

                if (TryParseTimeRanges(raw, out var from, out var to, out var dur))
                {
                    sum += dur;
                    dayCells.Add(new SummaryDayCell(from, to, FormatHoursCell(dur)));
                }
                else
                {
                    dayCells.Add(new SummaryDayCell(raw, string.Empty, string.Empty));
                }
            }

            var sumText = FormatTimeSpanToSummary(sum);
            var workDays = CountWorkDays(dayCells);
            var freeDays = Math.Max(0, daysInMonth - workDays);
            resultRows.Add(new SummaryEmployeeRow(displayName, workDays, freeDays, sumText, dayCells));
        }

        return (headers, resultRows);
    }

    private static bool TryParseTimeRanges(string text, out string from, out string to, out TimeSpan duration)
    {
        from = string.Empty;
        to = string.Empty;
        duration = TimeSpan.Zero;

        var matches = TimeRegex.Matches(text);
        if (matches.Count < 2) return false;

        var times = new List<TimeSpan>(matches.Count);
        foreach (Match m in matches)
        {
            if (TimeSpan.TryParseExact(m.Value, [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out var ts))
                times.Add(ts);
        }

        if (times.Count < 2) return false;
        from = matches[0].Value;
        to = matches[^1].Value;

        for (var i = 0; i + 1 < times.Count; i += 2)
        {
            var delta = times[i + 1] - times[i];
            if (delta > TimeSpan.Zero) duration += delta;
        }

        if (duration == TimeSpan.Zero)
        {
            var delta = times[^1] - times[0];
            if (delta > TimeSpan.Zero) duration = delta;
        }

        return true;
    }

    private static int CountWorkDays(IEnumerable<SummaryDayCell> dayCells)
        => dayCells.Count(x => !string.IsNullOrWhiteSpace(x.From)
                              || !string.IsNullOrWhiteSpace(x.To)
                              || !string.IsNullOrWhiteSpace(x.Hours));

    private static string FormatHoursCell(TimeSpan ts) => FormatTimeSpanToSummary(ts);

    private static string FormatTimeSpanToSummary(TimeSpan ts)
    {
        var totalMinutes = (int)Math.Round(ts.TotalMinutes);
        if (totalMinutes <= 0) return "0";
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return m == 0 ? h.ToString(CultureInfo.InvariantCulture) : $"{h}h {m}m";
    }

    private static string BuildPreviewList(IReadOnlyList<string> items, int previewCount = 8)
    {
        if (items.Count == 0) return "—";
        var trimmed = items.Select(x => (x ?? string.Empty).Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        if (trimmed.Count == 0) return "—";
        if (trimmed.Count <= previewCount) return string.Join(", ", trimmed);
        return $"{string.Join(", ", trimmed.Take(previewCount))} … (+{trimmed.Count - previewCount})";
    }

    private static string GetEmployeeDisplayName(ScheduleEmployeeModel employee)
    {
        var first = employee.Employee?.FirstName?.Trim() ?? string.Empty;
        var last = employee.Employee?.LastName?.Trim() ?? string.Empty;
        return $"{first} {last}".Trim();
    }
}

public sealed record GraphExcelContext(ScheduleModel Graph, ShopModel? Shop, IReadOnlyList<ScheduleEmployeeModel> Employees, IReadOnlyList<ScheduleSlotModel> Slots, ScheduleExcelContext ScheduleContext);

public sealed record ScheduleExcelContext(
    string ScheduleName,
    int ScheduleMonth,
    int ScheduleYear,
    string ShopName,
    string ShopAddress,
    string TotalHoursText,
    int TotalEmployees,
    int TotalDays,
    string Shift1,
    string Shift2,
    string TotalEmployeesListText,
    DataView ScheduleMatrix,
    IReadOnlyList<SummaryDayHeader> SummaryDayHeaders,
    IReadOnlyList<SummaryEmployeeRow> SummaryRows,
    IReadOnlyList<EmployeeWorkFreeStatRow> EmployeeWorkFreeStats);

public sealed record SummaryDayHeader(int Day, string Text);
public sealed record SummaryDayCell(string From = "", string To = "", string Hours = "");
public sealed record SummaryEmployeeRow(string Employee, int WorkDays, int FreeDays, string Sum, IReadOnlyList<SummaryDayCell> Days);
public sealed record EmployeeWorkFreeStatRow(string Employee, int WorkDays, int FreeDays);

public sealed record ContainerExcelContext(
    int ContainerId,
    string ContainerName,
    string ContainerNote,
    int TotalEmployees,
    int TotalShops,
    string TotalEmployeesListText,
    string TotalShopsListText,
    string TotalHoursText,
    IReadOnlyList<ContainerShopHeader> ShopHeaders,
    IReadOnlyList<ContainerEmployeeShopHoursRow> EmployeeShopHoursRows,
    IReadOnlyList<EmployeeWorkFreeStatRow> EmployeeWorkFreeStats,
    IReadOnlyList<ContainerExcelChartContext> Charts);

public sealed record ContainerExcelChartContext(string ChartName, ScheduleExcelContext ScheduleContext);
public sealed record ContainerShopHeader(string Key, string Name);

public sealed class ContainerEmployeeShopHoursRow
{
    public ContainerEmployeeShopHoursRow(string employee) => Employee = employee;
    public string Employee { get; }
    public int WorkDays { get; set; }
    public int FreeDays { get; set; }
    public string HoursSum { get; set; } = "0";
    public Dictionary<string, string> HoursByShop { get; } = new(StringComparer.OrdinalIgnoreCase);
    public TimeSpan TotalDuration { get; set; }
}
