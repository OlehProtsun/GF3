using System.Data;
using System.Globalization;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Schedule;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Services.Export;
using ClosedXML.Excel;

namespace BusinessLogicLayer.Services;

public sealed class GraphTemplateExportService : IGraphTemplateExportService
{
    private static readonly string[] MatrixTemplateSheetNames = ["ScheduleName", "R_FL_35", "MatrixTemplate"];
    private static readonly string[] StatisticTemplateSheetNames = ["ScheduleStatistic", "Schedule Statistic", "StatisticTemplate"];
    private static readonly string[] ContainerTemplateSheetNames = ["ContainerTemplate", "Sheet1", "Container"];

    private const string MatrixClearRange = "B1:AA32";
    private const int M_ShopRow = 1;
    private const int M_ShopCol = 2;
    private const int M_HeaderRow = 1;
    private const int M_DateCol = 2;
    private const int M_FirstDataCol = 3;
    private const int M_LastDataCol = 27;
    private const int M_FirstDayRow = 2;
    private const int M_DayCount = 31;

    private const int S_ValueCol = 2;
    private const int S_EmployeesLineRow = 12;
    private const int S_DayHeaderRow = 14;
    private const int S_BodyFirstRow = 16;
    private const int S_MaxSummaryRows = 10;
    private const int S_EmployeeCol = 1;
    private const int S_WorkDaysCol = 2;
    private const int S_FreeDaysCol = 3;
    private const int S_SumCol = 4;
    private const int S_FirstDayCol = 5;
    private const int S_DaysCapacity = 31;

    private readonly IContainerService _containerService;
    private readonly IShopService _shopService;
    private readonly IExcelTemplateLocator _templateLocator;
    private readonly IScheduleExcelContextBuilder _contextBuilder;

    public GraphTemplateExportService(IContainerService containerService, IShopService shopService, IExcelTemplateLocator templateLocator, IScheduleExcelContextBuilder contextBuilder)
    {
        _containerService = containerService;
        _shopService = shopService;
        _templateLocator = templateLocator;
        _contextBuilder = contextBuilder;
    }

    public async Task<(byte[] content, string fileName)> ExportGraphToXlsxAsync(int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
    {
        var graphData = await LoadGraphDataAsync(containerId, graphId, includeStyles, includeEmployees, ct).ConfigureAwait(false);
        using var workbook = new XLWorkbook(_templateLocator.GetScheduleTemplatePath());

        var matrixTemplate = FindTemplateSheet(workbook, MatrixTemplateSheetNames)
            ?? throw new InvalidOperationException($"Matrix template sheet not found. Expected one of: {string.Join(", ", MatrixTemplateSheetNames)}");
        var statTemplate = FindTemplateSheet(workbook, StatisticTemplateSheetNames)
            ?? throw new InvalidOperationException($"Statistic template sheet not found. Expected one of: {string.Join(", ", StatisticTemplateSheetNames)}");

        var scheduleName = SanitizeWorksheetName(graphData.ScheduleContext.ScheduleName, "Schedule");
        var matrixSheet = matrixTemplate.CopyTo(scheduleName);

        var statisticNameBase = SanitizeWorksheetName($"{scheduleName} - Statistic", "Schedule - Statistic");
        var statisticName = MakeUniqueSheetName(workbook, statisticNameBase);
        var statSheet = statTemplate.CopyTo(statisticName);

        matrixTemplate.Delete();
        statTemplate.Delete();

        FillMatrixSheetFromTemplate(matrixSheet, graphData.ScheduleContext);
        FillStatisticSheetFromTemplate(statSheet, graphData.ScheduleContext);

        FixGray125Fills(matrixSheet, "B2:AA32");
        FixGray125Fills(statSheet);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), $"GF3_Graph_{graphId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
    }

