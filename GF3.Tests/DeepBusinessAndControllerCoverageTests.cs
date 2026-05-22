using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Availability;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Shops;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Contracts.Shops;

namespace GF3.Tests;

public sealed class DeepBusinessAndControllerCoverageTests
{
    [Fact]
    public async Task ScheduleGenerator_GenerateAsync_ProducesSlots_ForSimpleSingleEmployeeScenario()
    {
        var generator = new ScheduleGenerator();
        var schedule = TestDataFactory.CreateScheduleModel(
            id: 5,
            containerId: 1,
            shopId: 2,
            year: 2026,
            month: 4);

        schedule.PeoplePerShift = 1;
        schedule.Shift1Time = "08:00 - 16:00";
        schedule.Shift2Time = string.Empty;
        schedule.MaxHoursPerEmpMonth = 300;
        schedule.MaxConsecutiveDays = 31;
        schedule.MaxConsecutiveFull = 31;
        schedule.MaxFullPerMonth = 31;

        var employees = new[]
        {
            new ScheduleEmployeeModel
            {
                EmployeeId = 42,
                DisplayOrder = 0,
                MinHoursMonth = 160,
                Employee = TestDataFactory.CreateEmployeeModel(42, "John", "Smith"),
            }
        };

        var result = await generator.GenerateAsync(schedule, [], employees, progress: null, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.All(result, slot =>
        {
            Assert.InRange(slot.ScheduleId, 0, schedule.Id);
            Assert.InRange(slot.DayOfMonth, 1, DateTime.DaysInMonth(schedule.Year, schedule.Month));
            Assert.True(slot.SlotNo >= 1);
            Assert.False(string.IsNullOrWhiteSpace(slot.FromTime));
            Assert.False(string.IsNullOrWhiteSpace(slot.ToTime));
        });
        Assert.Contains(result, slot => slot.EmployeeId == 42);
    }

    [Fact]
    public async Task ScheduleGenerator_GenerateAsync_GuardsInvalidInputs()
    {
        var generator = new ScheduleGenerator();
        var employees = new[]
        {
            new ScheduleEmployeeModel
            {
                EmployeeId = 42,
                MinHoursMonth = 20,
            }
        };

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            generator.GenerateAsync(null!, [], employees, progress: null, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            generator.GenerateAsync(CreateGeneratorSchedule(), null!, employees, progress: null, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            generator.GenerateAsync(CreateGeneratorSchedule(), [], null!, progress: null, CancellationToken.None));

        var invalidMonth = CreateGeneratorSchedule();
        invalidMonth.Month = 13;
        var invalidYear = CreateGeneratorSchedule();
        invalidYear.Year = 1899;
        var invalidPeoplePerShift = CreateGeneratorSchedule();
        invalidPeoplePerShift.PeoplePerShift = 0;
        var missingShiftTemplate = CreateGeneratorSchedule();
        missingShiftTemplate.Shift1Time = " ";
        missingShiftTemplate.Shift2Time = string.Empty;

        Assert.Empty(await generator.GenerateAsync(invalidMonth, [], employees, progress: null, CancellationToken.None));
        Assert.Empty(await generator.GenerateAsync(invalidYear, [], employees, progress: null, CancellationToken.None));
        Assert.Empty(await generator.GenerateAsync(invalidPeoplePerShift, [], employees, progress: null, CancellationToken.None));
        Assert.Empty(await generator.GenerateAsync(missingShiftTemplate, [], employees, progress: null, CancellationToken.None));
        Assert.Empty(await generator.GenerateAsync(CreateGeneratorSchedule(), [], [], progress: null, CancellationToken.None));
    }

    [Fact]
    public async Task ScheduleGenerator_GenerateAsync_HonorsUnavailableAndUnreadableAvailability()
    {
        var generator = new ScheduleGenerator();
        var schedule = CreateGeneratorSchedule();
        var employees = new[]
        {
            new ScheduleEmployeeModel { EmployeeId = 1, DisplayOrder = 0, MinHoursMonth = 40 },
            new ScheduleEmployeeModel { EmployeeId = 2, DisplayOrder = 1, MinHoursMonth = 40 },
        };
        var availability = new AvailabilityGroupModel
        {
            Id = 10,
            Name = "April Availability",
            Year = schedule.Year,
            Month = schedule.Month,
            Members =
            [
                new AvailabilityGroupMemberModel
                {
                    Id = 11,
                    AvailabilityGroupId = 10,
                    EmployeeId = 1,
                    Days =
                    [
                        new AvailabilityGroupDayModel
                        {
                            AvailabilityGroupMemberId = 11,
                            DayOfMonth = 1,
                            Kind = AvailabilityKind.NONE,
                        },
                        new AvailabilityGroupDayModel
                        {
                            AvailabilityGroupMemberId = 11,
                            DayOfMonth = 2,
                            Kind = AvailabilityKind.INT,
                            IntervalStr = "not-a-time",
                        },
                    ],
                },
                new AvailabilityGroupMemberModel
                {
                    Id = 12,
                    AvailabilityGroupId = 10,
                    EmployeeId = 2,
                },
            ],
        };

        var result = await generator.GenerateAsync(schedule, [availability], employees, progress: null, CancellationToken.None);

        var day1Slot = Assert.Single(result, slot => slot.DayOfMonth == 1);
        var day2Slot = Assert.Single(result, slot => slot.DayOfMonth == 2);
        Assert.Equal(2, day1Slot.EmployeeId);
        Assert.Equal(2, day2Slot.EmployeeId);
        Assert.DoesNotContain(result, slot => slot.DayOfMonth is 1 or 2 && slot.EmployeeId == 1);
    }

    [Fact]
    public async Task GraphExportService_ExportGraphSqlAsync_EmitsPortableSqlForGraphBundle()
    {
        var fixture = ExportFixture.Create();
        var service = fixture.CreateExportService();

        var bytes = await service.ExportGraphSqlAsync(
            fixture.Container.Id,
            fixture.Graph.Id,
            includeEmployees: true,
            includeStyles: true,
            CancellationToken.None);

        var script = Encoding.UTF8.GetString(bytes);

        Assert.Contains("BEGIN TRANSACTION;", script);
        Assert.Contains("INSERT OR IGNORE INTO container", script);
        Assert.Contains("INSERT OR IGNORE INTO shop", script);
        Assert.Contains("INSERT OR IGNORE INTO employee", script);
        Assert.Contains("INSERT OR IGNORE INTO availability_group", script);
        Assert.Contains("INSERT OR IGNORE INTO schedule", script);
        Assert.Contains("INSERT OR IGNORE INTO schedule_employee", script);
        Assert.Contains("INSERT OR IGNORE INTO schedule_slot", script);
        Assert.Contains("INSERT OR IGNORE INTO schedule_cell_style", script);
        Assert.Contains("COMMIT;", script);
        Assert.Contains(fixture.Graph.Name, script);
        Assert.Contains(fixture.Employee.FirstName, script);
    }

    [Fact]
    public async Task GraphExportService_ExportGraphSqlAsync_FetchesReferencedEmployeesAndHonorsIncludeFlags()
    {
        var fixture = ExportFixture.Create();
        fixture.Slot.Employee = null;
        fixture.Member.Employee = null;
        fixture.ScheduleEmployee.Employee = null;
        var service = fixture.CreateExportService();

        var bytes = await service.ExportGraphSqlAsync(
            fixture.Container.Id,
            fixture.Graph.Id,
            includeEmployees: false,
            includeStyles: false,
            CancellationToken.None);

        var script = Encoding.UTF8.GetString(bytes);

        Assert.Contains("INSERT OR IGNORE INTO employee", script);
        Assert.Contains(fixture.Employee.FirstName, script);
        Assert.Contains("INSERT OR IGNORE INTO availability_group_member", script);
        Assert.Contains("INSERT OR IGNORE INTO availability_group_day", script);
        Assert.Contains("INSERT OR IGNORE INTO schedule_slot", script);
        Assert.DoesNotContain("INSERT OR IGNORE INTO schedule_employee", script);
        Assert.DoesNotContain("INSERT OR IGNORE INTO schedule_cell_style", script);
    }

    [Fact]
    public async Task GraphExportService_ExportGraphSqlAsync_ThrowsWhenReferencedEmployeeCannotBeLoaded()
    {
        var fixture = ExportFixture.Create();
        fixture.Slot.Employee = null;
        fixture.Slot.EmployeeId = 999;
        var service = fixture.CreateExportService();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ExportGraphSqlAsync(
                fixture.Container.Id,
                fixture.Graph.Id,
                includeEmployees: false,
                includeStyles: false,
                CancellationToken.None));

        Assert.Equal("A referenced employee could not be loaded for SQL export.", exception.Message);
    }

    [Fact]
    public async Task GraphExportService_ExportGraphExcelCsvAsync_IncludesEmployeeNameAndStyles()
    {
        var fixture = ExportFixture.Create();
        var service = fixture.CreateExportService();

        var bytes = await service.ExportGraphExcelCsvAsync(
            fixture.Container.Id,
            fixture.Graph.Id,
            includeEmployees: true,
            includeStyles: true,
            CancellationToken.None);

        var csv = Encoding.UTF8.GetString(bytes);

        Assert.Contains("ScheduleId,ScheduleName,Year,Month,Day,SlotNo,FromTime,ToTime,EmployeeId,EmployeeName,Status,BackgroundColorArgb,TextColorArgb", csv);
        Assert.Contains($"{fixture.Employee.FirstName} {fixture.Employee.LastName}", csv);
        Assert.Contains("10", csv);
        Assert.Contains("20", csv);
        Assert.Contains(fixture.Graph.Name, csv);
    }

    [Fact]
    public async Task GraphExportService_ExportGraphExcelCsvAsync_EscapesCsvAndOmitsOptionalEmployeeData()
    {
        var fixture = ExportFixture.Create();
        fixture.Graph.Name = "April, \"Night\"";
        fixture.Slot.FromTime = "08:00\nstart";
        var service = fixture.CreateExportService();

        var bytes = await service.ExportGraphExcelCsvAsync(
            fixture.Container.Id,
            fixture.Graph.Id,
            includeEmployees: false,
            includeStyles: false,
            CancellationToken.None);

        var csv = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"April, \"\"Night\"\"\"", csv);
        Assert.Contains("\"08:00\nstart\"", csv);
        Assert.Contains($",{fixture.Employee.Id},,", csv);
        Assert.DoesNotContain($"{fixture.Employee.FirstName} {fixture.Employee.LastName}", csv);
        Assert.DoesNotContain(",10,20", csv);
    }

