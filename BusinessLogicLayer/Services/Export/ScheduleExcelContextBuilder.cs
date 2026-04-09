using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Schedule;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services.Export;

/// <summary>
/// Builds export-oriented read models for both single-graph and container-level Excel reports.
/// This component intentionally owns presentation shaping so scheduling services can stay focused
/// on business rules while the export layer receives stable, template-friendly DTOs.
/// </summary>
public sealed class ScheduleExcelContextBuilder : IScheduleExcelContextBuilder
{
    private static readonly Regex TimeRegex = new(@"\b\d{1,2}:\d{2}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly ILogger<ScheduleExcelContextBuilder>? _logger;

    /// <summary>
    /// Creates the builder. Logger stays optional because tests and command-line tooling often
    /// instantiate the builder directly, and export generation should remain deterministic either way.
    /// </summary>
    public ScheduleExcelContextBuilder(ILogger<ScheduleExcelContextBuilder>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds the full export context for one schedule graph.
    /// The flow intentionally follows the same lifecycle as the workbook:
    /// 1. build the schedule matrix,
    /// 2. apply note-driven manual overrides,
    /// 3. recompute staffing conflicts,
    /// 4. derive summary rows and preview strings for the template.
    /// </summary>
    public ScheduleExcelContext BuildScheduleContext(ScheduleModel graph, ShopModel? shop, IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)
    {
        var noteMetadata = GraphNoteExportMetadataParser.Parse(graph.Note);
        var table = BuildScheduleMatrix(graph, employees, slots, noteMetadata, out var colMap);
        var daysInMonth = DateTime.DaysInMonth(graph.Year, graph.Month);
        RecomputeConflictFlags(table, graph, slots, daysInMonth);
        var totals = ScheduleTotalsCalculator.Calculate(employees, slots);
        var summary = BuildSummaryFromMatrix(table, colMap, employees, graph.Year, graph.Month, BuildIgnoredTextCellSet(noteMetadata.TextCells));
        var employeeNames = BuildSortedDistinctEmployeeNames(employees);
        var workFree = BuildWorkFreeStats(summary.Rows);

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

    private DataTable BuildScheduleMatrix(
        ScheduleModel graph,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<ScheduleSlotModel> slots,
        GraphNoteExportMetadata noteMetadata,
        out Dictionary<string, int> colMap)
    {
        var table = ScheduleMatrixEngine.BuildScheduleTable(graph.Year, graph.Month, slots, employees, out colMap, CancellationToken.None);
        ApplyGraphNoteMetadata(table, colMap, employees, noteMetadata);
        LogDebugDiagnostics(graph, slots, employees, table, colMap);
        return table;
    }

    private static void RecomputeConflictFlags(
        DataTable table,
        ScheduleModel graph,
        IReadOnlyList<ScheduleSlotModel> slots,
        int daysInMonth)
    {
        for (var day = 1; day <= daysInMonth; day++)
        {
            var conflict = ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(
                slots,
                day,
                graph.PeoplePerShift,
                graph.Shift1Time,
                graph.Shift2Time);
            table.Rows[day - 1][ScheduleMatrixConstants.ConflictColumnName] = conflict;
        }
    }

    private static void ApplyGraphNoteMetadata(
        DataTable table,
        Dictionary<string, int> colMap,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        GraphNoteExportMetadata noteMetadata)
    {
        if (noteMetadata == GraphNoteExportMetadata.Empty)
            return;

        var rowsByDay = table.Rows
            .Cast<DataRow>()
            .ToDictionary(
                row => Convert.ToInt32(row[ScheduleMatrixConstants.DayColumnName], CultureInfo.InvariantCulture),
                row => row);

        var employeeColumnNamesById = colMap
            .GroupBy(entry => entry.Value)
            .ToDictionary(group => group.Key, group => group.First().Key);
        var manualColumnNamesById = AddManualColumns(table, rowsByDay, noteMetadata.ManualColumns);

        foreach (var textCell in noteMetadata.TextCells)
        {
            if (!employeeColumnNamesById.TryGetValue(textCell.EmployeeId, out var columnName))
                continue;
            if (!rowsByDay.TryGetValue(textCell.DayOfMonth, out var row))
                continue;

            row[columnName] = textCell.Value;
        }

        ReorderMatrixColumns(table, employees, noteMetadata, employeeColumnNamesById, manualColumnNamesById);
    }

    private static Dictionary<int, string> AddManualColumns(
        DataTable table,
        IReadOnlyDictionary<int, DataRow> rowsByDay,
        IReadOnlyList<GraphNoteExportManualColumn> manualColumns)
    {
        var manualColumnNamesById = new Dictionary<int, string>();

        foreach (var manualColumn in manualColumns)
        {
            var columnName = $"manual_{manualColumn.Id}";
            var suffix = 1;
            while (table.Columns.Contains(columnName))
                columnName = $"manual_{manualColumn.Id}_{++suffix}";

            var column = table.Columns.Add(columnName, typeof(string));
            column.Caption = string.IsNullOrWhiteSpace(manualColumn.Label)
                ? $"Custom {manualColumn.Id}"
                : manualColumn.Label;

            foreach (DataRow row in table.Rows)
                row[columnName] = ScheduleMatrixConstants.EmptyMark;

            foreach (var cell in manualColumn.Cells)
            {
                if (!rowsByDay.TryGetValue(cell.Key, out var row))
                    continue;

                row[columnName] = string.IsNullOrWhiteSpace(cell.Value)
                    ? ScheduleMatrixConstants.EmptyMark
                    : cell.Value;
            }

            manualColumnNamesById[manualColumn.Id] = columnName;
        }

        return manualColumnNamesById;
    }

    private static void ReorderMatrixColumns(
        DataTable table,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        GraphNoteExportMetadata noteMetadata,
        IReadOnlyDictionary<int, string> employeeColumnNamesById,
        IReadOnlyDictionary<int, string> manualColumnNamesById)
    {
        var fallbackOrder = BuildDefaultMatrixColumnOrder(employees, noteMetadata.ManualColumns);
        var validIds = fallbackOrder.ToHashSet();
        var orderedIds = new List<int>(fallbackOrder.Count);

        foreach (var columnId in noteMetadata.ColumnOrder)
        {
            if (!validIds.Contains(columnId) || orderedIds.Contains(columnId))
                continue;

            orderedIds.Add(columnId);
        }

        foreach (var columnId in fallbackOrder)
        {
            if (!orderedIds.Contains(columnId))
                orderedIds.Add(columnId);
        }

        var nextOrdinal = 3;
        foreach (var columnId in orderedIds)
        {
            var columnName = columnId > 0
                ? employeeColumnNamesById.GetValueOrDefault(columnId)
                : manualColumnNamesById.GetValueOrDefault(Math.Abs(columnId));

            if (string.IsNullOrWhiteSpace(columnName) || !table.Columns.Contains(columnName))
                continue;

            var column = table.Columns[columnName];
            if (column is null)
                continue;

            column.SetOrdinal(nextOrdinal++);
        }
    }

    private static List<int> BuildDefaultMatrixColumnOrder(
        IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<GraphNoteExportManualColumn> manualColumns)
    {
        var orderedEmployeeIds = employees
            .Where(employee => employee is not null)
            .Select(employee => new
            {
                EmployeeId = (employee.Employee?.Id is int navId && navId > 0) ? navId : employee.EmployeeId,
                DisplayOrder = employee.DisplayOrder >= 0 ? employee.DisplayOrder : int.MaxValue,
                Label = GetEmployeeSortLabel(employee),
            })
            .Where(item => item.EmployeeId > 0)
            .GroupBy(item => item.EmployeeId)
            .Select(group => group.First())
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.EmployeeId)
            .Select(item => item.EmployeeId)
            .ToList();

        orderedEmployeeIds.AddRange(manualColumns.Select(column => -column.Id));
        return orderedEmployeeIds;
    }

    private static string GetEmployeeSortLabel(ScheduleEmployeeModel employee)
    {
        var displayName = GetEmployeeDisplayName(employee);
        return string.IsNullOrWhiteSpace(displayName)
            ? $"Employee {employee.EmployeeId}"
            : displayName;
    }

    private static HashSet<(int EmployeeId, int DayOfMonth)> BuildIgnoredTextCellSet(IReadOnlyList<GraphNoteExportTextCell> textCells)
        => textCells
            .Where(textCell => textCell.EmployeeId > 0 && textCell.DayOfMonth > 0)
            .Select(textCell => (textCell.EmployeeId, textCell.DayOfMonth))
            .ToHashSet();

    /// <summary>
    /// Emits optional diagnostics for hard-to-reproduce export incidents.
    /// We keep this behind an environment flag so production-like troubleshooting remains possible
    /// without permanently increasing log volume for the default happy path.
    /// </summary>
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

    /// <summary>
    /// Builds the aggregated context for container exports that combine multiple schedule graphs.
    /// </summary>
    public ContainerExcelContext BuildContainerContext(ContainerModel container, IReadOnlyList<GraphExcelContext> graphs)
    {
        var shops = BuildShopHeaders(graphs);
        var employeeRows = BuildEmployeeShopHoursRows(graphs, shops);
        var employeeStats = BuildContainerEmployeeStats(employeeRows);
        var (totalDuration, totalEmployeeIds) = AggregateContainerTotals(graphs);

        var shopNames = shops.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var employeeNames = employeeRows
            .Where(x => !string.Equals(x.Employee, TotalRowEmployeeName, StringComparison.OrdinalIgnoreCase))
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

    private const string TotalRowEmployeeName = "TOTAL";

    private static List<ContainerShopHeader> BuildShopHeaders(IReadOnlyList<GraphExcelContext> graphs)
        => graphs
            .GroupBy(graph => graph.Graph.ShopId)
            .Select(group =>
            {
                var firstGraph = group.First();
                return new ContainerShopHeader(
                    firstGraph.Graph.ShopId.ToString(CultureInfo.InvariantCulture),
                    firstGraph.Shop?.Name ?? string.Empty);
            })
            .OrderBy(header => header.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static (TimeSpan TotalDuration, HashSet<int> EmployeeIds) AggregateContainerTotals(IReadOnlyList<GraphExcelContext> graphs)
    {
        var totalDuration = TimeSpan.Zero;
        var employeeIds = new HashSet<int>();

        foreach (var graph in graphs)
        {
            var totals = ScheduleTotalsCalculator.Calculate(graph.Employees, graph.Slots);
            totalDuration += totals.TotalDuration;

            foreach (var employee in graph.Employees)
            {
                employeeIds.Add(employee.EmployeeId);
            }
        }

        return (totalDuration, employeeIds);
    }

    private static List<EmployeeWorkFreeStatRow> BuildContainerEmployeeStats(IReadOnlyList<ContainerEmployeeShopHoursRow> employeeRows)
        => employeeRows
            .Where(row => !string.Equals(row.Employee, TotalRowEmployeeName, StringComparison.OrdinalIgnoreCase))
            .Select(row => new EmployeeWorkFreeStatRow(row.Employee, row.WorkDays, row.FreeDays))
            .ToList();

    /// <summary>
    /// Aggregates container-level hours by employee and by shop.
    /// The export template expects a synthetic TOTAL row, so we build it here instead of asking
    /// the Excel layer to recalculate the summary with formulas.
    /// </summary>
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

        var totalRow = new ContainerEmployeeShopHoursRow(TotalRowEmployeeName);
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

    /// <summary>
    /// Parses already-formatted hour cells back into a <see cref="TimeSpan"/>.
    /// The parsing order intentionally preserves historical behavior: a raw value like "7" is
    /// first treated as a time token by the matrix engine before we fall back to "7 hours".
    /// Existing exports and tests depend on that compatibility contract.
    /// </summary>
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

    /// <summary>
    /// Converts the matrix view into the summary layout consumed by the workbook templates.
    /// It keeps raw text overrides intact while still deriving work/free day counters and total hours.
    /// </summary>
    private static (List<SummaryDayHeader> Headers, List<SummaryEmployeeRow> Rows) BuildSummaryFromMatrix(
        DataTable table,
        Dictionary<string, int> colMap,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        int year,
        int month,
        IReadOnlySet<(int EmployeeId, int DayOfMonth)>? ignoredTextCells = null)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var rowsByDay = table.Rows.Cast<DataRow>()
            .ToDictionary(r => Convert.ToInt32(r[ScheduleMatrixConstants.DayColumnName], CultureInfo.InvariantCulture), r => r);

        var headers = BuildSummaryHeaders(year, month, daysInMonth);
        var colByEmpId = colMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        var resultRows = new List<SummaryEmployeeRow>(employees.Count);

        foreach (var emp in employees)
        {
            var empId = (emp.Employee?.Id is int navId && navId > 0) ? navId : emp.EmployeeId;
            if (!colByEmpId.TryGetValue(empId, out var colName))
            {
                continue;
            }

            var displayName = GetEmployeeDisplayName(emp);
            var dayCells = new List<SummaryDayCell>(daysInMonth);
            var sum = TimeSpan.Zero;

            for (var d = 1; d <= daysInMonth; d++)
            {
                dayCells.Add(BuildSummaryDayCell(rowsByDay, ignoredTextCells, empId, d, colName, ref sum));
            }

            var sumText = FormatTimeSpanToSummary(sum);
            var workDays = CountWorkDays(dayCells);
            var freeDays = Math.Max(0, daysInMonth - workDays);
            resultRows.Add(new SummaryEmployeeRow(displayName, workDays, freeDays, sumText, dayCells));
        }

        return (headers, resultRows);
    }

    private static List<SummaryDayHeader> BuildSummaryHeaders(int year, int month, int daysInMonth)
    {
        var headers = new List<SummaryDayHeader>(daysInMonth);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            headers.Add(new SummaryDayHeader(day, date.ToString("dddd(dd.MM.yyyy)", CultureInfo.InvariantCulture)));
        }

        return headers;
    }

    private static SummaryDayCell BuildSummaryDayCell(
        IReadOnlyDictionary<int, DataRow> rowsByDay,
        IReadOnlySet<(int EmployeeId, int DayOfMonth)>? ignoredTextCells,
        int employeeId,
        int dayOfMonth,
        string columnName,
        ref TimeSpan totalDuration)
    {
        if (ignoredTextCells?.Contains((employeeId, dayOfMonth)) == true)
        {
            return new SummaryDayCell();
        }

        if (!rowsByDay.TryGetValue(dayOfMonth, out var row))
        {
            return new SummaryDayCell();
        }

        var rawValue = ReadMatrixTextValue(row, columnName);
        if (string.IsNullOrWhiteSpace(rawValue) || rawValue == ScheduleMatrixConstants.EmptyMark)
        {
            return new SummaryDayCell();
        }

        if (rawValue.IndexOf(':') < 0)
        {
            return new SummaryDayCell(rawValue, string.Empty, string.Empty);
        }

        if (!TryParseTimeRanges(rawValue, out var from, out var to, out var duration))
        {
            return new SummaryDayCell(rawValue, string.Empty, string.Empty);
        }

        totalDuration += duration;
        return new SummaryDayCell(from, to, FormatHoursCell(duration));
    }

    private static string ReadMatrixTextValue(DataRow row, string columnName)
        => row[columnName] is null or DBNull
            ? string.Empty
            : row[columnName]?.ToString() ?? string.Empty;

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

    private static List<string> BuildSortedDistinctEmployeeNames(IReadOnlyList<ScheduleEmployeeModel> employees)
        => employees
            .Select(GetEmployeeDisplayName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static List<EmployeeWorkFreeStatRow> BuildWorkFreeStats(IReadOnlyList<SummaryEmployeeRow> summaryRows)
        => summaryRows
            .Select(row => new EmployeeWorkFreeStatRow(row.Employee, row.WorkDays, row.FreeDays))
            .ToList();

    /// <summary>
    /// Produces a compact preview string for workbook headers.
    /// The export contains the full data elsewhere, so the header intentionally shows only a short summary.
    /// </summary>
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

/// <summary>
/// Wraps one graph together with all data already prepared for container-level exports.
/// </summary>
public sealed record GraphExcelContext(ScheduleModel Graph, ShopModel? Shop, IReadOnlyList<ScheduleEmployeeModel> Employees, IReadOnlyList<ScheduleSlotModel> Slots, ScheduleExcelContext ScheduleContext);

/// <summary>
/// Immutable payload consumed by single-graph Excel templates.
/// </summary>
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

/// <summary>
/// Header descriptor for one day in the summary section.
/// </summary>
public sealed record SummaryDayHeader(int Day, string Text);

/// <summary>
/// Per-day summary cell for one employee.
/// </summary>
public sealed record SummaryDayCell(string From = "", string To = "", string Hours = "");

/// <summary>
/// Summary row with work/free day counters and rendered cells for one employee.
/// </summary>
public sealed record SummaryEmployeeRow(string Employee, int WorkDays, int FreeDays, string Sum, IReadOnlyList<SummaryDayCell> Days);

/// <summary>
/// Compact work/free day statistic reused by multiple export templates.
/// </summary>
public sealed record EmployeeWorkFreeStatRow(string Employee, int WorkDays, int FreeDays);

/// <summary>
/// Immutable payload consumed by container-level Excel templates.
/// </summary>
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

/// <summary>
/// One chart section embedded into a container workbook.
/// </summary>
public sealed record ContainerExcelChartContext(string ChartName, ScheduleExcelContext ScheduleContext);

/// <summary>
/// Header descriptor for one shop column in a container export.
/// </summary>
public sealed record ContainerShopHeader(string Key, string Name);

/// <summary>
/// Mutable aggregation row used while calculating container totals by employee and by shop.
/// The builder fills this object incrementally before exposing it through the immutable context.
/// </summary>
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
