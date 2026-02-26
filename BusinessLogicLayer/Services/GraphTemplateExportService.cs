using System.Data;
using System.Globalization;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Schedule;
using BusinessLogicLayer.Services.Abstractions;
using ClosedXML.Excel;

namespace BusinessLogicLayer.Services;

public sealed class GraphTemplateExportService : IGraphTemplateExportService
{
    private static readonly string[] MatrixTemplateSheetNames = ["ScheduleName", "R_FL_35", "MatrixTemplate"];
    private static readonly string[] StatisticTemplateSheetNames = ["ScheduleStatistic", "Schedule Statistic", "StatisticTemplate"];
    private static readonly string[] ContainerTemplateSheetNames = ["ContainerTemplate", "Sheet1", "Container"];

    private readonly IContainerService _containerService;
    private readonly IShopService _shopService;
    private readonly IExcelTemplateLocator _templateLocator;

    public GraphTemplateExportService(IContainerService containerService, IShopService shopService, IExcelTemplateLocator templateLocator)
    {
        _containerService = containerService;
        _shopService = shopService;
        _templateLocator = templateLocator;
    }

    public async Task<(byte[] content, string fileName)> ExportGraphToXlsxAsync(int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
    {
        var graphData = await LoadGraphDataAsync(containerId, graphId, includeStyles, includeEmployees, ct).ConfigureAwait(false);
        using var wb = new XLWorkbook(_templateLocator.GetScheduleTemplatePath());
        AddGraphSheets(wb, graphData, graphData.Graph.Name);
        DeleteTemplateSheets(wb, MatrixTemplateSheetNames);
        DeleteTemplateSheets(wb, StatisticTemplateSheetNames);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return (ms.ToArray(), $"GF3_Graph_{graphId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
    }

    public async Task<(byte[] content, string fileName)> ExportContainerToXlsxAsync(int containerId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
    {
        var container = await _containerService.GetAsync(containerId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Container with id {containerId} was not found.");
        var graphs = await _containerService.GetGraphsAsync(containerId, ct).ConfigureAwait(false);

        using var wb = new XLWorkbook(_templateLocator.GetScheduleTemplatePath());
        using var containerWb = new XLWorkbook(_templateLocator.GetContainerTemplatePath());

        var containerTemplate = FindTemplateSheet(containerWb, ContainerTemplateSheetNames)
            ?? throw new InvalidOperationException($"Container template sheet not found. Expected one of: {string.Join(", ", ContainerTemplateSheetNames)}");

        var imported = containerTemplate.CopyTo(wb, MakeUniqueSheetName(wb, "__ContainerTemplate__"));
        var containerSheet = imported.CopyTo(MakeUniqueSheetName(wb, "Container"));
        FillContainerSheet(containerSheet, container, graphs);
        NormalizeGray125(containerSheet);

        foreach (var graph in graphs)
        {
            var graphData = await LoadGraphDataAsync(containerId, graph.Id, includeStyles, includeEmployees, ct).ConfigureAwait(false);
            AddGraphSheets(wb, graphData, graph.Name);
        }

        DeleteTemplateSheets(wb, MatrixTemplateSheetNames);
        DeleteTemplateSheets(wb, StatisticTemplateSheetNames);
        imported.Delete();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return (ms.ToArray(), $"GF3_Container_{containerId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
    }

    private async Task<GraphExportData> LoadGraphDataAsync(int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct)
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
        var shop = await _shopService.GetAsync(graph.ShopId, ct).ConfigureAwait(false);
        return new GraphExportData(graph, shop, employees, slots, styles);
    }

    private static void AddGraphSheets(XLWorkbook wb, GraphExportData data, string sheetBaseName)
    {
        var matrixTemplate = FindTemplateSheet(wb, MatrixTemplateSheetNames)
            ?? throw new InvalidOperationException($"Matrix template sheet not found. Expected one of: {string.Join(", ", MatrixTemplateSheetNames)}");
        var statTemplate = FindTemplateSheet(wb, StatisticTemplateSheetNames)
            ?? throw new InvalidOperationException($"Statistic template sheet not found. Expected one of: {string.Join(", ", StatisticTemplateSheetNames)}");

        var matrixSheet = matrixTemplate.CopyTo(MakeUniqueSheetName(wb, SanitizeWorksheetName(sheetBaseName, "Schedule")));
        var statSheet = statTemplate.CopyTo(MakeUniqueSheetName(wb, SanitizeWorksheetName($"{sheetBaseName} - Statistic", "Schedule - Statistic")));

        FillMatrixSheet(matrixSheet, data);
        NormalizeGray125(matrixSheet);
        FillStatisticSheet(statSheet, data);
        NormalizeGray125(statSheet);
    }

    private static void DeleteTemplateSheets(XLWorkbook wb, IEnumerable<string> templateSheetNames)
    {
        foreach (var sheetName in templateSheetNames)
        {
            if (wb.Worksheets.TryGetWorksheet(sheetName, out _))
                wb.Worksheets.Delete(sheetName);
        }
    }

    private static void FillMatrixSheet(IXLWorksheet sheet, GraphExportData data)
    {
        sheet.Range("B1:AA32").Clear(XLClearOptions.Contents);
        sheet.Cell(1, 2).Value = data.Shop?.Name ?? string.Empty;

        var table = BuildMatrixTable(data.Graph, data.Employees, data.Slots);
        var dayColumn = table.Columns[0];
        var employeeColumns = table.Columns.Cast<DataColumn>().Skip(1).ToList();

        for (var i = 0; i < Math.Min(employeeColumns.Count, 25); i++)
            sheet.Cell(1, 3 + i).Value = employeeColumns[i].Caption;

        var maxDays = DateTime.DaysInMonth(data.Graph.Year, data.Graph.Month);
        for (var day = 1; day <= 31; day++)
        {
            var row = day + 1;
            sheet.Cell(row, 2).Value = day <= maxDays ? new DateTime(data.Graph.Year, data.Graph.Month, day) : " ";

            for (var i = 0; i < Math.Min(employeeColumns.Count, 25); i++)
            {
                var value = day <= table.Rows.Count ? table.Rows[day - 1][employeeColumns[i].ColumnName]?.ToString() : null;
                sheet.Cell(row, 3 + i).Value = string.IsNullOrWhiteSpace(value) ? "-" : value;
            }
        }
    }

    private static void FillStatisticSheet(IXLWorksheet sheet, GraphExportData data)
    {
        var totals = ScheduleTotalsCalculator.Calculate(data.Employees, data.Slots);
        sheet.Cell(1, 2).Value = data.Graph.Name;
        sheet.Cell(2, 2).Value = data.Shop?.Name ?? string.Empty;
        sheet.Cell(3, 2).Value = data.Shop?.Address ?? string.Empty;
        sheet.Cell(4, 2).Value = new DateTime(data.Graph.Year, data.Graph.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        sheet.Cell(5, 2).Value = $"{data.Graph.Shift1Time}; {data.Graph.Shift2Time}";
        sheet.Cell(6, 2).Value = ScheduleTotalsCalculator.FormatHoursMinutes(totals.TotalDuration);
        sheet.Cell(7, 2).Value = totals.TotalEmployees;

        var maxDays = DateTime.DaysInMonth(data.Graph.Year, data.Graph.Month);
        for (var i = 0; i < 31; i++)
            sheet.Cell(14, 5 + i * 3).Value = i < maxDays ? i + 1 : string.Empty;

        var rows = data.Employees.Take(10).ToList();
        foreach (var (employee, index) in rows.Select((x, i) => (x, i)))
        {
            var rowNo = 16 + index;
            var employeeName = FormatEmployee(employee.Employee);
            sheet.Cell(rowNo, 1).Value = employeeName;

            var empSlots = data.Slots.Where(x => x.EmployeeId == employee.EmployeeId).ToList();
            var workDays = empSlots.Select(x => x.DayOfMonth).Distinct().Count();
            sheet.Cell(rowNo, 2).Value = workDays;
            sheet.Cell(rowNo, 3).Value = Math.Max(0, maxDays - workDays);
            if (totals.PerEmployeeDuration.TryGetValue(employee.EmployeeId, out var dur))
                sheet.Cell(rowNo, 4).Value = ScheduleTotalsCalculator.FormatHoursMinutes(dur);

            for (var d = 1; d <= maxDays; d++)
            {
                var slot = empSlots.Where(x => x.DayOfMonth == d && x.Status != SlotStatus.UNFURNISHED).OrderBy(x => x.SlotNo).FirstOrDefault();
                var c = 5 + (d - 1) * 3;
                sheet.Cell(rowNo, c).Value = slot?.FromTime ?? string.Empty;
                sheet.Cell(rowNo, c + 1).Value = slot?.ToTime ?? string.Empty;
                if (slot is not null)
                    sheet.Cell(rowNo, c + 2).Value = BuildDuration(slot.FromTime, slot.ToTime);
            }
        }
    }

    private static void FillContainerSheet(IXLWorksheet sheet, ContainerModel container, IReadOnlyList<ScheduleModel> graphs)
    {
        var totalEmployees = graphs.SelectMany(x => x.Employees.Select(e => e.EmployeeId)).Distinct().Count();
        var map = new Dictionary<string, string>
        {
            ["{Id}"] = container.Id.ToString(CultureInfo.InvariantCulture),
            ["{Name}"] = container.Name,
            ["{Note}"] = container.Note ?? string.Empty,
            ["{Container Name}"] = container.Name,
            ["{Total Employees Count}"] = totalEmployees.ToString(CultureInfo.InvariantCulture),
            ["{Total Shops Count}"] = graphs.Select(x => x.ShopId).Distinct().Count().ToString(CultureInfo.InvariantCulture),
            ["{Total Hours}"] = "",
            ["{Total Employees}"] = "",
            ["{Total Shops}"] = ""
        };

        foreach (var cell in sheet.CellsUsed())
        {
            var value = cell.GetString();
            foreach (var kv in map)
                value = value.Replace(kv.Key, kv.Value, StringComparison.Ordinal);
            cell.Value = value;
        }
    }

    private static DataTable BuildMatrixTable(ScheduleModel graph, IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)
    {
        var table = new DataTable();
        table.Columns.Add(ScheduleMatrixConstants.DayColumnName, typeof(int));

        var orderedEmployees = employees
            .OrderBy(x => x.Employee?.LastName)
            .ThenBy(x => x.Employee?.FirstName)
            .ToList();

        foreach (var employee in orderedEmployees)
            table.Columns.Add($"E_{employee.EmployeeId}", typeof(string)).Caption = FormatEmployee(employee.Employee);

        var maxDays = DateTime.DaysInMonth(graph.Year, graph.Month);
        for (var day = 1; day <= maxDays; day++)
        {
            var row = table.NewRow();
            row[0] = day;

            for (var col = 0; col < orderedEmployees.Count; col++)
            {
                var emp = orderedEmployees[col];
                var empSlots = slots.Where(x => x.DayOfMonth == day && x.EmployeeId == emp.EmployeeId && x.Status != SlotStatus.UNFURNISHED)
                    .OrderBy(x => x.SlotNo)
                    .ToList();
                row[col + 1] = empSlots.Count == 0
                    ? "-"
                    : string.Join(Environment.NewLine, empSlots.Select(x => $"{x.FromTime}-{x.ToTime}"));
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static string BuildDuration(string? from, string? to)
    {
        if (!ScheduleMatrixEngine.TryParseTime(from, out var t1) || !ScheduleMatrixEngine.TryParseTime(to, out var t2))
            return string.Empty;

        var diff = t2 - t1;
        if (diff < TimeSpan.Zero)
            diff += TimeSpan.FromHours(24);
        return ScheduleTotalsCalculator.FormatHoursMinutes(diff);
    }

    private static string FormatEmployee(EmployeeModel? employee)
    {
        if (employee is null) return string.Empty;
        return $"{employee.FirstName} {employee.LastName}".Trim();
    }

    private static IXLWorksheet? FindTemplateSheet(XLWorkbook wb, IEnumerable<string> names)
        => wb.Worksheets.FirstOrDefault(x => names.Contains(x.Name, StringComparer.OrdinalIgnoreCase));

    private static void NormalizeGray125(IXLWorksheet sheet, string? rangeAddress = null)
    {
        var range = !string.IsNullOrWhiteSpace(rangeAddress)
            ? sheet.Range(rangeAddress)
            : sheet.RangeUsed();

        if (range is not null)
        {
            foreach (var cell in range.Cells())
            {
                if (cell.Style.Fill.PatternType == XLFillPatternValues.Gray125)
                {
                    cell.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                    cell.Style.Fill.BackgroundColor = XLColor.White;
                    cell.Style.Fill.PatternColor = XLColor.Black;
                }
            }
        }

        foreach (var conditionalFormat in sheet.ConditionalFormats)
        {
            if (conditionalFormat.Style.Fill.PatternType == XLFillPatternValues.Gray125)
            {
                conditionalFormat.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                conditionalFormat.Style.Fill.BackgroundColor = XLColor.White;
                conditionalFormat.Style.Fill.PatternColor = XLColor.Black;
            }
        }
    }

    private static string MakeUniqueSheetName(XLWorkbook wb, string baseName)
    {
        var name = baseName;
        var i = 1;
        while (wb.Worksheets.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            var suffix = $" ({i++})";
            var truncated = baseName.Length + suffix.Length > 31 ? baseName[..(31 - suffix.Length)] : baseName;
            name = truncated + suffix;
        }

        return name;
    }

    private static string SanitizeWorksheetName(string? name, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        foreach (var ch in new[] { '[', ']', '*', '?', '/', '\\', ':' })
            value = value.Replace(ch, '_');
        return value.Length > 31 ? value[..31] : value;
    }

    private sealed record GraphExportData(
        ScheduleModel Graph,
        ShopModel? Shop,
        IReadOnlyList<ScheduleEmployeeModel> Employees,
        IReadOnlyList<ScheduleSlotModel> Slots,
        IReadOnlyList<ScheduleCellStyleModel> Styles);
}