    [Fact]
    public async Task GraphExportService_ExportContainerSqlAsync_DeduplicatesSharedReferencesAcrossGraphs()
    {
        var fixture = ExportFixture.Create();
        var service = fixture.CreateExportService();

        var bytes = await service.ExportContainerSqlAsync(
            fixture.Container.Id,
            includeEmployees: true,
            includeStyles: true,
            CancellationToken.None);

        var script = Encoding.UTF8.GetString(bytes);

        Assert.Equal(1, CountOccurrences(script, "INSERT OR IGNORE INTO container"));
        Assert.Equal(1, CountOccurrences(script, "INSERT OR IGNORE INTO shop"));
        Assert.Equal(1, CountOccurrences(script, "INSERT OR IGNORE INTO employee"));
        Assert.Equal(2, CountOccurrences(script, "INSERT OR IGNORE INTO schedule ("));
    }

    [Fact]
    public async Task ContainersController_DeleteAndGenerateGraph_ReturnExpectedPayloads()
    {
        var service = new ContainerServiceStub
        {
            Container = new ContainerModel { Id = 1, Name = "Container" },
            DeleteResult = DeleteOperationResult.Failure("Blocked."),
            GenerateResult = new GenerateGraphResult
            {
                ContainerId = 1,
                GraphId = 5,
                GeneratedSlotsCount = 3,
                WrittenSlotsCount = 3,
                Slots =
                [
                    TestDataFactory.CreateScheduleSlotModel(1, 1, 10, "08:00", "16:00", scheduleId: 5)
                ]
            }
        };

        var controller = new ContainersController(service, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetHttpContext(controller);

        var deleteResult = await controller.Delete(1, CancellationToken.None);
        var generateResult = await controller.GenerateGraph(
            1,
            5,
            new GenerateGraphRequest
            {
                DryRun = false,
                ReturnSlots = false,
                Overwrite = true,
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(deleteResult);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal("Blocked.", problem.Detail);

        var okResult = Assert.IsType<OkObjectResult>(generateResult.Result);
        var payload = Assert.IsType<GenerateGraphResponse>(okResult.Value);
        Assert.Equal(3, payload.GeneratedSlotsCount);
        Assert.Equal(3, payload.WrittenSlotsCount);
        Assert.Null(payload.Slots);
    }

    [Fact]
    public async Task ContainersController_Delete_ReturnsNoContentWhenDeleteSucceeds()
    {
        var service = new ContainerServiceStub
        {
            Container = new ContainerModel { Id = 1, Name = "Container" },
            DeleteResult = DeleteOperationResult.Success(),
        };
        var controller = new ContainersController(service, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetHttpContext(controller);

        var result = await controller.Delete(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ContainersController_Delete_ThrowsWhenContainerIsMissing()
    {
        var service = new ContainerServiceStub();
        var controller = new ContainersController(service, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetHttpContext(controller);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            controller.Delete(1, CancellationToken.None));

        Assert.Equal("Container with id 1 was not found.", exception.Message);
    }

    [Fact]
    public async Task ContainersController_ReadAndCreateRoutes_ReturnExpectedResults()
    {
        var service = new ContainerServiceStub
        {
            Container = new ContainerModel { Id = 1, Name = "Container", Note = "Note" },
            Graphs =
            [
                TestDataFactory.CreateScheduleModel(id: 5, containerId: 1, shopId: 2, name: "April")
            ]
        };

        var controller = new ContainersController(service, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetHttpContext(controller);

        var getById = await controller.GetById(1, CancellationToken.None);
        var getGraphs = await controller.GetGraphs(1, CancellationToken.None);
        var created = await controller.Create(
            new WebApi.Contracts.Containers.CreateContainerRequest
            {
                Name = "New Container",
                Note = "Fresh",
            },
            CancellationToken.None);

        var containerResult = Assert.IsType<OkObjectResult>(getById.Result);
        Assert.IsType<WebApi.Contracts.Containers.ContainerDto>(containerResult.Value);

        var graphsResult = Assert.IsType<OkObjectResult>(getGraphs.Result);
        var graphs = Assert.IsAssignableFrom<IEnumerable<WebApi.Contracts.Containers.Graphs.GraphDto>>(graphsResult.Value);
        Assert.Single(graphs);

        var createdResult = Assert.IsType<CreatedAtActionResult>(created.Result);
        Assert.Equal(nameof(ContainersController.GetById), createdResult.ActionName);
    }

    [Fact]
    public async Task ShopsController_CreateAndDelete_MapFacadeResults()
    {
        var facade = new ShopFacadeStub
        {
            CreatedShop = new BusinessLogicLayer.Contracts.Shops.ShopDto
            {
                Id = 7,
                Name = "North Shop",
                Address = "Main street",
                Description = "Desc",
            },
            ExistingShop = new BusinessLogicLayer.Contracts.Shops.ShopDto
            {
                Id = 7,
                Name = "North Shop",
                Address = "Main street",
                Description = "Desc",
            },
            DeleteResult = DeleteOperationResult.Success(),
        };

        var controller = new ShopsController(facade);
        SetHttpContext(controller);

        var createdResult = await controller.Create(
            new CreateShopRequest
            {
                Name = "North Shop",
                Address = "Main street",
                Description = "Desc",
            },
            CancellationToken.None);
        var deleteResult = await controller.Delete(7, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createdResult.Result);
        var dto = Assert.IsType<WebApi.Contracts.Shops.ShopDto>(created.Value);
        Assert.Equal(7, dto.Id);
        Assert.Equal("North Shop", dto.Name);
        Assert.IsType<NoContentResult>(deleteResult);
    }

    [Fact]
    public async Task ShopsController_Delete_ReturnsValidationProblem_WhenFacadeBlocksDeletion()
    {
        var facade = new ShopFacadeStub
        {
            ExistingShop = new BusinessLogicLayer.Contracts.Shops.ShopDto
            {
                Id = 8,
                Name = "Blocked Shop",
                Address = "Blocked street",
            },
            DeleteResult = DeleteOperationResult.Failure("Shop is still used."),
        };

        var controller = new ShopsController(facade);
        SetHttpContext(controller);

        var result = await controller.Delete(8, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal("Shop is still used.", problem.Detail);
    }

    [Fact]
    public async Task ExportsController_ExportEndpoints_ReturnFileResults()
    {
        var graphExport = new GraphExportServiceStub
        {
            GraphSqlBytes = Encoding.UTF8.GetBytes("graph-sql"),
            ContainerSqlBytes = Encoding.UTF8.GetBytes("container-sql"),
        };
        var templateExport = new GraphTemplateExportServiceStub
        {
            GraphExcel = (Encoding.UTF8.GetBytes("graph-xlsx"), "graph.xlsx"),
            ContainerExcel = (Encoding.UTF8.GetBytes("container-xlsx"), "container.xlsx"),
        };

        var controller = new ExportsController(graphExport, templateExport);

        var excelGraph = Assert.IsType<FileContentResult>(await controller.ExportExcel(1, 5, true, true, CancellationToken.None));
        var sqlGraph = Assert.IsType<FileContentResult>(await controller.ExportGraphSql(1, 5, true, true, CancellationToken.None));
        var excelContainer = Assert.IsType<FileContentResult>(await controller.ExportContainerExcel(1, true, true, CancellationToken.None));
        var sqlContainer = Assert.IsType<FileContentResult>(await controller.ExportContainerSql(1, true, true, CancellationToken.None));

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelGraph.ContentType);
        Assert.Equal("graph.xlsx", excelGraph.FileDownloadName);
        Assert.Equal("text/plain; charset=utf-8", sqlGraph.ContentType);
        Assert.StartsWith("GF3_Graph_5_", sqlGraph.FileDownloadName, StringComparison.Ordinal);
        Assert.EndsWith(".sql", sqlGraph.FileDownloadName, StringComparison.Ordinal);
        Assert.Equal("container.xlsx", excelContainer.FileDownloadName);
        Assert.StartsWith("GF3_Container_1_", sqlContainer.FileDownloadName, StringComparison.Ordinal);
        Assert.EndsWith(".sql", sqlContainer.FileDownloadName, StringComparison.Ordinal);
    }

    private static void SetHttpContext(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
    }

    private static int CountOccurrences(string text, string value)
        => text.Split(value, StringSplitOptions.None).Length - 1;

    private static ScheduleModel CreateGeneratorSchedule()
    {
        var schedule = TestDataFactory.CreateScheduleModel(
            id: 5,
            containerId: 1,
            shopId: 2,
            year: 2026,
            month: 4);

        schedule.PeoplePerShift = 1;
        schedule.Shift1Time = "08:00 - 16:00";
        schedule.Shift2Time = string.Empty;
        schedule.MaxHoursPerEmpMonth = 300;
        schedule.MaxConsecutiveDays = 31;
        schedule.MaxConsecutiveFull = 31;
        schedule.MaxFullPerMonth = 31;
        return schedule;
    }

    private sealed class ExportFixture
    {
        public required ContainerModel Container { get; init; }
        public required ShopModel Shop { get; init; }
        public required EmployeeModel Employee { get; init; }
        public required ScheduleModel Graph { get; init; }
        public required ScheduleModel RelatedGraph { get; init; }
        public required AvailabilityGroupModel Group { get; init; }
        public required AvailabilityGroupMemberModel Member { get; init; }
        public required AvailabilityGroupDayModel Day { get; init; }
        public required ScheduleEmployeeModel ScheduleEmployee { get; init; }
        public required ScheduleSlotModel Slot { get; init; }
        public required ScheduleCellStyleModel Style { get; init; }

        public static ExportFixture Create()
        {
            var container = new ContainerModel { Id = 1, Name = "Main Container", Note = "Container note" };
            var shop = new ShopModel { Id = 2, Name = "Main Shop", Address = "Address 1", Description = "Shop desc" };
            var employee = new EmployeeModel { Id = 3, FirstName = "John", LastName = "Smith", Email = "john@example.com", Phone = "123" };

            var graph = TestDataFactory.CreateScheduleModel(id: 5, containerId: container.Id, shopId: shop.Id, name: "April Graph", year: 2026, month: 4, availabilityGroupId: 11);
            graph.Note = "Portable note";

            var relatedGraph = TestDataFactory.CreateScheduleModel(id: 6, containerId: container.Id, shopId: shop.Id, name: "April Graph 2", year: 2026, month: 4, availabilityGroupId: null);

            var group = new AvailabilityGroupModel { Id = 11, Name = "April Availability", Year = 2026, Month = 4 };
            var member = new AvailabilityGroupMemberModel
            {
                Id = 12,
                AvailabilityGroupId = group.Id,
                EmployeeId = employee.Id,
                Employee = employee,
                DisplayOrder = 0,
            };
            var day = new AvailabilityGroupDayModel
            {
                Id = 13,
                AvailabilityGroupMemberId = member.Id,
                DayOfMonth = 1,
                Kind = BusinessLogicLayer.Contracts.Enums.AvailabilityKind.INT,
                IntervalStr = "08:00 - 16:00",
            };

            member.Days.Add(day);
            group.Members.Add(member);

            var scheduleEmployee = new ScheduleEmployeeModel
            {
                Id = 14,
                ScheduleId = graph.Id,
                EmployeeId = employee.Id,
                Employee = employee,
                DisplayOrder = 0,
                MinHoursMonth = 80,
            };

            var slot = new ScheduleSlotModel
            {
                Id = 15,
                ScheduleId = graph.Id,
                DayOfMonth = 1,
                SlotNo = 1,
                EmployeeId = employee.Id,
                Employee = employee,
                Status = BusinessLogicLayer.Contracts.Enums.SlotStatus.ASSIGNED,
                FromTime = "08:00",
                ToTime = "16:00",
            };

            var style = new ScheduleCellStyleModel
            {
                Id = 16,
                ScheduleId = graph.Id,
                DayOfMonth = 1,
                EmployeeId = employee.Id,
                BackgroundColorArgb = 10,
                TextColorArgb = 20,
            };

            return new ExportFixture
            {
                Container = container,
                Shop = shop,
                Employee = employee,
                Graph = graph,
                RelatedGraph = relatedGraph,
                Group = group,
                Member = member,
                Day = day,
                ScheduleEmployee = scheduleEmployee,
                Slot = slot,
                Style = style,
            };
        }

        public GraphExportService CreateExportService()
        {
            var containerService = new ContainerServiceStub
            {
                Container = Container,
                Graph = Graph,
                Graphs = [Graph, RelatedGraph],
                GraphSlots =
                {
                    [Graph.Id] =
                    [
                        Slot
                    ],
                    [RelatedGraph.Id] =
                    [
                        new ScheduleSlotModel
                        {
                            ScheduleId = RelatedGraph.Id,
                            DayOfMonth = 2,
                            SlotNo = 1,
                            EmployeeId = null,
                            Status = BusinessLogicLayer.Contracts.Enums.SlotStatus.UNFURNISHED,
                            FromTime = "08:00",
                            ToTime = "16:00",
                        }
                    ]
                },
                GraphEmployees =
                {
                    [Graph.Id] =
                    [
                        ScheduleEmployee
                    ],
                    [RelatedGraph.Id] = []
                },
                GraphStyles =
                {
                    [Graph.Id] =
                    [
                        Style
                    ],
                    [RelatedGraph.Id] = []
                }
            };

            return new GraphExportService(
                containerService,
                new AvailabilityGroupServiceStub(Group, [Member], [Day]),
                new EmployeeServiceStub(Employee),
                new ShopServiceStub(Shop));
        }
    }

    private sealed class ContainerServiceStub : IContainerService
    {
        public ContainerModel? Container { get; init; }
        public ScheduleModel? Graph { get; init; }
        public List<ScheduleModel> Graphs { get; init; } = [];
        public Dictionary<int, List<ScheduleSlotModel>> GraphSlots { get; } = [];
        public Dictionary<int, List<ScheduleEmployeeModel>> GraphEmployees { get; } = [];
        public Dictionary<int, List<ScheduleCellStyleModel>> GraphStyles { get; } = [];
        public DeleteOperationResult DeleteResult { get; init; } = DeleteOperationResult.Success();
        public GenerateGraphResult GenerateResult { get; init; } = new();

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
            => Task.FromResult(DeleteResult);

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
            => Task.FromResult(GenerateResult);

        public Task<GenerateGraphResult> GenerateGraphPreviewAsync(int containerId, ScheduleModel model, IEnumerable<ScheduleEmployeeModel> employees, IProgress<int>? progress, CancellationToken ct = default)
            => Task.FromResult(GenerateResult);

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

    private sealed class AvailabilityGroupServiceStub : IAvailabilityGroupService
    {
        private readonly AvailabilityGroupModel _group;
        private readonly List<AvailabilityGroupMemberModel> _members;
        private readonly List<AvailabilityGroupDayModel> _days;

        public AvailabilityGroupServiceStub(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days)
        {
            _group = group;
            _members = members;
            _days = days;
        }

        public Task<AvailabilityGroupModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(id == _group.Id ? _group : null);

        public Task<List<AvailabilityGroupModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<AvailabilityGroupModel> { _group });

        public Task<AvailabilityGroupModel> CreateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
            => Task.FromResult(entity);

        public Task UpdateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(new List<AvailabilityGroupModel>());

        public Task SaveGroupAsync(AvailabilityGroupModel group, IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)> LoadFullAsync(int groupId, CancellationToken ct = default)
            => Task.FromResult((_group, _members, _days));

        public Task<List<EmployeeAvailabilityModel>> GetPublishedForEmployeeAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeAvailabilityModel>());

        public Task<EmployeeAvailabilityModel> GetPublishedForEmployeeByIdAsync(int employeeId, int groupId, DateTimeOffset nowUtc, CancellationToken ct = default)
            => Task.FromResult(new EmployeeAvailabilityModel
            {
                Group = _group,
                Member = _members.FirstOrDefault(member => member.EmployeeId == employeeId) ?? new AvailabilityGroupMemberModel(),
                Days = GetDaysForEmployee(employeeId),
            });

        public Task<EmployeeAvailabilityModel> SaveEmployeeAvailabilityAsync(
            int employeeId,
            int groupId,
            IList<AvailabilityGroupDayModel> days,
            DateTimeOffset nowUtc,
            CancellationToken ct = default)
            => Task.FromResult(new EmployeeAvailabilityModel
            {
                Group = _group,
                Member = _members.FirstOrDefault(member => member.EmployeeId == employeeId) ?? new AvailabilityGroupMemberModel(),
                Days = days.ToList(),
                CanSubmit = true,
            });

        private List<AvailabilityGroupDayModel> GetDaysForEmployee(int employeeId)
        {
            var member = _members.FirstOrDefault(member => member.EmployeeId == employeeId);
            return member is null
                ? []
                : _days.Where(day => day.AvailabilityGroupMemberId == member.Id).ToList();
        }

        public Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default)
            => Task.FromResult(_members);

        public Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default)
            => Task.FromResult(_days);

        public Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteSlotAsync(int groupId, int slotId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class EmployeeServiceStub : IEmployeeService
    {
        private readonly EmployeeModel _employee;

        public EmployeeServiceStub(EmployeeModel employee)
        {
            _employee = employee;
        }

        public Task<EmployeeModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(id == _employee.Id ? _employee : null);

        public Task<List<EmployeeModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeModel> { _employee });

        public Task<EmployeeModel> CreateAsync(EmployeeModel entity, CancellationToken ct = default)
            => Task.FromResult(entity);

        public Task UpdateAsync(EmployeeModel entity, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeModel>());

        public Task<EmployeeModel> UpdateContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default)
        {
            _employee.Email = email;
            _employee.Phone = phone;
            return Task.FromResult(_employee);
        }

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteOperationResult.Success());
    }

    private sealed class ShopServiceStub : IShopService
    {
        private readonly ShopModel _shop;

        public ShopServiceStub(ShopModel shop)
        {
            _shop = shop;
        }

        public Task<ShopModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(id == _shop.Id ? _shop : null);

        public Task<List<ShopModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<ShopModel> { _shop });

        public Task<ShopModel> CreateAsync(ShopModel entity, CancellationToken ct = default)
            => Task.FromResult(entity);

        public Task UpdateAsync(ShopModel entity, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(new List<ShopModel>());

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteOperationResult.Success());
    }

    private sealed class ShopFacadeStub : IShopFacade
    {
        public BusinessLogicLayer.Contracts.Shops.ShopDto? CreatedShop { get; init; }
        public BusinessLogicLayer.Contracts.Shops.ShopDto? ExistingShop { get; init; }
        public DeleteOperationResult DeleteResult { get; init; } = DeleteOperationResult.Success();

        public Task<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>>(ExistingShop is null ? [] : [ExistingShop]);

        public Task<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>>([]);

        public Task<BusinessLogicLayer.Contracts.Shops.ShopDto?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(ExistingShop?.Id == id ? ExistingShop : null);

        public Task<BusinessLogicLayer.Contracts.Shops.ShopDto> CreateAsync(SaveShopRequest request, CancellationToken ct = default)
            => Task.FromResult(CreatedShop ?? new BusinessLogicLayer.Contracts.Shops.ShopDto
            {
                Id = request.Id,
                Name = request.Name,
                Address = request.Address,
                Description = request.Description,
            });

        public Task UpdateAsync(SaveShopRequest request, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteResult);
    }

    private sealed class GraphExportServiceStub : IGraphExportService
    {
        public byte[] GraphSqlBytes { get; init; } = [];
        public byte[] ContainerSqlBytes { get; init; } = [];

        public Task<byte[]> ExportGraphSqlAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
            => Task.FromResult(GraphSqlBytes);

        public Task<byte[]> ExportGraphExcelCsvAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
            => Task.FromResult(Array.Empty<byte>());

        public Task<byte[]> ExportContainerSqlAsync(int containerId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
            => Task.FromResult(ContainerSqlBytes);
    }

    private sealed class GraphTemplateExportServiceStub : IGraphTemplateExportService
    {
        public (byte[] content, string fileName) GraphExcel { get; init; }
        public (byte[] content, string fileName) ContainerExcel { get; init; }

        public Task<(byte[] content, string fileName)> ExportGraphToXlsxAsync(int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
            => Task.FromResult(GraphExcel);

        public Task<(byte[] content, string fileName)> ExportContainerToXlsxAsync(int containerId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
            => Task.FromResult(ContainerExcel);
    }
}