    public async Task<(byte[] content, string fileName)> ExportContainerToXlsxAsync(int containerId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
    {
        var container = await _containerService.GetAsync(containerId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Container with id {containerId} was not found.");
        var graphs = await _containerService.GetGraphsAsync(containerId, ct).ConfigureAwait(false);

        var graphContexts = new List<GraphExcelContext>(graphs.Count);
        foreach (var graph in graphs)
            graphContexts.Add(await LoadGraphDataAsync(containerId, graph.Id, includeStyles, includeEmployees, ct).ConfigureAwait(false));

        var containerContext = _contextBuilder.BuildContainerContext(container, graphContexts);

        using var workbook = new XLWorkbook(_templateLocator.GetScheduleTemplatePath());
        using var containerWb = new XLWorkbook(_templateLocator.GetContainerTemplatePath());

        var matrixTemplate = FindTemplateSheet(workbook, MatrixTemplateSheetNames)
            ?? throw new InvalidOperationException($"Matrix template sheet not found. Expected one of: {string.Join(", ", MatrixTemplateSheetNames)}");
        var statTemplate = FindTemplateSheet(workbook, StatisticTemplateSheetNames)
            ?? throw new InvalidOperationException($"Statistic template sheet not found. Expected one of: {string.Join(", ", StatisticTemplateSheetNames)}");

        var containerSrc = FindTemplateSheet(containerWb, ContainerTemplateSheetNames)
            ?? throw new InvalidOperationException($"Container template sheet not found in ContainerTemplate.xlsx. Expected one of: {string.Join(", ", ContainerTemplateSheetNames)}");

        var containerTemplate = containerSrc.CopyTo(workbook, MakeUniqueSheetName(workbook, "__ContainerTemplate__"));
        var containerSheet = containerTemplate.CopyTo(MakeUniqueSheetName(workbook, SanitizeWorksheetName("Container", "Container")));
        FillContainerTemplateSheetFromTemplate(containerSheet, containerContext);

        foreach (var chart in containerContext.Charts)
        {
            ct.ThrowIfCancellationRequested();
            var scheduleName = MakeUniqueSheetName(workbook, SanitizeWorksheetName(chart.ChartName, "Schedule"));
            var matrixSheet = matrixTemplate.CopyTo(scheduleName);
            FillMatrixSheetFromTemplate(matrixSheet, chart.ScheduleContext);
            FixGray125Fills(matrixSheet, "B2:AA32");

            var statNameBase = SanitizeWorksheetName($"{scheduleName} - Statistic", "Schedule - Statistic");
            var statName = MakeUniqueSheetName(workbook, statNameBase);
            var statSheet = statTemplate.CopyTo(statName);
            FillStatisticSheetFromTemplate(statSheet, chart.ScheduleContext);
            FixGray125Fills(statSheet);
        }

        matrixTemplate.Delete();
        statTemplate.Delete();
        containerTemplate.Delete();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), $"GF3_Container_{containerId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
    }

    private async Task<GraphExcelContext> LoadGraphDataAsync(int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct)
    {
        var graph = await _containerService.GetGraphByIdAsync(containerId, graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");
        var slots = await _containerService.GetGraphSlotsAsync(containerId, graphId, ct).ConfigureAwait(false);
        var employees = includeEmployees
            ? await _containerService.GetGraphEmployeesAsync(containerId, graphId, ct).ConfigureAwait(false)
            : [];

        _ = includeStyles
            ? await _containerService.GetGraphCellStylesAsync(containerId, graphId, ct).ConfigureAwait(false)
            : [];

        var shop = await _shopService.GetAsync(graph.ShopId, ct).ConfigureAwait(false);
        var scheduleContext = _contextBuilder.BuildScheduleContext(graph, shop, employees, slots);
        return new GraphExcelContext(graph, shop, employees, slots, scheduleContext);
    }

    private static IXLWorksheet? FindTemplateSheet(XLWorkbook wb, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var ws = wb.Worksheets.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (ws is not null) return ws;
        }

        return null;
    }

    private static string MakeUniqueSheetName(XLWorkbook wb, string baseName)
    {
        var name = baseName;
        var i = 1;
        while (wb.Worksheets.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            var suffix = $" ({i++})";
            var trimmed = baseName;
            if (trimmed.Length + suffix.Length > 31)
                trimmed = trimmed[..(31 - suffix.Length)];
            name = trimmed + suffix;
        }

        return name;
    }

    private static string SanitizeWorksheetName(string? name, string fallback)
    {
        var safe = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        foreach (var c in new[] { '[', ']', '*', '?', '/', '\\', ':' })
            safe = safe.Replace(c, '_');
        return safe.Length > 31 ? safe[..31] : safe;
    }

    private static void FixGray125Fills(IXLWorksheet sheet, string? rangeAddress = null)
    {
        var range = !string.IsNullOrWhiteSpace(rangeAddress) ? sheet.Range(rangeAddress) : sheet.RangeUsed();
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

        foreach (var cf in sheet.ConditionalFormats)
        {
            if (cf.Style.Fill.PatternType == XLFillPatternValues.Gray125)
            {
                cf.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                cf.Style.Fill.BackgroundColor = XLColor.White;
                cf.Style.Fill.PatternColor = XLColor.Black;
            }
        }
    }

    private static void FillMatrixSheetFromTemplate(IXLWorksheet sheet, ScheduleExcelContext context)
    {
        sheet.Range(MatrixClearRange).Clear(XLClearOptions.Contents);
        sheet.Cell(M_ShopRow, M_ShopCol).Value = context.ShopName;

        var table = context.ScheduleMatrix?.Table;
        if (table is null) return;

        var visibleCols = table.Columns.Cast<DataColumn>()
            .Where(c => c.ColumnName != ScheduleMatrixConstants.ConflictColumnName && c.ColumnName != ScheduleMatrixConstants.WeekendColumnName)
            .ToList();

        var dayCol = visibleCols.FirstOrDefault(c => c.ColumnName == ScheduleMatrixConstants.DayColumnName)
                     ?? visibleCols.FirstOrDefault();

        var dataCols = visibleCols.Where(c => !ReferenceEquals(c, dayCol)).ToList();
        var capacity = M_LastDataCol - M_FirstDataCol + 1;
        if (dataCols.Count > capacity)
            throw new InvalidOperationException($"Template supports max {capacity} employee columns (C..AA). Current: {dataCols.Count}.");

        for (var i = 0; i < dataCols.Count; i++)
        {
            var col = dataCols[i];
            sheet.Cell(M_HeaderRow, M_FirstDataCol + i).Value = string.IsNullOrWhiteSpace(col.Caption) ? col.ColumnName : col.Caption;
        }

        for (var c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
            sheet.Cell(M_HeaderRow, c).Value = "0";

        var daysInMonth = DateTime.DaysInMonth(context.ScheduleYear, context.ScheduleMonth);
        var maxRows = Math.Min(M_DayCount, context.ScheduleMatrix.Count);

        for (var r = 0; r < M_DayCount; r++)
        {
            var excelRow = M_FirstDayRow + r;
            DataRowView? rowView = r < maxRows ? (DataRowView)context.ScheduleMatrix[r] : null;
            var dateCell = sheet.Cell(excelRow, M_DateCol);

            if (r < daysInMonth)
            {
                var date = rowView is not null ? TryBuildDate(context, dayCol, rowView) : null;
                date ??= SafeDate(context.ScheduleYear, context.ScheduleMonth, r + 1);
                dateCell.Value = date.HasValue ? date.Value : " ";
            }
            else
            {
                dateCell.Value = " ";
            }

            for (var i = 0; i < dataCols.Count; i++)
            {
                var cell = sheet.Cell(excelRow, M_FirstDataCol + i);
                if (rowView is null)
                {
                    cell.Value = "-";
                    continue;
                }

                SetDashIfEmpty(cell, rowView[dataCols[i].ColumnName]);
            }

            for (var c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
                sheet.Cell(excelRow, c).Value = "-";
        }

        sheet.PageSetup.SetRowsToRepeatAtTop(M_HeaderRow, M_HeaderRow);
        sheet.PageSetup.SetColumnsToRepeatAtLeft(M_DateCol, M_DateCol);
        sheet.PageSetup.PrintAreas.Clear();
        sheet.PageSetup.PrintAreas.Add(MatrixClearRange);
    }

    private static void SetDashIfEmpty(IXLCell cell, object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            cell.Value = "-";
            return;
        }

        var text = value.ToString()?.Trim() ?? string.Empty;
        cell.Value = string.IsNullOrWhiteSpace(text) ? "-" : text;
    }

    private static DateTime? TryBuildDate(ScheduleExcelContext context, DataColumn? dayCol, DataRowView rowView)
    {
        if (dayCol is null) return null;

        var raw = rowView[dayCol.ColumnName];
        if (raw is null || raw == DBNull.Value) return null;
        if (raw is DateTime dt) return dt.Date;
        if (raw is int d && d is >= 1 and <= 31)
            return SafeDate(context.ScheduleYear, context.ScheduleMonth, d);

        var s = raw.ToString()?.Trim();
        if (int.TryParse(s, out var parsed) && parsed is >= 1 and <= 31)
            return SafeDate(context.ScheduleYear, context.ScheduleMonth, parsed);

        if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDt))
            return parsedDt.Date;

        return null;
    }

