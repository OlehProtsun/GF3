using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Services.Export;
using ClosedXML.Excel;
using Xunit;

namespace ArchitectureTests;

public class GraphTemplateExportServiceTests
{
    [Fact]
    public async Task ExportGraphToXlsxAsync_WhenSlotsContainLegacyScheduleEmployeeIds_ShouldRenderShiftAndNonZeroTotals()
    {
        var container = new ContainerModel { Id = 2, Name = "Container-2" };
        var graph = new ScheduleModel
        {
            Id = 7,
            ContainerId = 2,
            ShopId = 1,
            Name = "Graph 7",
            Year = 2025,
            Month = 1,
            PeoplePerShift = 1,
            Shift1Time = "09:00 - 15:00",
            Shift2Time = "15:00 - 21:00"
        };

        var employees = new List<ScheduleEmployeeModel>
        {
            new()
            {
                Id = 1001,
                ScheduleId = 7,
                EmployeeId = 101,
                Employee = new EmployeeModel { Id = 101, FirstName = "Ivan", LastName = "Petrenko" }
            }
        };

        var slots = new List<ScheduleSlotModel>
        {
            new()
            {
                Id = 1,
                ScheduleId = 7,
                DayOfMonth = 1,
                SlotNo = 1,
                EmployeeId = 1001, // legacy bug shape: schedule_employee.id instead of employee.id
                Status = SlotStatus.ASSIGNED,
                FromTime = "09:00",
                ToTime = "15:00"
            }
        };

        var containerService = new FakeContainerService(container, graph, employees, slots);
        var shopService = new FakeShopService(new ShopModel { Id = 1, Name = "Shop", Address = "Addr" });
        var contextBuilder = new ScheduleExcelContextBuilder();
        var templateLocator = new InlineTemplateLocator(CreateTemplateFiles());
        var sut = new GraphTemplateExportService(containerService, shopService, templateLocator, contextBuilder);

        var (content, _) = await sut.ExportGraphToXlsxAsync(2, 7, includeStyles: false, includeEmployees: true);

        using var ms = new MemoryStream(content);
        using var wb = new XLWorkbook(ms);
        var matrixWs = wb.Worksheet("Graph 7");
        var statisticWs = wb.Worksheet("Graph 7 - Statistic");

        var hasRenderedShift = matrixWs.Range("C2:AA32")
            .Cells()
            .Select(c => c.GetString())
            .Any(s => s.Contains(':') && s.Contains(" - ", StringComparison.Ordinal));

        Assert.True(hasRenderedShift);
        Assert.NotEqual("0h 0m", statisticWs.Cell(7, 2).GetString());
    }

    private static (string scheduleTemplatePath, string containerTemplatePath) CreateTemplateFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gf3_test_templates_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var schedule = Path.Combine(root, "ScheduleTemplate.xlsx");
        using (var wb = new XLWorkbook())
        {
            wb.AddWorksheet("ScheduleName");
            wb.AddWorksheet("ScheduleStatistic");
            wb.SaveAs(schedule);
        }

        var container = Path.Combine(root, "ContainerTemplate.xlsx");
        using (var wb = new XLWorkbook())
        {
            wb.AddWorksheet("ContainerTemplate");
            wb.SaveAs(container);
        }

        return (schedule, container);
    }

    private sealed class InlineTemplateLocator : IExcelTemplateLocator
    {
        private readonly string _schedule;
        private readonly string _container;

        public InlineTemplateLocator((string scheduleTemplatePath, string containerTemplatePath) paths)
        {
            _schedule = paths.scheduleTemplatePath;
            _container = paths.containerTemplatePath;
        }

        public string GetScheduleTemplatePath() => _schedule;
        public string GetContainerTemplatePath() => _container;
    }

    private sealed class FakeShopService : IShopService
    {
        private readonly ShopModel _shop;
        public FakeShopService(ShopModel shop) => _shop = shop;

        public Task<ShopModel?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult<ShopModel?>(_shop);
        public Task<List<ShopModel>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<ShopModel> { _shop });
        public Task<ShopModel> CreateAsync(ShopModel entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(ShopModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeContainerService : IContainerService
    {
        private readonly ContainerModel _container;
        private readonly ScheduleModel _graph;
        private readonly List<ScheduleEmployeeModel> _employees;
        private readonly List<ScheduleSlotModel> _slots;

        public FakeContainerService(ContainerModel container, ScheduleModel graph, List<ScheduleEmployeeModel> employees, List<ScheduleSlotModel> slots)
        {
            _container = container;
            _graph = graph;
            _employees = employees;
            _slots = slots;
        }

        public Task<ContainerModel?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult<ContainerModel?>(_container);
        public Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<ContainerModel> { _container });
        public Task<ContainerModel> CreateAsync(ContainerModel entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(ContainerModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ScheduleModel>> GetGraphsAsync(int containerId, CancellationToken ct = default) => Task.FromResult(new List<ScheduleModel> { _graph });
        public Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default) => Task.FromResult<ScheduleModel?>(_graph);
        public Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<GenerateGraphResult> GenerateGraphAsync(int containerId, int graphId, bool overwrite, bool dryRun, IProgress<int>? progress, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ScheduleSlotModel>> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default) => Task.FromResult(_slots);
        public Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ScheduleEmployeeModel>> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default) => Task.FromResult(_employees);
        public Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ScheduleCellStyleModel>> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default) => Task.FromResult(new List<ScheduleCellStyleModel>());
        public Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
