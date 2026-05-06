using System.Collections;
using System.Data;
using System.Reflection;
using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Services.Export;
using ClosedXML.Excel;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class ExportLayerDeepTests
{
    [Fact]
    public void ScheduleExcelContextBuilder_BuildScheduleContext_AppliesManualColumnsAndIgnoresTextOverridesInSummary()
    {
        var builder = new ScheduleExcelContextBuilder();
        var shop = TestDataFactory.CreateShopModel(id: 2, name: "Main Shop", address: "Address 1");
        var graph = TestDataFactory.CreateScheduleModel(id: 5, containerId: 1, shopId: shop.Id, name: "April Graph", year: 2026, month: 4, availabilityGroupId: null);
        graph.Note = CreatePortableNote(
            "Planner note",
            """
            {
              "manualColumns":[{"id":9,"label":"Special","cells":{"2":"Vacation"}}],
              "columnOrder":[-9,4,3],
              "textCells":[{"employeeId":3,"dayOfMonth":2,"value":"Training"}]
            }
            """);

        var john = new ScheduleEmployeeModel
        {
            Id = 14,
            ScheduleId = graph.Id,
            EmployeeId = 3,
            DisplayOrder = 1,
            MinHoursMonth = 80,
            Employee = TestDataFactory.CreateEmployeeModel(id: 3, firstName: "John", lastName: "Smith"),
        };
        var ann = new ScheduleEmployeeModel
        {
            Id = 15,
            ScheduleId = graph.Id,
            EmployeeId = 4,
            DisplayOrder = 0,
            MinHoursMonth = 60,
            Employee = TestDataFactory.CreateEmployeeModel(id: 4, firstName: "Ann", lastName: "Brown"),
        };

        var slots = new List<ScheduleSlotModel>
        {
            new()
            {
                ScheduleId = graph.Id,
                DayOfMonth = 1,
                SlotNo = 1,
                EmployeeId = 3,
                Status = SlotStatus.ASSIGNED,
                FromTime = "08:00",
                ToTime = "16:00",
            },
            new()
            {
                ScheduleId = graph.Id,
                DayOfMonth = 2,
                SlotNo = 1,
                EmployeeId = 4,
                Status = SlotStatus.ASSIGNED,
                FromTime = "12:00",
                ToTime = "16:00",
            },
        };

        var context = builder.BuildScheduleContext(graph, shop, [john, ann], slots);
        var table = Assert.IsType<DataTable>(context.ScheduleMatrix.Table);
        var manualColumn = FindColumnByCaption(table, "Special");
        var annColumn = FindColumnByCaption(table, "Ann Brown");
        var johnColumn = FindColumnByCaption(table, "John Smith");

        Assert.Equal("12h 0m", context.TotalHoursText);
        Assert.Equal(2, context.TotalEmployees);
        Assert.Equal(30, context.TotalDays);
        Assert.Equal("Special", table.Columns[3].Caption);
        Assert.Equal("Ann Brown", table.Columns[4].Caption);
        Assert.Equal("John Smith", table.Columns[5].Caption);
        Assert.Equal("Vacation", table.Rows[1][manualColumn]?.ToString());
        Assert.Equal("Training", table.Rows[1][johnColumn]?.ToString());
        Assert.Equal("12:00 - 16:00", table.Rows[1][annColumn]?.ToString());

        var johnSummary = Assert.Single(context.SummaryRows, row => row.Employee == "John Smith");
        var annSummary = Assert.Single(context.SummaryRows, row => row.Employee == "Ann Brown");

        Assert.Equal(1, johnSummary.WorkDays);
        Assert.Equal(29, johnSummary.FreeDays);
        Assert.Equal("8", johnSummary.Sum);
        Assert.Equal(string.Empty, johnSummary.Days[1].From);
        Assert.Equal(string.Empty, johnSummary.Days[1].Hours);

        Assert.Equal(1, annSummary.WorkDays);
        Assert.Equal(29, annSummary.FreeDays);
        Assert.Equal("4", annSummary.Sum);
        Assert.Equal("12:00", annSummary.Days[1].From);
        Assert.Equal("16:00", annSummary.Days[1].To);
        Assert.Equal("4", annSummary.Days[1].Hours);
    }

    [Fact]
    public void ScheduleExcelContextBuilder_BuildContainerContext_AggregatesRowsAcrossShopsAndAppendsTotalRow()
    {
        var builder = new ScheduleExcelContextBuilder();
        var container = new ContainerModel { Id = 1, Name = "Main Container", Note = "Shared" };

        var johnEmployee = TestDataFactory.CreateEmployeeModel(id: 3, firstName: "John", lastName: "Smith");
        var annEmployee = TestDataFactory.CreateEmployeeModel(id: 4, firstName: "Ann", lastName: "Brown");

        var alphaShop = TestDataFactory.CreateShopModel(id: 2, name: "Alpha Shop", address: "Address A");
        var alphaGraph = TestDataFactory.CreateScheduleModel(id: 5, containerId: container.Id, shopId: alphaShop.Id, name: "Alpha Graph", year: 2026, month: 4, availabilityGroupId: null);
        var alphaEmployees = new List<ScheduleEmployeeModel>
        {
            new() { Id = 11, ScheduleId = alphaGraph.Id, EmployeeId = johnEmployee.Id, Employee = johnEmployee, DisplayOrder = 0, MinHoursMonth = 80 },
            new() { Id = 12, ScheduleId = alphaGraph.Id, EmployeeId = annEmployee.Id, Employee = annEmployee, DisplayOrder = 1, MinHoursMonth = 40 },
        };
        var alphaSlots = new List<ScheduleSlotModel>
        {
            new() { ScheduleId = alphaGraph.Id, DayOfMonth = 1, SlotNo = 1, EmployeeId = johnEmployee.Id, Employee = johnEmployee, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "16:00" },
            new() { ScheduleId = alphaGraph.Id, DayOfMonth = 2, SlotNo = 1, EmployeeId = annEmployee.Id, Employee = annEmployee, Status = SlotStatus.ASSIGNED, FromTime = "12:00", ToTime = "16:00" },
        };
        var alphaContext = builder.BuildScheduleContext(alphaGraph, alphaShop, alphaEmployees, alphaSlots);

        var betaShop = TestDataFactory.CreateShopModel(id: 7, name: "Beta Shop", address: "Address B");
        var betaGraph = TestDataFactory.CreateScheduleModel(id: 6, containerId: container.Id, shopId: betaShop.Id, name: "Beta Graph", year: 2026, month: 4, availabilityGroupId: null);
        var betaEmployees = new List<ScheduleEmployeeModel>
        {
            new() { Id = 13, ScheduleId = betaGraph.Id, EmployeeId = johnEmployee.Id, Employee = johnEmployee, DisplayOrder = 0, MinHoursMonth = 80 },
        };
        var betaSlots = new List<ScheduleSlotModel>
        {
            new() { ScheduleId = betaGraph.Id, DayOfMonth = 3, SlotNo = 1, EmployeeId = johnEmployee.Id, Employee = johnEmployee, Status = SlotStatus.ASSIGNED, FromTime = "16:00", ToTime = "20:00" },
        };
        var betaContext = builder.BuildScheduleContext(betaGraph, betaShop, betaEmployees, betaSlots);

        var context = builder.BuildContainerContext(
            container,
            [
                new GraphExcelContext(alphaGraph, alphaShop, alphaEmployees, alphaSlots, alphaContext),
                new GraphExcelContext(betaGraph, betaShop, betaEmployees, betaSlots, betaContext),
            ]);

        Assert.Equal(2, context.TotalEmployees);
        Assert.Equal(2, context.TotalShops);
        Assert.Equal("16h 0m", context.TotalHoursText);
        Assert.Equal(new[] { "Alpha Shop", "Beta Shop" }, context.ShopHeaders.Select(header => header.Name).ToArray());

        var annRow = Assert.Single(context.EmployeeShopHoursRows, row => row.Employee == "Ann Brown");
        var johnRow = Assert.Single(context.EmployeeShopHoursRows, row => row.Employee == "John Smith");
        var totalRow = Assert.Single(context.EmployeeShopHoursRows, row => row.Employee == "TOTAL");

        Assert.Equal("4h 0m", annRow.HoursSum);
        Assert.Equal("4h 0m", annRow.HoursByShop["2"]);
        Assert.False(annRow.HoursByShop.ContainsKey("7"));

        Assert.Equal("12h 0m", johnRow.HoursSum);
        Assert.Equal("8h 0m", johnRow.HoursByShop["2"]);
        Assert.Equal("4h 0m", johnRow.HoursByShop["7"]);

        Assert.Equal("16h 0m", totalRow.HoursSum);
        Assert.Equal("12h 0m", totalRow.HoursByShop["2"]);
        Assert.Equal("4h 0m", totalRow.HoursByShop["7"]);
    }

    [Fact]
    public void ScheduleExportDataBuilder_BuildSqlData_OrdersCollectionsAndMapsAllFields()
    {
        var builder = new ScheduleExportDataBuilder();
        var schedule = TestDataFactory.CreateScheduleModel(id: 5, containerId: 1, shopId: 2, name: "Export Graph", year: 2026, month: 4, availabilityGroupId: 11);
        schedule.Note = "Export note";

        var employees = new List<ScheduleEmployeeModel>
        {
            new() { Id = 2, ScheduleId = schedule.Id, EmployeeId = 10, DisplayOrder = 2, MinHoursMonth = null },
            new() { Id = 1, ScheduleId = schedule.Id, EmployeeId = 5, DisplayOrder = 0, MinHoursMonth = 80 },
        };
        var slots = new List<ScheduleSlotModel>
        {
            new() { Id = 21, ScheduleId = schedule.Id, DayOfMonth = 3, SlotNo = 2, EmployeeId = 10, Status = SlotStatus.ASSIGNED, FromTime = "16:00", ToTime = "20:00" },
            new() { Id = 20, ScheduleId = schedule.Id, DayOfMonth = 1, SlotNo = 1, EmployeeId = 5, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "16:00" },
        };
        var styles = new List<ScheduleCellStyleModel>
        {
            new() { Id = 31, ScheduleId = schedule.Id, EmployeeId = 5, DayOfMonth = 1, BackgroundColorArgb = 10, TextColorArgb = 20 },
        };

        var availabilityGroup = new AvailabilityGroupModel { Id = 11, Name = "April Availability", Year = 2026, Month = 4 };
        var members = new List<AvailabilityGroupMemberModel>
        {
            new() { Id = 42, AvailabilityGroupId = availabilityGroup.Id, EmployeeId = 10, DisplayOrder = 2 },
            new() { Id = 41, AvailabilityGroupId = availabilityGroup.Id, EmployeeId = 5, DisplayOrder = 0 },
        };
        var days = new List<AvailabilityGroupDayModel>
        {
            new() { Id = 52, AvailabilityGroupMemberId = 42, DayOfMonth = 3, Kind = AvailabilityKind.ANY, IntervalStr = null },
            new() { Id = 51, AvailabilityGroupMemberId = 41, DayOfMonth = 1, Kind = AvailabilityKind.INT, IntervalStr = "08:00 - 16:00" },
        };

        var exportData = builder.BuildSqlData(schedule, employees, slots, styles, availabilityGroup, members, days);

        Assert.Equal(schedule.Id, exportData.Schedule.Id);
        Assert.Equal(schedule.ContainerId, exportData.Schedule.ContainerId);
        Assert.Equal(schedule.ShopId, exportData.Schedule.ShopId);
        Assert.Equal(schedule.Note, exportData.Schedule.Note);
        Assert.Equal(new[] { 5, 10 }, exportData.Employees.Select(employee => employee.EmployeeId).ToArray());
        Assert.Equal(new[] { 0, 2 }, exportData.Employees.Select(employee => employee.DisplayOrder).ToArray());
        Assert.Equal(new[] { 1, 3 }, exportData.Slots.Select(slot => slot.DayOfMonth).ToArray());
        Assert.Equal("ASSIGNED", exportData.Slots[0].Status);
        Assert.Equal(0, exportData.Employees[1].MinHoursMonth);
        Assert.Single(exportData.CellStyles);
        Assert.Equal(10, exportData.CellStyles[0].BackgroundArgb);
        Assert.Equal(20, exportData.CellStyles[0].ForegroundArgb);
        Assert.NotNull(exportData.AvailabilityGroup);
        Assert.Equal("April Availability", exportData.AvailabilityGroup!.Name);
        Assert.Equal(new[] { 5, 10 }, exportData.AvailabilityMembers.Select(member => member.EmployeeId).ToArray());
        Assert.Equal(new[] { 1, 3 }, exportData.AvailabilityDays.Select(day => day.DayOfMonth).ToArray());
        Assert.Equal("INT", exportData.AvailabilityDays[0].Kind);
    }

    [Fact]
    public async Task GraphTemplateExportService_ExportGraphToXlsxAsync_GeneratesWorkbookFromTemplatesAndNormalizesLegacyEmployeeIds()
    {
        using var templates = new TempExcelTemplates();
        var fixture = TemplateExportFixture.CreateRich(useLegacyPrimarySlotEmployeeId: true);
        var service = fixture.CreateService(templates);

        var (content, fileName) = await service.ExportGraphToXlsxAsync(
            fixture.Container.Id,
            fixture.PrimaryGraph.Id,
            includeStyles: true,
            includeEmployees: true,
            CancellationToken.None);

        using var workbook = OpenWorkbook(content);
        var matrixSheet = workbook.Worksheet("April Graph");
        var statisticSheet = workbook.Worksheet("April Graph - Statistic");
        var worksheetNames = workbook.Worksheets.Select(worksheet => worksheet.Name).ToList();

        Assert.StartsWith("GF3_Graph_5_", fileName, StringComparison.Ordinal);
        Assert.EndsWith(".xlsx", fileName, StringComparison.Ordinal);
        Assert.DoesNotContain("MatrixTemplate", worksheetNames);
        Assert.DoesNotContain("StatisticTemplate", worksheetNames);
        Assert.Equal("Main Shop", matrixSheet.Cell("B1").GetString());
        Assert.Equal("John Smith", matrixSheet.Cell("C1").GetString());
        Assert.Equal(new DateTime(2026, 4, 1), matrixSheet.Cell("B2").GetDateTime().Date);
        Assert.Equal("08:00 - 16:00", matrixSheet.Cell("C2").GetString());
        Assert.Equal("April Graph 2", matrixSheet.Cell("C3").GetString());
        Assert.Equal("April Graph", statisticSheet.Cell("B2").GetString());
        Assert.Equal("Main Shop", statisticSheet.Cell("B5").GetString());
        Assert.Equal("8h 0m", statisticSheet.Cell("B7").GetString());
        Assert.Contains("John Smith", statisticSheet.Cell("A12").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GraphTemplateExportService_ExportContainerToXlsxAsync_GeneratesContainerSummaryAndChartSheets()
    {
        using var templates = new TempExcelTemplates();
        var fixture = TemplateExportFixture.CreateRich(useLegacyPrimarySlotEmployeeId: false);
        var service = fixture.CreateService(templates);

        var (content, fileName) = await service.ExportContainerToXlsxAsync(
            fixture.Container.Id,
            includeStyles: true,
            includeEmployees: true,
            CancellationToken.None);

        using var workbook = OpenWorkbook(content);
        var containerSheet = workbook.Worksheet("Container");
        var worksheetNames = workbook.Worksheets.Select(worksheet => worksheet.Name).ToList();

        Assert.StartsWith("GF3_Container_1_", fileName, StringComparison.Ordinal);
        Assert.EndsWith(".xlsx", fileName, StringComparison.Ordinal);
        Assert.Contains("Container", worksheetNames);
        Assert.Contains("April Graph", worksheetNames);
        Assert.Contains("April Graph - Statistic", worksheetNames);
        Assert.Contains("April Graph 2", worksheetNames);
        Assert.Contains("April Graph 2 - Statistic", worksheetNames);
        Assert.Equal("Main Container", containerSheet.Cell("A1").GetString());
        Assert.Equal("16h 0m", containerSheet.Cell("A2").GetString());
        Assert.Equal("Main Shop", containerSheet.Cell("E5").GetString());
        Assert.Equal("Second Shop", containerSheet.Cell("F5").GetString());
        Assert.Equal("John Smith", containerSheet.Cell("A6").GetString());
        Assert.Equal("16h 0m", containerSheet.Cell("D6").GetString());
        Assert.Equal("8h 0m", containerSheet.Cell("E6").GetString());
        Assert.Equal("8h 0m", containerSheet.Cell("F6").GetString());
        Assert.Equal("TOTAL", containerSheet.Cell("A7").GetString());
        Assert.Equal("16h 0m", containerSheet.Cell("D7").GetString());
    }

    [Fact]
    public async Task GraphTemplateExportService_ExportContainerToXlsxAsync_ClearsTemplateRow_WhenContainerHasNoEmployees()
    {
        using var templates = new TempExcelTemplates();
        var fixture = TemplateExportFixture.CreateEmpty();
        var service = fixture.CreateService(templates);

        var (content, _) = await service.ExportContainerToXlsxAsync(
            fixture.Container.Id,
            includeStyles: false,
            includeEmployees: false,
            CancellationToken.None);

        using var workbook = OpenWorkbook(content);
        var containerSheet = workbook.Worksheet("Container");

        Assert.Equal("Empty Container", containerSheet.Cell("A1").GetString());
        Assert.Equal("0h 0m", containerSheet.Cell("A2").GetString());
        Assert.Equal(string.Empty, containerSheet.Cell("A6").GetString());
        Assert.Equal(string.Empty, containerSheet.Cell("D6").GetString());
        Assert.Equal(string.Empty, containerSheet.Cell("E6").GetString());
        Assert.Equal(string.Empty, containerSheet.Cell("F6").GetString());
    }

    [Fact]
    public void GraphNoteExportMetadataParser_Parse_LegacyPayloadFiltersInvalidEntries()
    {
        var note =
            """
            Visible note

            <!--GF3_GRAPH_META:{"manualColumns":[{"id":7,"label":"Notes","cells":{"1":"Call","32":"skip"}},{"id":7,"label":"Dup"}],"columnOrder":[5,5,0,-7],"textCells":[{"employeeId":3,"dayOfMonth":2,"value":"Vacation"},{"employeeId":3,"dayOfMonth":2,"value":"Dup"},{"employeeId":4,"dayOfMonth":40,"value":"skip"},{"employeeId":5,"dayOfMonth":3,"value":"-"}]}-->
            """;

        var metadata = ParseGraphNoteMetadata(note);
        var manualColumns = ReadManualColumns(metadata);
        var columnOrder = ReadIntList(metadata, "ColumnOrder");
        var textCells = ReadTextCells(metadata);

        var manualColumn = Assert.Single(manualColumns);
        Assert.Equal(7, manualColumn.Id);
        Assert.Equal("Notes", manualColumn.Label);
        Assert.Equal("Call", manualColumn.Cells[1]);
        Assert.Equal(new[] { 5, -7 }, columnOrder);

        var textCell = Assert.Single(textCells);
        Assert.Equal(3, textCell.EmployeeId);
        Assert.Equal(2, textCell.DayOfMonth);
        Assert.Equal("Vacation", textCell.Value);
    }

    [Fact]
    public void GraphNoteExportMetadataParser_Parse_PortableBase64PayloadSupportsCompactShape()
    {
        var note = CreatePortableNote(
            "Portable note",
            """
            {
              "m":[[9,"Extra",{"2":"WFH"}]],
              "o":[-9,4,3],
              "t":[[4,6,"Clinic"]]
            }
            """);

        var metadata = ParseGraphNoteMetadata(note);
        var manualColumns = ReadManualColumns(metadata);
        var columnOrder = ReadIntList(metadata, "ColumnOrder");
        var textCells = ReadTextCells(metadata);

        var manualColumn = Assert.Single(manualColumns);
        Assert.Equal(9, manualColumn.Id);
        Assert.Equal("Extra", manualColumn.Label);
        Assert.Equal("WFH", manualColumn.Cells[2]);
        Assert.Equal(new[] { -9, 4, 3 }, columnOrder);

        var textCell = Assert.Single(textCells);
        Assert.Equal(4, textCell.EmployeeId);
        Assert.Equal(6, textCell.DayOfMonth);
        Assert.Equal("Clinic", textCell.Value);
    }

    [Fact]
    public void GraphNoteSqlExportBuilder_BuildPortableNote_AppendsCompactMetadataWhenMissing()
    {
        var result = BuildPortableGraphNote(
            "Visible note",
            CreateInternalTextCellList((3, 2, "Vacation")));

        Assert.StartsWith("Visible note", result, StringComparison.Ordinal);
        Assert.Contains("[[GF3_GRAPH_META:b64:", result, StringComparison.Ordinal);

        var parsedMetadata = ParseGraphNoteMetadata(result);
        var textCells = ReadTextCells(parsedMetadata);
        var textCell = Assert.Single(textCells);

        Assert.Equal(3, textCell.EmployeeId);
        Assert.Equal(2, textCell.DayOfMonth);
        Assert.Equal("Vacation", textCell.Value);
    }

    [Fact]
    public void GraphNoteSqlExportBuilder_BuildPortableNote_ConvertsLegacyMetadataAndReplacesLegacyTextCells()
    {
        var legacyNote =
            """
            Visible note

            <!--GF3_GRAPH_META:{"manualColumns":[{"id":1,"label":"Old","cells":{"1":"X"}}],"textCells":[{"employeeId":1,"dayOfMonth":1,"value":"Old"}]}-->
            """;

        var result = BuildPortableGraphNote(
            legacyNote,
            CreateInternalTextCellList((3, 4, "Remote")));

        Assert.StartsWith("Visible note", result, StringComparison.Ordinal);
        Assert.Contains("[[GF3_GRAPH_META:b64:", result, StringComparison.Ordinal);
        Assert.DoesNotContain("<!--GF3_GRAPH_META:", result, StringComparison.Ordinal);

        var parsedMetadata = ParseGraphNoteMetadata(result);
        var manualColumn = Assert.Single(ReadManualColumns(parsedMetadata));
        var textCell = Assert.Single(ReadTextCells(parsedMetadata));

        Assert.Equal(1, manualColumn.Id);
        Assert.Equal("Old", manualColumn.Label);
        Assert.Equal("X", manualColumn.Cells[1]);
        Assert.Equal(3, textCell.EmployeeId);
        Assert.Equal(4, textCell.DayOfMonth);
        Assert.Equal("Remote", textCell.Value);
    }

    [Fact]
    public void GraphRelatedScheduleHintExportBuilder_BuildTextCells_MergesDistinctGraphNamesAndSkipsBusyCells()
    {
        var currentGraph = TestDataFactory.CreateScheduleModel(id: 5, containerId: 1, shopId: 2, name: "Current", year: 2026, month: 4, availabilityGroupId: null);
        var john = new ScheduleEmployeeModel { Id = 11, ScheduleId = currentGraph.Id, EmployeeId = 3, Employee = TestDataFactory.CreateEmployeeModel(id: 3, firstName: "John", lastName: "Smith") };
        var ann = new ScheduleEmployeeModel { Id = 12, ScheduleId = currentGraph.Id, EmployeeId = 4, Employee = TestDataFactory.CreateEmployeeModel(id: 4, firstName: "Ann", lastName: "Brown") };

        var currentSlots = new List<ScheduleSlotModel>
        {
            new() { ScheduleId = currentGraph.Id, DayOfMonth = 1, SlotNo = 1, EmployeeId = 3, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "16:00" },
        };
        var persistedTextCells = CreateInternalTextCellList((4, 2, "Manual"));
        var relatedSources = CreateInternalHintSourceList(
            (TestDataFactory.CreateScheduleModel(id: 6, containerId: 1, shopId: 2, name: "Zulu", year: 2026, month: 4, availabilityGroupId: null),
                new List<ScheduleSlotModel>
                {
                    new() { ScheduleId = 6, DayOfMonth = 1, SlotNo = 1, EmployeeId = 3, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "16:00" },
                    new() { ScheduleId = 6, DayOfMonth = 2, SlotNo = 1, EmployeeId = 3, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "16:00" },
                    new() { ScheduleId = 6, DayOfMonth = 2, SlotNo = 2, EmployeeId = 3, Status = SlotStatus.ASSIGNED, FromTime = "16:00", ToTime = "20:00" },
                    new() { ScheduleId = 6, DayOfMonth = 2, SlotNo = 3, EmployeeId = 4, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "12:00" },
                }),
            (TestDataFactory.CreateScheduleModel(id: 7, containerId: 1, shopId: 2, name: "Alpha", year: 2026, month: 4, availabilityGroupId: null),
                new List<ScheduleSlotModel>
                {
                    new() { ScheduleId = 7, DayOfMonth = 2, SlotNo = 1, EmployeeId = 3, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "16:00" },
                    new() { ScheduleId = 7, DayOfMonth = 2, SlotNo = 2, EmployeeId = 99, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "12:00" },
                }),
            (TestDataFactory.CreateScheduleModel(id: 8, containerId: 1, shopId: 2, name: "Ignored", year: 2026, month: 5, availabilityGroupId: null),
                new List<ScheduleSlotModel>
                {
                    new() { ScheduleId = 8, DayOfMonth = 2, SlotNo = 1, EmployeeId = 3, Status = SlotStatus.ASSIGNED, FromTime = "08:00", ToTime = "16:00" },
                }));

        var result = BuildDynamicHintTextCells(
            currentGraph,
            [john, ann],
            currentSlots,
            persistedTextCells,
            relatedSources);

        var textCell = Assert.Single(ReadTextCellSequence(result));
        Assert.Equal(3, textCell.EmployeeId);
        Assert.Equal(2, textCell.DayOfMonth);
        Assert.Equal("Alpha, Zulu", textCell.Value);
    }

    [Fact]
    public void GraphRelatedScheduleHintExportBuilder_MergeTextCells_PrefersPersistedFirstAndDropsInvalidOrDuplicateCells()
    {
        var result = MergeHintTextCells(
            CreateInternalTextCellList((3, 2, "Manual"), (0, 5, "skip"), (4, 1, "Sick")),
            CreateInternalTextCellList((3, 2, "Hint"), (4, 1, "Hint2"), (5, 3, "Travel"), (6, 0, "bad")));

        var textCells = ReadTextCellSequence(result);

        Assert.Equal(3, textCells.Count);
        Assert.Equal((3, 2, "Manual"), (textCells[0].EmployeeId, textCells[0].DayOfMonth, textCells[0].Value));
        Assert.Equal((4, 1, "Sick"), (textCells[1].EmployeeId, textCells[1].DayOfMonth, textCells[1].Value));
        Assert.Equal((5, 3, "Travel"), (textCells[2].EmployeeId, textCells[2].DayOfMonth, textCells[2].Value));
    }

    private static DataColumn FindColumnByCaption(DataTable table, string caption)
        => table.Columns.Cast<DataColumn>().Single(column => string.Equals(column.Caption, caption, StringComparison.Ordinal));

    private static XLWorkbook OpenWorkbook(byte[] content)
        => new(new MemoryStream(content));

    private static string CreatePortableNote(string visibleNote, string json)
        => $"{visibleNote}\n\n[[GF3_GRAPH_META:b64:{EncodeBase64Url(json)}]]";

    private static string EncodeBase64Url(string payload)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static object ParseGraphNoteMetadata(string note)
        => ReflectionTestHelper.InvokeStatic(ExportType("GraphNoteExportMetadataParser"), "Parse", note)
           ?? throw new InvalidOperationException("Could not parse graph note metadata.");

    private static string BuildPortableGraphNote(string note, object internalTextCellList)
        => (string)(ReflectionTestHelper.InvokeStatic(ExportType("GraphNoteSqlExportBuilder"), "BuildPortableNote", note, internalTextCellList)
            ?? throw new InvalidOperationException("Could not build portable graph note."));

    private static object BuildDynamicHintTextCells(
        ScheduleModel currentGraph,
        IReadOnlyList<ScheduleEmployeeModel> currentEmployees,
        IReadOnlyList<ScheduleSlotModel> currentSlots,
        object persistedTextCells,
        object relatedSources)
        => ReflectionTestHelper.InvokeStatic(
               ExportType("GraphRelatedScheduleHintExportBuilder"),
               "BuildTextCells",
               currentGraph,
               currentEmployees,
               currentSlots,
               persistedTextCells,
               relatedSources)
           ?? throw new InvalidOperationException("Could not build dynamic hint text cells.");

    private static object MergeHintTextCells(object persistedTextCells, object dynamicTextCells)
        => ReflectionTestHelper.InvokeStatic(
               ExportType("GraphRelatedScheduleHintExportBuilder"),
               "MergeTextCells",
               persistedTextCells,
               dynamicTextCells)
           ?? throw new InvalidOperationException("Could not merge hint text cells.");

    private static Type ExportType(string typeName)
        => ReflectionTestHelper.GetType("BusinessLogicLayer", $"BusinessLogicLayer.Services.Export.{typeName}");

    private static object CreateInternalTextCellList(params (int employeeId, int dayOfMonth, string value)[] cells)
    {
        var textCellType = ExportType("GraphNoteExportTextCell");
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(textCellType))!;

        foreach (var (employeeId, dayOfMonth, value) in cells)
        {
            list.Add(ReflectionTestHelper.Create(textCellType, employeeId, dayOfMonth, value));
        }

        return list;
    }

    private static object CreateInternalHintSourceList(params (ScheduleModel graph, IReadOnlyList<ScheduleSlotModel> slots)[] sources)
    {
        var sourceType = ExportType("GraphRelatedScheduleHintSource");
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(sourceType))!;

        foreach (var (graph, slots) in sources)
        {
            list.Add(ReflectionTestHelper.Create(sourceType, graph, slots));
        }

        return list;
    }

    private static List<int> ReadIntList(object instance, string propertyName)
        => ((IEnumerable)(GetPropertyValue(instance, propertyName) ?? Array.Empty<int>()))
            .Cast<object>()
            .Select(value => Convert.ToInt32(value))
            .ToList();

    private static List<GraphNoteManualColumnView> ReadManualColumns(object metadata)
    {
        return ((IEnumerable)(GetPropertyValue(metadata, "ManualColumns") ?? Array.Empty<object>()))
            .Cast<object>()
            .Select(column => new GraphNoteManualColumnView(
                Convert.ToInt32(GetPropertyValue(column, "Id")),
                Convert.ToString(GetPropertyValue(column, "Label")) ?? string.Empty,
                ReadDictionary(GetPropertyValue(column, "Cells"))))
            .ToList();
    }

    private static List<GraphNoteTextCellView> ReadTextCells(object metadata)
    {
        return ((IEnumerable)(GetPropertyValue(metadata, "TextCells") ?? Array.Empty<object>()))
            .Cast<object>()
            .Select(textCell => new GraphNoteTextCellView(
                Convert.ToInt32(GetPropertyValue(textCell, "EmployeeId")),
                Convert.ToInt32(GetPropertyValue(textCell, "DayOfMonth")),
                Convert.ToString(GetPropertyValue(textCell, "Value")) ?? string.Empty))
            .ToList();
    }

    private static List<GraphNoteTextCellView> ReadTextCellSequence(object sequence)
    {
        return ((IEnumerable)sequence)
            .Cast<object>()
            .Select(textCell => new GraphNoteTextCellView(
                Convert.ToInt32(GetPropertyValue(textCell, "EmployeeId")),
                Convert.ToInt32(GetPropertyValue(textCell, "DayOfMonth")),
                Convert.ToString(GetPropertyValue(textCell, "Value")) ?? string.Empty))
            .ToList();
    }

    private static Dictionary<int, string> ReadDictionary(object? source)
    {
        var result = new Dictionary<int, string>();
        if (source is not IEnumerable entries)
            return result;

        foreach (var entry in entries.Cast<object>())
        {
            var key = Convert.ToInt32(GetPropertyValue(entry, "Key"));
            var value = Convert.ToString(GetPropertyValue(entry, "Value")) ?? string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(instance);

    private sealed record GraphNoteManualColumnView(int Id, string Label, Dictionary<int, string> Cells);

    private sealed record GraphNoteTextCellView(int EmployeeId, int DayOfMonth, string Value);

    private sealed class TempExcelTemplates : IDisposable
    {
        private readonly string _directory;

        public TempExcelTemplates()
        {
            _directory = Path.Combine(AppContext.BaseDirectory, "excel-template-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);

            ScheduleTemplatePath = Path.Combine(_directory, "ScheduleTemplate.xlsx");
            ContainerTemplatePath = Path.Combine(_directory, "ContainerTemplate.xlsx");

            CreateScheduleTemplate(ScheduleTemplatePath);
            CreateContainerTemplate(ContainerTemplatePath);
        }

        public string ScheduleTemplatePath { get; }

        public string ContainerTemplatePath { get; }

        public void Dispose()
        {
            if (!Directory.Exists(_directory))
                return;

            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for test artifacts.
            }
        }

        private static void CreateScheduleTemplate(string path)
        {
            using var workbook = new XLWorkbook();

            var matrix = workbook.AddWorksheet("MatrixTemplate");
            matrix.Cell("B1").Style.Fill.PatternType = XLFillPatternValues.Gray125;
            matrix.Cell("AA32").Value = "tail";

            var statistic = workbook.AddWorksheet("StatisticTemplate");
            statistic.Cell(16, 1).Value = "row";
            statistic.Cell(25, 97).Value = "tail";

            workbook.SaveAs(path);
        }

        private static void CreateContainerTemplate(string path)
        {
            using var workbook = new XLWorkbook();

            var sheet = workbook.AddWorksheet("ContainerTemplate");
            sheet.Cell("A1").Value = "{Container Name}";
            sheet.Cell("A2").Value = "{Total Hours}";
            sheet.Cell("A3").Value = "{Total Employees Count}";
            sheet.Cell("A4").Value = "{Total Shops Count}";
            sheet.Cell("A5").Value = "Employee";
            sheet.Cell("B5").Value = "Work Days";
            sheet.Cell("C5").Value = "Free Days";
            sheet.Cell("D5").Value = "Hours Sum";
            sheet.Cell("E5").Value = "{Shop}";
            sheet.Cell("F5").Value = "{Shop}";
            sheet.Cell("A6").Value = "{Employee}";
            sheet.Cell("B6").Value = "{WorkDays}";
            sheet.Cell("C6").Value = "{FreeDays}";
            sheet.Cell("D6").Value = "{HoursSum}";
            sheet.Cell("E6").Value = "{Shop}";
            sheet.Cell("F6").Value = "{Shop}";

            workbook.SaveAs(path);
        }
    }

    private sealed class TemplateExportFixture
    {
        public required ContainerModel Container { get; init; }
        public required ScheduleModel PrimaryGraph { get; init; }
        public required TemplateContainerServiceStub ContainerService { get; init; }
        public required TemplateShopServiceStub ShopService { get; init; }

        public GraphTemplateExportService CreateService(TempExcelTemplates templates)
            => new(
                ContainerService,
                ShopService,
                new ExcelTemplateLocatorStub(templates.ScheduleTemplatePath, templates.ContainerTemplatePath),
                new ScheduleExcelContextBuilder());

        public static TemplateExportFixture CreateRich(bool useLegacyPrimarySlotEmployeeId)
        {
            var container = new ContainerModel { Id = 1, Name = "Main Container", Note = "Container note" };
            var shop = new ShopModel { Id = 2, Name = "Main Shop", Address = "Address 1", Description = "Shop desc" };
            var secondShop = new ShopModel { Id = 7, Name = "Second Shop", Address = "Address 2", Description = "Backup shop" };
            var employee = new EmployeeModel { Id = 3, FirstName = "John", LastName = "Smith", Email = "john@example.com", Phone = "123" };

            var primaryGraph = TestDataFactory.CreateScheduleModel(id: 5, containerId: container.Id, shopId: shop.Id, name: "April Graph", year: 2026, month: 4, availabilityGroupId: null);
            primaryGraph.Note = "Portable note";
            var relatedGraph = TestDataFactory.CreateScheduleModel(id: 6, containerId: container.Id, shopId: secondShop.Id, name: "April Graph 2", year: 2026, month: 4, availabilityGroupId: null);

            var primaryScheduleEmployee = new ScheduleEmployeeModel
            {
                Id = 14,
                ScheduleId = primaryGraph.Id,
                EmployeeId = employee.Id,
                Employee = employee,
                DisplayOrder = 0,
                MinHoursMonth = 80,
            };
            var relatedScheduleEmployee = new ScheduleEmployeeModel
            {
                Id = 15,
                ScheduleId = relatedGraph.Id,
                EmployeeId = employee.Id,
                Employee = employee,
                DisplayOrder = 0,
                MinHoursMonth = 80,
            };

            var primarySlot = new ScheduleSlotModel
            {
                Id = 21,
                ScheduleId = primaryGraph.Id,
                DayOfMonth = 1,
                SlotNo = 1,
                EmployeeId = useLegacyPrimarySlotEmployeeId ? primaryScheduleEmployee.Id : employee.Id,
                Employee = employee,
                Status = SlotStatus.ASSIGNED,
                FromTime = "08:00",
                ToTime = "16:00",
            };
            var relatedSlot = new ScheduleSlotModel
            {
                Id = 22,
                ScheduleId = relatedGraph.Id,
                DayOfMonth = 2,
                SlotNo = 1,
                EmployeeId = employee.Id,
                Employee = employee,
                Status = SlotStatus.ASSIGNED,
                FromTime = "08:00",
                ToTime = "16:00",
            };
            var style = new ScheduleCellStyleModel
            {
                Id = 31,
                ScheduleId = primaryGraph.Id,
                DayOfMonth = 1,
                EmployeeId = employee.Id,
                BackgroundColorArgb = 10,
                TextColorArgb = 20,
            };

            return new TemplateExportFixture
            {
                Container = container,
                PrimaryGraph = primaryGraph,
                ContainerService = new TemplateContainerServiceStub
                {
                    Container = container,
                    Graphs = [primaryGraph, relatedGraph],
                    GraphSlots =
                    {
                        [primaryGraph.Id] = [primarySlot],
                        [relatedGraph.Id] = [relatedSlot],
                    },
                    GraphEmployees =
                    {
                        [primaryGraph.Id] = [primaryScheduleEmployee],
                        [relatedGraph.Id] = [relatedScheduleEmployee],
                    },
                    GraphStyles =
                    {
                        [primaryGraph.Id] = [style],
                        [relatedGraph.Id] = [],
                    },
                },
                ShopService = new TemplateShopServiceStub([shop, secondShop]),
            };
        }

        public static TemplateExportFixture CreateEmpty()
        {
            var container = new ContainerModel { Id = 2, Name = "Empty Container", Note = "Nothing here" };
            var shop = new ShopModel { Id = 8, Name = "Quiet Shop", Address = "Address 8", Description = "Calm" };
            var graph = TestDataFactory.CreateScheduleModel(id: 9, containerId: container.Id, shopId: shop.Id, name: "Quiet Graph", year: 2026, month: 4, availabilityGroupId: null);

            return new TemplateExportFixture
            {
                Container = container,
                PrimaryGraph = graph,
                ContainerService = new TemplateContainerServiceStub
                {
                    Container = container,
                    Graphs = [graph],
                    GraphSlots =
                    {
                        [graph.Id] = [],
                    },
                    GraphEmployees =
                    {
                        [graph.Id] = [],
                    },
                    GraphStyles =
                    {
                        [graph.Id] = [],
                    },
                },
                ShopService = new TemplateShopServiceStub([shop]),
            };
        }
    }

    private sealed class TemplateContainerServiceStub : IContainerService
    {
        public ContainerModel? Container { get; init; }
        public ScheduleModel? Graph { get; init; }
        public List<ScheduleModel> Graphs { get; init; } = [];
        public Dictionary<int, List<ScheduleSlotModel>> GraphSlots { get; init; } = [];
        public Dictionary<int, List<ScheduleEmployeeModel>> GraphEmployees { get; init; } = [];
        public Dictionary<int, List<ScheduleCellStyleModel>> GraphStyles { get; init; } = [];

        public Task<ContainerModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(Container?.Id == id ? Container : null);

        public Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Container is null ? new List<ContainerModel>() : [Container]);

        public Task<ContainerModel> CreateAsync(ContainerModel entity, CancellationToken ct = default)
            => Task.FromResult(entity);

        public Task UpdateAsync(ContainerModel entity, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(new List<ContainerModel>());

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteOperationResult.Success());

        public Task<List<ScheduleModel>?> GetGraphsAsync(int containerId, CancellationToken ct = default)
            => Task.FromResult<List<ScheduleModel>?>(Container?.Id == containerId ? Graphs : null);

        public Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult(Graphs.FirstOrDefault(graph => graph.Id == graphId && graph.ContainerId == containerId));

        public Task<List<ScheduleModel>> GetPublishedGraphsForEmployeeAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(Graphs);

        public Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<SchedulePresetModel>?> GetSchedulePresetsAsync(int containerId, CancellationToken ct = default)
            => Task.FromResult<List<SchedulePresetModel>?>([]);

        public Task<SchedulePresetModel> CreateSchedulePresetAsync(int containerId, SchedulePresetModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task<GenerateGraphResult> GenerateGraphAsync(int containerId, int graphId, bool overwrite, bool dryRun, IProgress<int>? progress, CancellationToken ct = default)
            => Task.FromResult(new GenerateGraphResult());

        public Task<GenerateGraphResult> GenerateGraphPreviewAsync(int containerId, ScheduleModel model, IEnumerable<ScheduleEmployeeModel> employees, IProgress<int>? progress, CancellationToken ct = default)
            => Task.FromResult(new GenerateGraphResult());

        public Task<List<ScheduleSlotModel>?> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult(GraphSlots.TryGetValue(graphId, out var slots) ? (List<ScheduleSlotModel>?)slots : null);

        public Task ReplaceGraphSlotsAsync(int containerId, int graphId, IEnumerable<ScheduleSlotModel> slots, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ScheduleEmployeeModel>?> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult(GraphEmployees.TryGetValue(graphId, out var employees) ? (List<ScheduleEmployeeModel>?)employees : null);

        public Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ScheduleCellStyleModel>?> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult(GraphStyles.TryGetValue(graphId, out var styles) ? (List<ScheduleCellStyleModel>?)styles : null);

        public Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class TemplateShopServiceStub : IShopService
    {
        private readonly Dictionary<int, ShopModel> _shops;

        public TemplateShopServiceStub(IEnumerable<ShopModel> shops)
        {
            _shops = shops.ToDictionary(shop => shop.Id);
        }

        public Task<ShopModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(_shops.GetValueOrDefault(id));

        public Task<List<ShopModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_shops.Values.OrderBy(shop => shop.Id).ToList());

        public Task<ShopModel> CreateAsync(ShopModel entity, CancellationToken ct = default)
            => Task.FromResult(entity);

        public Task UpdateAsync(ShopModel entity, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(_shops.Values.Where(shop => shop.Name.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList());

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteOperationResult.Success());
    }

    private sealed class ExcelTemplateLocatorStub : IExcelTemplateLocator
    {
        private readonly string _scheduleTemplatePath;
        private readonly string _containerTemplatePath;

        public ExcelTemplateLocatorStub(string scheduleTemplatePath, string containerTemplatePath)
        {
            _scheduleTemplatePath = scheduleTemplatePath;
            _containerTemplatePath = containerTemplatePath;
        }

        public string GetScheduleTemplatePath()
            => _scheduleTemplatePath;

        public string GetContainerTemplatePath()
            => _containerTemplatePath;
    }
}