    private static DateTime? SafeDate(int year, int month, int day)
    {
        try { return new DateTime(year, month, day); }
        catch { return null; }
    }

    private static void FillStatisticSheetFromTemplate(IXLWorksheet sheet, ScheduleExcelContext context)
    {
        sheet.Cell(2, S_ValueCol).Value = context.ScheduleName;
        sheet.Cell(3, S_ValueCol).Value = context.ScheduleMonth.ToString("D2", CultureInfo.InvariantCulture);
        sheet.Cell(4, S_ValueCol).Value = context.ScheduleYear.ToString(CultureInfo.InvariantCulture);
        sheet.Cell(5, S_ValueCol).Value = context.ShopName;
        sheet.Cell(6, S_ValueCol).Value = context.ShopAddress;
        sheet.Cell(7, S_ValueCol).Value = context.TotalHoursText;
        sheet.Cell(8, S_ValueCol).Value = context.TotalEmployees.ToString(CultureInfo.InvariantCulture);
        sheet.Cell(9, S_ValueCol).Value = context.TotalDays.ToString(CultureInfo.InvariantCulture);
        sheet.Cell(10, S_ValueCol).Value = context.Shift1;
        sheet.Cell(11, S_ValueCol).Value = context.Shift2;
        sheet.Cell(S_EmployeesLineRow, 1).Value = $"Total employees ({context.TotalEmployees}): {context.TotalEmployeesListText}";

        for (var i = 0; i < S_DaysCapacity; i++)
        {
            var col = S_FirstDayCol + i * 3;
            var text = i < context.SummaryDayHeaders.Count ? context.SummaryDayHeaders[i].Text : string.Empty;
            sheet.Cell(S_DayHeaderRow, col).Value = text;
        }

        var wfByEmployee = context.EmployeeWorkFreeStats
            .Where(x => !string.IsNullOrWhiteSpace(x.Employee))
            .GroupBy(x => x.Employee, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var summaryNeeded = context.SummaryRows.Count;
        var summaryExtra = Math.Max(0, summaryNeeded - S_MaxSummaryRows);
        var summaryLastCol = S_FirstDayCol + (S_DaysCapacity - 1) * 3 + 2;

        if (summaryExtra > 0)
        {
            var insertAfter = S_BodyFirstRow + S_MaxSummaryRows - 1;
            sheet.Row(insertAfter).InsertRowsBelow(summaryExtra);

            var templateRange = sheet.Range(insertAfter, 1, insertAfter, summaryLastCol);
            for (var k = 1; k <= summaryExtra; k++)
            {
                templateRange.CopyTo(sheet.Range(insertAfter + k, 1, insertAfter + k, summaryLastCol));
                sheet.Row(insertAfter + k).Height = sheet.Row(insertAfter).Height;
            }
        }

        var summaryTotalRows = Math.Max(S_MaxSummaryRows, summaryNeeded);

        for (var r = 0; r < summaryTotalRows; r++)
        {
            var row = S_BodyFirstRow + r;
            if (r >= summaryNeeded)
            {
                sheet.Cell(row, S_EmployeeCol).Clear(XLClearOptions.Contents);
                sheet.Cell(row, S_WorkDaysCol).Clear(XLClearOptions.Contents);
                sheet.Cell(row, S_FreeDaysCol).Clear(XLClearOptions.Contents);
                sheet.Cell(row, S_SumCol).Clear(XLClearOptions.Contents);
                for (var d = 0; d < S_DaysCapacity; d++)
                {
                    var c = S_FirstDayCol + d * 3;
                    sheet.Cell(row, c).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, c + 1).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, c + 2).Clear(XLClearOptions.Contents);
                }

                continue;
            }

            var summaryRow = context.SummaryRows[r];
            var employeeName = summaryRow.Employee ?? string.Empty;
            sheet.Cell(row, S_EmployeeCol).Value = employeeName;

            if (wfByEmployee.TryGetValue(employeeName, out var wf))
            {
                sheet.Cell(row, S_WorkDaysCol).Value = wf.WorkDays.ToString(CultureInfo.InvariantCulture);
                sheet.Cell(row, S_FreeDaysCol).Value = wf.FreeDays.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                sheet.Cell(row, S_WorkDaysCol).Clear(XLClearOptions.Contents);
                sheet.Cell(row, S_FreeDaysCol).Clear(XLClearOptions.Contents);
            }

            sheet.Cell(row, S_SumCol).Value = summaryRow.Sum ?? string.Empty;
            var dayCells = summaryRow.Days?.ToList() ?? [];
            for (var d = 0; d < S_DaysCapacity; d++)
            {
                var c = S_FirstDayCol + d * 3;
                var day = d < dayCells.Count ? dayCells[d] : null;
                sheet.Cell(row, c).Value = day?.From ?? string.Empty;
                sheet.Cell(row, c + 1).Value = day?.To ?? string.Empty;
                sheet.Cell(row, c + 2).Value = day?.Hours ?? string.Empty;
            }
        }
    }

    private static void FillContainerTemplateSheetFromTemplate(IXLWorksheet sheet, ContainerExcelContext context)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{Id}"] = context.ContainerId.ToString(CultureInfo.InvariantCulture),
            ["{Name}"] = context.ContainerName ?? string.Empty,
            ["{Note}"] = context.ContainerNote ?? string.Empty,
            ["{Container Name}"] = context.ContainerName ?? string.Empty,
            ["{Total Hours}"] = context.TotalHoursText ?? string.Empty,
            ["{Total Employees Count}"] = context.TotalEmployees.ToString(CultureInfo.InvariantCulture),
            ["{Total Shops Count}"] = context.TotalShops.ToString(CultureInfo.InvariantCulture),
            ["{Total Employees}"] = context.TotalEmployeesListText ?? string.Empty,
            ["{Total Shops}"] = context.TotalShopsListText ?? string.Empty
        };

        foreach (var cell in sheet.CellsUsed())
        {
            var s = cell.GetString();
            if (string.IsNullOrWhiteSpace(s) || !s.Contains('{')) continue;
            foreach (var kv in map)
                if (s.Contains(kv.Key, StringComparison.Ordinal))
                    s = s.Replace(kv.Key, kv.Value);
            cell.Value = s;
        }

        var headerRow = sheet.RowsUsed().Select(r => r.RowNumber()).FirstOrDefault(r =>
            string.Equals(sheet.Cell(r, 1).GetString(), "Employee", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(sheet.Cell(r, 4).GetString(), "Hours Sum", StringComparison.OrdinalIgnoreCase));
        if (headerRow <= 0) return;

        var templateRow = sheet.RowsUsed().Select(r => r.RowNumber()).FirstOrDefault(r =>
            sheet.Cell(r, 1).GetString().Contains("{Employee}", StringComparison.OrdinalIgnoreCase));
        if (templateRow <= 0) templateRow = headerRow + 1;

        var lastUsedCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 50;
        int firstShopCol = 0;
        for (var c = 1; c <= lastUsedCol; c++)
        {
            if (sheet.Cell(headerRow, c).GetString().Contains("{Shop}", StringComparison.OrdinalIgnoreCase))
            {
                firstShopCol = c;
                break;
            }
        }

        if (firstShopCol <= 0) return;

        var lastShopCol = firstShopCol;
        while (lastShopCol <= lastUsedCol && sheet.Cell(headerRow, lastShopCol).GetString().Contains("{Shop}", StringComparison.OrdinalIgnoreCase))
            lastShopCol++;
        lastShopCol--;

        var shopCapacity = lastShopCol - firstShopCol + 1;
        var shops = context.ShopHeaders?.ToList() ?? [];
        if (shops.Count > shopCapacity)
            throw new InvalidOperationException($"ContainerTemplate supports max {shopCapacity} shops. Add more {{Shop}} columns to the template.");

        for (var i = 0; i < shopCapacity; i++)
            sheet.Cell(headerRow, firstShopCol + i).Value = i < shops.Count ? shops[i].Name : string.Empty;

        var rows = context.EmployeeShopHoursRows?.ToList() ?? [];
        if (rows.Count > 1)
            rows = rows.OrderBy(x => string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase) ? 1 : 0).ToList();

        if (rows.Count == 0)
        {
            sheet.Range(templateRow, 1, templateRow, lastShopCol).Clear(XLClearOptions.Contents);
            return;
        }

        if (rows.Count > 1)
        {
            sheet.Row(templateRow).InsertRowsBelow(rows.Count - 1);
            var templateRange = sheet.Range(templateRow, 1, templateRow, lastShopCol);
            for (var i = 1; i < rows.Count; i++)
                templateRange.CopyTo(sheet.Range(templateRow + i, 1, templateRow + i, lastShopCol));
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var r = templateRow + i;
            var item = rows[i];
            sheet.Cell(r, 1).Value = item.Employee ?? string.Empty;
            sheet.Cell(r, 2).Value = item.WorkDays;
            sheet.Cell(r, 3).Value = item.FreeDays;
            sheet.Cell(r, 4).Value = item.HoursSum ?? "0";

            for (var s = 0; s < shopCapacity; s++)
            {
                if (s >= shops.Count)
                {
                    sheet.Cell(r, firstShopCol + s).Value = string.Empty;
                    continue;
                }

                var shopKey = shops[s].Key;
                item.HoursByShop.TryGetValue(shopKey, out var val);
                sheet.Cell(r, firstShopCol + s).Value = string.IsNullOrWhiteSpace(val) ? "0" : val;
            }
        }

        var lastDataRow = templateRow + rows.Count - 1;
        var endCol = firstShopCol + Math.Max(shops.Count, 1) - 1;
        var tableRange = sheet.Range(headerRow, 1, lastDataRow, endCol);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var totalIndex = rows.FindIndex(x => string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase));
        if (totalIndex >= 0)
        {
            var totalRow = templateRow + totalIndex;
            sheet.Row(totalRow).Style.Font.Bold = true;
            tableRange.Worksheet.Range(totalRow, 1, totalRow, tableRange.LastColumn().ColumnNumber()).Style.Border.TopBorder = XLBorderStyleValues.Medium;
        }
    }
}
