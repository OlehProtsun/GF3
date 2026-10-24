using BusinessLogicLayer.Contracts.Enums;
using ClosedXML.Excel;

namespace GF3.Tests;

public sealed partial class ExportLayerDeepTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishedExport_UsesSharedWorkbookAndFiltersPrivateHints(bool relatedPublic)
    {
        using var templates = new TempExcelTemplates();
        var fixture = TemplateExportFixture.CreateRich(useLegacyPrimarySlotEmployeeId: false);
        fixture.PrimaryGraph.PublicationStatus = SchedulePublicationStatus.Public;
        var related = fixture.ContainerService.Graphs[1];
        related.PublicationStatus = relatedPublic ? SchedulePublicationStatus.Public : SchedulePublicationStatus.Private;
        fixture.PrimaryGraph.Note = CreatePortableNote("Planner note", """
            {"manualColumns":[{"id":9,"label":"Manual","cells":{"3":"Training"}}]}
            """);
        var note = fixture.PrimaryGraph.Note;
        var slotIds = fixture.ContainerService.GraphSlots[5].Select(slot => slot.EmployeeId).ToArray();
        var service = fixture.CreateService(templates);
        var employee = await service.ExportPublishedGraphToXlsxAsync(3, 5);
        var manager = await service.ExportGraphToXlsxAsync(1, 5, true, true);
        using var employeeBook = new XLWorkbook(new MemoryStream(employee.content));
        using var managerBook = new XLWorkbook(new MemoryStream(manager.content));
        Assert.Equal(2, employeeBook.Worksheets.Count);
        Assert.EndsWith(" - Statistic", employeeBook.Worksheet(2).Name);
        var employeeCells = employeeBook.Worksheet(1).Range("B1:AA32").Cells().Select(cell => cell.GetString()).ToArray();
        var managerCells = managerBook.Worksheet(1).Range("B1:AA32").Cells().Select(cell => cell.GetString()).ToArray();
        Assert.Contains("Manual", employeeCells);
        Assert.Contains("Training", employeeCells);
        Assert.Contains(managerCells, value => value.Contains(related.Name));
        if (relatedPublic) Assert.Equal(managerCells, employeeCells);
        else Assert.DoesNotContain(employeeCells, value => value.Contains(related.Name));
        Assert.Equal(note, fixture.PrimaryGraph.Note);
        Assert.Equal(slotIds, fixture.ContainerService.GraphSlots[5].Select(slot => slot.EmployeeId));
    }

    [Theory]
    [InlineData(3, 5, false)]
    [InlineData(99, 5, true)]
    [InlineData(3, 99, true)]
    public async Task PublishedExport_DeniesUnavailableGraphsIdentically(int employeeId, int graphId, bool published)
    {
        using var templates = new TempExcelTemplates();
        var fixture = TemplateExportFixture.CreateRich(false);
        fixture.PrimaryGraph.PublicationStatus = published ? SchedulePublicationStatus.Public : SchedulePublicationStatus.Private;
        var error = await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.CreateService(templates)
            .ExportPublishedGraphToXlsxAsync(employeeId, graphId));
        Assert.Equal("Schedule was not found.", error.Message);
    }
}
