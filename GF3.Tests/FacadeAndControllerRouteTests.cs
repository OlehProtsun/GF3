using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Availability;
using BusinessLogicLayer.Contracts.Database;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Shops;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApi.Controllers;
using WebApi.Contracts.AdminDb;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;
using WebApi.Contracts.Containers;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Contracts.Containers.Graphs.CellStyles;
using WebApi.Contracts.Containers.Graphs.Employees;
using WebApi.Contracts.Containers.Graphs.Slots;
using WebApi.Contracts.Containers.SchedulePresets;
using WebApi.Options;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace GF3.Tests;

public sealed class FacadeAndControllerRouteTests
{
    [Fact]
    public async Task EmployeeFacade_MapsCrudAndSearchOperations_ToServiceContracts()
    {
        var employee = TestDataFactory.CreateEmployeeModel(id: 3, firstName: "John", lastName: "Smith", phone: "123", email: "john@example.com");
        var createdEmployee = TestDataFactory.CreateEmployeeModel(id: 9, firstName: "Anna", lastName: "Brown", phone: "555", email: "anna@example.com");
        var service = new RecordingEmployeeService
        {
            ExistingEmployee = employee,
            AllEmployees = [employee],
            SearchEmployees = [employee],
            CreatedEmployee = createdEmployee,
            DeleteResult = DeleteOperationResult.Failure("Blocked."),
        };
        var facade = TestEmployeeFacadeFactory.Create(service);

        var all = await facade.GetAllAsync(CancellationToken.None);
        var search = await facade.GetByValueAsync("john", CancellationToken.None);
        var byId = await facade.GetAsync(employee.Id, CancellationToken.None);
        var created = await facade.CreateAsync(new SaveEmployeeRequest
        {
            FirstName = "Anna",
            LastName = "Brown",
            Phone = "555",
            Email = "anna@example.com",
        }, CancellationToken.None);

        await facade.UpdateAsync(new SaveEmployeeRequest
        {
            Id = 5,
            FirstName = "Updated",
            LastName = "Person",
            Phone = "999",
            Email = "updated@example.com",
        }, CancellationToken.None);
        await facade.DeleteAsync(7, CancellationToken.None);
        var deleteResult = await facade.TryDeleteAsync(7, CancellationToken.None);

        Assert.Single(all);
        Assert.Single(search);
        Assert.NotNull(byId);
        Assert.Equal(employee.Id, byId!.Id);
        Assert.Equal("John", byId.FirstName);
        Assert.Equal(createdEmployee.Id, created.Id);
        Assert.Equal("Anna", created.FirstName);
        Assert.NotNull(service.LastCreatedModel);
        Assert.Equal("Anna", service.LastCreatedModel!.FirstName);
        Assert.NotNull(service.LastUpdatedModel);
        Assert.Equal(5, service.LastUpdatedModel!.Id);
        Assert.Equal(7, service.LastDeletedId);
        Assert.False(deleteResult.Succeeded);
        Assert.Equal("Blocked.", deleteResult.Message);
    }

    [Fact]
    public async Task ShopFacade_MapsCrudAndSearchOperations_ToServiceContracts()
    {
        var shop = TestDataFactory.CreateShopModel(id: 2, name: "North Shop", address: "Main 1", description: "Desc");
        var createdShop = TestDataFactory.CreateShopModel(id: 7, name: "West Shop", address: "West 1", description: "Fresh");
        var service = new RecordingShopService
        {
            ExistingShop = shop,
            AllShops = [shop],
            SearchShops = [shop],
            CreatedShop = createdShop,
            DeleteResult = DeleteOperationResult.Success(),
        };
        var facade = new ShopFacade(service);

        var all = await facade.GetAllAsync(CancellationToken.None);
        var search = await facade.GetByValueAsync("north", CancellationToken.None);
        var byId = await facade.GetAsync(shop.Id, CancellationToken.None);
        var created = await facade.CreateAsync(new SaveShopRequest
        {
            Name = "West Shop",
            Address = "West 1",
            Description = "Fresh",
        }, CancellationToken.None);

        await facade.UpdateAsync(new SaveShopRequest
        {
            Id = 5,
            Name = "Updated Shop",
            Address = "Updated 1",
            Description = "Updated",
        }, CancellationToken.None);
        await facade.DeleteAsync(8, CancellationToken.None);
        var deleteResult = await facade.TryDeleteAsync(8, CancellationToken.None);

        Assert.Single(all);
        Assert.Single(search);
        Assert.NotNull(byId);
        Assert.Equal(shop.Id, byId!.Id);
        Assert.Equal(createdShop.Id, created.Id);
        Assert.Equal("West Shop", created.Name);
        Assert.NotNull(service.LastCreatedModel);
        Assert.Equal("West Shop", service.LastCreatedModel!.Name);
        Assert.NotNull(service.LastUpdatedModel);
        Assert.Equal("Updated Shop", service.LastUpdatedModel!.Name);
        Assert.Equal(8, service.LastDeletedId);
        Assert.True(deleteResult.Succeeded);
    }

    [Fact]
    public async Task SqliteAdminFacade_MapsLowLevelServiceResults()
    {
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add("employees");

        var databaseInfo = new DatabaseInfo
        {
            DatabasePath = "C:\\db\\gf3.db",
            FileSizeBytes = 1234,
            LastModifiedUtc = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc),
            UserVersion = 7,
            Tables = ["employees", "shops"],
        };
        var service = new RecordingSqliteAdminService
        {
            DatabasePath = databaseInfo.DatabasePath,
            ExecuteResult = new SqlExecutionResult
            {
                IsSelect = true,
                ResultTable = table,
                AffectedRows = 0,
                Message = "ok",
            },
            DatabaseInfo = databaseInfo,
            FileHash = "abc123",
        };
        var facade = new SqliteAdminFacade(service);

        var execution = await facade.ExecuteSqlAsync("SELECT * FROM employees;", CancellationToken.None);
        await facade.ImportSqlScriptAsync("BEGIN TRANSACTION;", CancellationToken.None);
        var info = await facade.GetDatabaseInfoAsync(CancellationToken.None);
        var hash = await facade.ComputeFileHashAsync(databaseInfo.DatabasePath, CancellationToken.None);

        Assert.Equal(databaseInfo.DatabasePath, facade.DatabasePath);
        Assert.True(execution.IsSelect);
        Assert.Same(table, execution.ResultTable);
        Assert.Equal("ok", execution.Message);
        Assert.Equal("BEGIN TRANSACTION;", service.LastImportedSql);
        Assert.Equal(databaseInfo.DatabasePath, info.DatabasePath);
        Assert.Equal(7, info.UserVersion);
        Assert.Equal(["employees", "shops"], info.Tables);
        Assert.Equal("abc123", hash);
        Assert.Equal(databaseInfo.DatabasePath, service.LastHashedFilePath);
    }

    [Fact]
    public async Task AvailabilityGroupsController_RemainingRoutes_MapDtosAndTrackUpdates()
    {
        var employee = TestDataFactory.CreateEmployeeModel(id: 3, firstName: "John", lastName: "Smith");
        var group = new AvailabilityGroupModel { Id = 5, Name = "April", Year = 2026, Month = 4 };
        var member = new AvailabilityGroupMemberModel
        {
            Id = 7,
            AvailabilityGroupId = group.Id,
            EmployeeId = employee.Id,
            Employee = employee,
            DisplayOrder = 1,
        };
        var slot = new AvailabilityGroupDayModel
        {
            Id = 9,
            AvailabilityGroupMemberId = member.Id,
            DayOfMonth = 3,
            Kind = AvailabilityKind.INT,
            IntervalStr = "08:00 - 12:00",
        };
        member.Days.Add(slot);
        group.Members.Add(member);

        var service = new RecordingAvailabilityGroupService
        {
            Group = group,
            Groups = [group],
            Members = [member],
            Slots = [slot],
        };
        var controller = new AvailabilityGroupsController(service);
        SetHttpContext(controller);

        var allResult = await controller.GetAll(CancellationToken.None);
        var byIdResult = await controller.GetById(group.Id, CancellationToken.None);
        var itemsResult = await controller.GetItems(group.Id, CancellationToken.None);
        var membersResult = await controller.GetMembers(group.Id, CancellationToken.None);
        var slotsResult = await controller.GetSlots(group.Id, CancellationToken.None);
        var updateGroupResult = await controller.Update(group.Id, new UpdateAvailabilityGroupRequest
        {
            Name = "April Updated",
            Year = 2026,
            Month = 5,
        }, CancellationToken.None);
        var updateMemberResult = await controller.UpdateMember(group.Id, member.Id, new UpdateAvailabilityGroupMemberRequest
        {
            EmployeeId = 4,
            DisplayOrder = 2,
        }, CancellationToken.None);
        var updateSlotResult = await controller.UpdateSlot(group.Id, slot.Id, new UpdateAvailabilitySlotRequest
        {
            AvailabilityGroupMemberId = member.Id,
            DayOfMonth = 4,
            Kind = AvailabilityKind.ANY,
            IntervalStr = "10:00 - 14:00",
        }, CancellationToken.None);
        var deleteMemberResult = await controller.DeleteMember(group.Id, member.Id, CancellationToken.None);
        var deleteSlotResult = await controller.DeleteSlot(group.Id, slot.Id, CancellationToken.None);
        var deleteGroupResult = await controller.Delete(group.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(allResult.Result);
        Assert.IsType<OkObjectResult>(byIdResult.Result);
        var itemsOk = Assert.IsType<OkObjectResult>(itemsResult.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<AvailabilityGroupItemDto>>(itemsOk.Value));
        var membersOk = Assert.IsType<OkObjectResult>(membersResult.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<AvailabilityGroupMemberDto>>(membersOk.Value));
        var slotsOk = Assert.IsType<OkObjectResult>(slotsResult.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<AvailabilitySlotDto>>(slotsOk.Value));
        Assert.IsType<NoContentResult>(updateGroupResult);
        Assert.IsType<NoContentResult>(updateMemberResult);
        Assert.IsType<NoContentResult>(updateSlotResult);
        Assert.IsType<NoContentResult>(deleteMemberResult);
        Assert.IsType<NoContentResult>(deleteSlotResult);
        Assert.IsType<NoContentResult>(deleteGroupResult);
        Assert.NotNull(service.LastUpdatedGroup);
        Assert.Equal("April Updated", service.LastUpdatedGroup!.Name);
        Assert.NotNull(service.LastUpdatedMember);
        Assert.Equal(4, service.LastUpdatedMember!.EmployeeId);
        Assert.NotNull(service.LastUpdatedSlot);
        Assert.Equal(4, service.LastUpdatedSlot!.DayOfMonth);
        Assert.Equal(member.Id, service.LastDeletedMemberId);
        Assert.Equal(slot.Id, service.LastDeletedSlotId);
        Assert.Equal(group.Id, service.LastDeletedGroupId);
    }

    [Fact]
    public async Task AvailabilityGroupsController_ThrowsNotFound_WhenGroupIsMissing()
    {
        var controller = new AvailabilityGroupsController(new RecordingAvailabilityGroupService());
        SetHttpContext(controller);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => controller.GetById(99, CancellationToken.None));

        Assert.Equal("Availability group with id 99 was not found.", exception.Message);
    }

    [Fact]
    public async Task AdminDbController_MetadataHashImportSelectAndManualCopy_MapResponses()
    {
        var activeDb = new AdminDbFileEntryDto
        {
            Name = "SQLite.db",
            Path = "C:\\db\\SQLite.db",
            Category = "database",
            FileSizeBytes = 100,
            LastModifiedUtc = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc),
            IsActive = true,
        };
        var metadata = new AdminDbMetadataDto
        {
            SqliteVersion = "3.45.0",
            DatabasePath = activeDb.Path,
            FileSizeBytes = 100,
            LastModifiedUtc = activeDb.LastModifiedUtc,
            UserVersion = 7,
            Tables = ["employee"],
            Objects =
            [
                new AdminDbObjectDto
                {
                    Type = "table",
                    Name = "employee",
                    Sql = "CREATE TABLE employee (...)",
                }
            ],
            StorageWorkspace = new AdminDbStorageWorkspaceDto
            {
                WorkspaceRootPath = "C:\\db",
                AutomaticBackupDirectoryPath = "C:\\db\\backups",
                ManualCopyDirectoryPath = "C:\\db\\manual",
                AutomaticBackupRetentionLimit = 12,
                AvailableDatabases = [activeDb],
                AutomaticBackups = [new AdminDbFileEntryDto { Name = "backup.db", Path = "C:\\db\\backups\\backup.db", Category = "backup", FileSizeBytes = 50 }],
                ManualCopies = [new AdminDbFileEntryDto { Name = "manual.db", Path = "C:\\db\\manual\\manual.db", Category = "manual", FileSizeBytes = 50 }],
            }
        };
        var service = new RecordingAdminDbService
        {
            Metadata = metadata,
            DbHash = "hash-123",
            ImportResult = new AdminDbImportResultDto
            {
                StatementsExecuted = 3,
                StatementsApplied = 2,
                StatementsAlreadyExisted = 1,
            },
            ManualCopy = new AdminDbFileEntryDto { Name = "manual-copy.db", Path = "C:\\db\\manual\\manual-copy.db", Category = "manual" },
            SelectedDatabase = new AdminDbFileEntryDto { Name = "selected.db", Path = "C:\\db\\selected.db", Category = "database", IsActive = true },
        };
        var controller = new AdminDbController(
            service,
            Options.Create(new AdminToolsOptions
            {
                AllowWriteSql = true,
                MaxSqlLength = 500,
                MaxImportBytes = 5000,
            }));

        var metadataResult = Assert.IsType<OkObjectResult>(await controller.Metadata(CancellationToken.None));
        var hashResult = Assert.IsType<OkObjectResult>(await controller.Hash(CancellationToken.None));

        using var importStream = new MemoryStream(Encoding.UTF8.GetBytes("SELECT 1;"));
        var file = new FormFile(importStream, 0, importStream.Length, "file", "import.sql");
        var importResult = Assert.IsType<OkObjectResult>(await controller.Import(file, CancellationToken.None));
        var manualCopyResult = Assert.IsType<OkObjectResult>(await controller.CreateManualCopy(CancellationToken.None));
        var selectResult = Assert.IsType<OkObjectResult>(await controller.SelectDatabase(
            new AdminDbSelectDatabaseRequest { DatabasePath = "C:\\db\\selected.db" },
            CancellationToken.None));

        var metadataJson = SerializeResult(metadataResult.Value);
        var hashJson = SerializeResult(hashResult.Value);
        var importJson = SerializeResult(importResult.Value);
        var manualCopyJson = SerializeResult(manualCopyResult.Value);
        var selectJson = SerializeResult(selectResult.Value);

        Assert.Contains("\"sqliteVersion\":\"3.45.0\"", metadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"allowWriteSql\":true", metadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"workspaceRootPath\":\"C:\\\\db\"", metadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"hash\":\"hash-123\"", hashJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"statementsApplied\":2", importJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"name\":\"manual-copy.db\"", manualCopyJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"path\":\"C:\\\\db\\\\selected.db\"", selectJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("C:\\db\\selected.db", service.LastSelectedDatabasePath);
        Assert.Equal("SELECT 1;", Encoding.UTF8.GetString(service.LastImportedBytes!));
    }

    [Fact]
    public async Task AdminDbController_ValidatesMissingSqlPathAndWriteProtection()
    {
        var controller = new AdminDbController(
            new RecordingAdminDbService(),
            Options.Create(new AdminToolsOptions
            {
                AllowWriteSql = false,
                MaxSqlLength = 100,
                MaxImportBytes = 100,
            }));

        await Assert.ThrowsAsync<DataAnnotationsValidationException>(() =>
            controller.Query(new AdminDbSqlRequest { Sql = " " }, CancellationToken.None));

        await Assert.ThrowsAsync<DataAnnotationsValidationException>(() =>
            controller.Execute(new AdminDbSqlRequest { Sql = "DELETE FROM employee;" }, CancellationToken.None));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("DELETE FROM employee;"));
        var file = new FormFile(stream, 0, stream.Length, "file", "import.sql");
        await Assert.ThrowsAsync<DataAnnotationsValidationException>(() =>
            controller.Import(file, CancellationToken.None));

        await Assert.ThrowsAsync<DataAnnotationsValidationException>(() =>
            controller.SelectDatabase(new AdminDbSelectDatabaseRequest { DatabasePath = " " }, CancellationToken.None));
    }

    [Fact]
    public async Task ContainersController_RemainingNestedRoutes_ReturnExpectedResults()
    {
        var container = new ContainerModel { Id = 1, Name = "Main Container", Note = "Note" };
        var graph = TestDataFactory.CreateScheduleModel(id: 5, containerId: container.Id, shopId: 2, name: "April Graph", year: 2026, month: 4, availabilityGroupId: null);
        var preset = new SchedulePresetModel
        {
            Id = 8,
            ContainerId = container.Id,
            Name = "April Preset",
            ScheduleName = "Preset Graph",
            ShopId = 2,
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            Employees = [new SchedulePresetEmployeeModel { Id = 9, EmployeeId = 3, MinHoursMonth = 80 }],
        };
        var graphSlot = new ScheduleSlotModel
        {
            Id = 10,
            ScheduleId = graph.Id,
            DayOfMonth = 1,
            SlotNo = 1,
            EmployeeId = 3,
            Status = SlotStatus.ASSIGNED,
            FromTime = "08:00",
            ToTime = "16:00",
        };
        var graphEmployee = new ScheduleEmployeeModel
        {
            Id = 11,
            ScheduleId = graph.Id,
            EmployeeId = 3,
            DisplayOrder = 0,
            MinHoursMonth = 80,
            Employee = TestDataFactory.CreateEmployeeModel(id: 3, firstName: "John", lastName: "Smith"),
        };
        var style = new ScheduleCellStyleModel
        {
            Id = 12,
            ScheduleId = graph.Id,
            DayOfMonth = 1,
            EmployeeId = 3,
            BackgroundColorArgb = 10,
            TextColorArgb = 20,
        };
        var service = new RecordingContainerService
        {
            Container = container,
            Graph = graph,
            Presets = [preset],
            Slots = [graphSlot],
            Employees = [graphEmployee],
            Styles = [style],
            PreviewResult = new GenerateGraphResult
            {
                ContainerId = container.Id,
                GraphId = graph.Id,
                GeneratedSlotsCount = 1,
                WrittenSlotsCount = 0,
                Slots = [graphSlot],
            },
        };
        var controller = new ContainersController(service, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetHttpContext(controller);

        var allResult = await controller.GetAll(CancellationToken.None);
        var presetResult = await controller.GetSchedulePresets(container.Id, CancellationToken.None);
        var createPresetResult = await controller.CreateSchedulePreset(container.Id, new CreateSchedulePresetRequest
        {
            Name = "Created Preset",
            ScheduleName = "Generated Graph",
            ShopId = 2,
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            Employees = [new CreateSchedulePresetEmployeeRequest { EmployeeId = 3, MinHoursMonth = 80 }],
        }, CancellationToken.None);
        var graphByIdResult = await controller.GetGraphById(container.Id, graph.Id, CancellationToken.None);
        var createGraphResult = await controller.CreateGraph(container.Id, new CreateGraphRequest
        {
            ShopId = 2,
            Name = "Created Graph",
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            Note = "New note",
        }, CancellationToken.None);
        var updateGraphResult = await controller.UpdateGraph(container.Id, graph.Id, new UpdateGraphRequest
        {
            ShopId = 2,
            Name = "Updated Graph",
            Year = 2026,
            Month = 5,
            PeoplePerShift = 2,
            Shift1Time = "07:00 - 15:00",
            Shift2Time = "15:00 - 19:00",
            MaxHoursPerEmpMonth = 170,
            MaxConsecutiveDays = 6,
            MaxConsecutiveFull = 4,
            MaxFullPerMonth = 11,
            Note = "Updated note",
        }, CancellationToken.None);
        var deleteGraphResult = await controller.DeleteGraph(container.Id, graph.Id, CancellationToken.None);
        var previewResult = await controller.GenerateGraphPreview(container.Id, new GenerateGraphPreviewRequest
        {
            GraphId = graph.Id,
            Graph = new CreateGraphRequest
            {
                ShopId = 2,
                Name = "Preview Graph",
                Year = 2026,
                Month = 4,
                PeoplePerShift = 1,
                Shift1Time = "08:00 - 16:00",
                Shift2Time = "16:00 - 20:00",
                MaxHoursPerEmpMonth = 160,
                MaxConsecutiveDays = 5,
                MaxConsecutiveFull = 3,
                MaxFullPerMonth = 10,
            },
            Employees = [new GenerateGraphPreviewEmployeeRequest { EmployeeId = 3, MinHoursMonth = 80, DisplayOrder = 0 }],
        }, CancellationToken.None);
        var graphSlotsResult = await controller.GetGraphSlots(container.Id, graph.Id, CancellationToken.None);
        var replaceSlotsResult = await controller.ReplaceGraphSlots(container.Id, graph.Id, new ReplaceGraphSlotsRequest
        {
            Slots =
            [
                new CreateGraphSlotRequest
                {
                    DayOfMonth = 2,
                    SlotNo = 2,
                    FromTime = "10:00",
                    ToTime = "14:00",
                    EmployeeId = 3,
                    Status = SlotStatus.ASSIGNED,
                }
            ]
        }, CancellationToken.None);
        var createSlotResult = await controller.CreateGraphSlot(container.Id, graph.Id, new CreateGraphSlotRequest
        {
            DayOfMonth = 3,
            SlotNo = 1,
            FromTime = "08:00",
            ToTime = "12:00",
            EmployeeId = 3,
            Status = SlotStatus.ASSIGNED,
        }, CancellationToken.None);
        var updateSlotResult = await controller.UpdateGraphSlot(container.Id, graph.Id, graphSlot.Id, new UpdateGraphSlotRequest
        {
            DayOfMonth = 4,
            SlotNo = 1,
            FromTime = "09:00",
            ToTime = "13:00",
            EmployeeId = 3,
            Status = SlotStatus.ASSIGNED,
        }, CancellationToken.None);
        var deleteSlotResult = await controller.DeleteGraphSlot(container.Id, graph.Id, graphSlot.Id, CancellationToken.None);
        var graphEmployeesResult = await controller.GetGraphEmployees(container.Id, graph.Id, CancellationToken.None);
        var addEmployeeResult = await controller.AddGraphEmployee(container.Id, graph.Id, new AddGraphEmployeeRequest
        {
            EmployeeId = 3,
            MinHoursMonth = 90,
            DisplayOrder = 1,
        }, CancellationToken.None);
        var updateEmployeeResult = await controller.UpdateGraphEmployee(container.Id, graph.Id, graphEmployee.Id, new UpdateGraphEmployeeRequest
        {
            EmployeeId = 4,
            MinHoursMonth = 100,
            DisplayOrder = 2,
        }, CancellationToken.None);
        var removeEmployeeResult = await controller.RemoveGraphEmployee(container.Id, graph.Id, graphEmployee.Id, CancellationToken.None);
        var stylesResult = await controller.GetGraphCellStyles(container.Id, graph.Id, CancellationToken.None);
        var upsertStyleResult = await controller.UpsertGraphCellStyle(container.Id, graph.Id, new UpsertGraphCellStyleRequest
        {
            DayOfMonth = 5,
            EmployeeId = 3,
            BackgroundColorArgb = 30,
            TextColorArgb = 40,
        }, CancellationToken.None);
        var deleteStyleResult = await controller.DeleteGraphCellStyle(container.Id, graph.Id, style.Id, CancellationToken.None);
        var updateContainerResult = await controller.Update(container.Id, new UpdateContainerRequest
        {
            Name = "Updated Container",
            Note = "Updated note",
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(allResult.Result);
        Assert.IsType<OkObjectResult>(presetResult.Result);
        Assert.IsType<CreatedAtActionResult>(createPresetResult.Result);
        Assert.IsType<OkObjectResult>(graphByIdResult.Result);
        Assert.IsType<CreatedAtActionResult>(createGraphResult.Result);
        Assert.IsType<NoContentResult>(updateGraphResult);
        Assert.IsType<NoContentResult>(deleteGraphResult);
        var previewOk = Assert.IsType<OkObjectResult>(previewResult.Result);
        var previewPayload = Assert.IsType<GenerateGraphResponse>(previewOk.Value);
        Assert.Equal(1, previewPayload.GeneratedSlotsCount);
        Assert.Single(previewPayload.Slots!);
        Assert.IsType<OkObjectResult>(graphSlotsResult.Result);
        Assert.IsType<NoContentResult>(replaceSlotsResult);
        Assert.IsType<CreatedAtActionResult>(createSlotResult.Result);
        Assert.IsType<NoContentResult>(updateSlotResult);
        Assert.IsType<NoContentResult>(deleteSlotResult);
        Assert.IsType<OkObjectResult>(graphEmployeesResult.Result);
        Assert.IsType<CreatedAtActionResult>(addEmployeeResult.Result);
        Assert.IsType<NoContentResult>(updateEmployeeResult);
        Assert.IsType<NoContentResult>(removeEmployeeResult);
        Assert.IsType<OkObjectResult>(stylesResult.Result);
        Assert.IsType<OkObjectResult>(upsertStyleResult.Result);
        Assert.IsType<NoContentResult>(deleteStyleResult);
        Assert.IsType<NoContentResult>(updateContainerResult);
        Assert.NotNull(service.LastUpdatedContainer);
        Assert.Equal("Updated Container", service.LastUpdatedContainer!.Name);
        Assert.NotNull(service.LastUpdatedGraph);
        Assert.Equal("Updated Graph", service.LastUpdatedGraph!.Name);
        Assert.NotNull(service.LastReplacedSlots);
        Assert.Single(service.LastReplacedSlots!);
        Assert.Equal(2, service.LastReplacedSlots![0].DayOfMonth);
        Assert.NotNull(service.LastUpdatedGraphSlot);
        Assert.Equal("09:00", service.LastUpdatedGraphSlot!.FromTime);
        Assert.NotNull(service.LastUpdatedGraphEmployee);
        Assert.Equal(4, service.LastUpdatedGraphEmployee!.EmployeeId);
        Assert.NotNull(service.LastUpsertedStyle);
        Assert.Equal(30, service.LastUpsertedStyle!.BackgroundColorArgb);
    }

    private static void SetHttpContext(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
    }

    private static string SerializeResult(object? value)
        => System.Text.Json.JsonSerializer.Serialize(value);

    private sealed class RecordingEmployeeService : IEmployeeService
    {
        public EmployeeModel? ExistingEmployee { get; init; }
        public List<EmployeeModel> AllEmployees { get; init; } = [];
        public List<EmployeeModel> SearchEmployees { get; init; } = [];
        public EmployeeModel? CreatedEmployee { get; init; }
        public DeleteOperationResult DeleteResult { get; init; } = DeleteOperationResult.Success();
        public EmployeeModel? LastCreatedModel { get; private set; }
        public EmployeeModel? LastUpdatedModel { get; private set; }
        public int? LastDeletedId { get; private set; }

        public Task<EmployeeModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(ExistingEmployee?.Id == id ? ExistingEmployee : null);

        public Task<List<EmployeeModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(AllEmployees);

        public Task<EmployeeModel> CreateAsync(EmployeeModel entity, CancellationToken ct = default)
        {
            LastCreatedModel = entity;
            return Task.FromResult(CreatedEmployee ?? entity);
        }

        public Task UpdateAsync(EmployeeModel entity, CancellationToken ct = default)
        {
            LastUpdatedModel = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            LastDeletedId = id;
            return Task.CompletedTask;
        }

        public Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(SearchEmployees);

        public Task<EmployeeModel> UpdateContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default)
        {
            var updatedEmployee = ExistingEmployee ?? new EmployeeModel { Id = employeeId };
            updatedEmployee.Email = email;
            updatedEmployee.Phone = phone;
            LastUpdatedModel = updatedEmployee;
            return Task.FromResult(updatedEmployee);
        }

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteResult);
    }

    private sealed class RecordingShopService : IShopService
    {
        public ShopModel? ExistingShop { get; init; }
        public List<ShopModel> AllShops { get; init; } = [];
        public List<ShopModel> SearchShops { get; init; } = [];
        public ShopModel? CreatedShop { get; init; }
        public DeleteOperationResult DeleteResult { get; init; } = DeleteOperationResult.Success();
        public ShopModel? LastCreatedModel { get; private set; }
        public ShopModel? LastUpdatedModel { get; private set; }
        public int? LastDeletedId { get; private set; }

        public Task<ShopModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(ExistingShop?.Id == id ? ExistingShop : null);

        public Task<List<ShopModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(AllShops);

        public Task<ShopModel> CreateAsync(ShopModel entity, CancellationToken ct = default)
        {
            LastCreatedModel = entity;
            return Task.FromResult(CreatedShop ?? entity);
        }

        public Task UpdateAsync(ShopModel entity, CancellationToken ct = default)
        {
            LastUpdatedModel = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            LastDeletedId = id;
            return Task.CompletedTask;
        }

        public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(SearchShops);

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteResult);
    }

    private sealed class RecordingSqliteAdminService : ISqliteAdminService
    {
        public string DatabasePath { get; init; } = string.Empty;
        public SqlExecutionResult ExecuteResult { get; init; } = new();
        public DatabaseInfo DatabaseInfo { get; init; } = new();
        public string FileHash { get; init; } = string.Empty;
        public string? LastImportedSql { get; private set; }
        public string? LastHashedFilePath { get; private set; }

        public Task<SqlExecutionResult> ExecuteSqlAsync(string sql, CancellationToken ct)
            => Task.FromResult(ExecuteResult);

        public Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct)
        {
            LastImportedSql = sqlScript;
            return Task.CompletedTask;
        }

        public Task<DatabaseInfo> GetDatabaseInfoAsync(CancellationToken ct)
            => Task.FromResult(DatabaseInfo);

        public Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
        {
            LastHashedFilePath = filePath;
            return Task.FromResult(FileHash);
        }
    }

    private sealed class RecordingAvailabilityGroupService : IAvailabilityGroupService
    {
        public AvailabilityGroupModel? Group { get; init; }
        public List<AvailabilityGroupModel> Groups { get; init; } = [];
        public List<AvailabilityGroupMemberModel> Members { get; init; } = [];
        public List<AvailabilityGroupDayModel> Slots { get; init; } = [];
        public AvailabilityGroupModel? LastUpdatedGroup { get; private set; }
        public AvailabilityGroupMemberModel? LastUpdatedMember { get; private set; }
        public AvailabilityGroupDayModel? LastUpdatedSlot { get; private set; }
        public int? LastDeletedMemberId { get; private set; }
        public int? LastDeletedSlotId { get; private set; }
        public int? LastDeletedGroupId { get; private set; }

        public Task<AvailabilityGroupModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(Group?.Id == id ? Group : null);

        public Task<List<AvailabilityGroupModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Groups);

        public Task<AvailabilityGroupModel> CreateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
            => Task.FromResult(entity);

        public Task UpdateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
        {
            LastUpdatedGroup = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            LastDeletedGroupId = id;
            return Task.CompletedTask;
        }

        public Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(Groups);

        public Task SaveGroupAsync(AvailabilityGroupModel group, IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)> LoadFullAsync(int groupId, CancellationToken ct = default)
            => Task.FromResult((Group ?? new AvailabilityGroupModel(), Members, Slots));

        public Task<List<EmployeeAvailabilityModel>> GetPublishedForEmployeeAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeAvailabilityModel>());

        public Task<EmployeeAvailabilityModel> GetPublishedForEmployeeByIdAsync(int employeeId, int groupId, DateTimeOffset nowUtc, CancellationToken ct = default)
            => Task.FromResult(new EmployeeAvailabilityModel
            {
                Group = Group ?? new AvailabilityGroupModel(),
                Member = Members.FirstOrDefault(member => member.EmployeeId == employeeId) ?? new AvailabilityGroupMemberModel(),
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
                Group = Group ?? new AvailabilityGroupModel(),
                Member = Members.FirstOrDefault(member => member.EmployeeId == employeeId) ?? new AvailabilityGroupMemberModel(),
                Days = days.ToList(),
                CanSubmit = true,
            });

        private List<AvailabilityGroupDayModel> GetDaysForEmployee(int employeeId)
        {
            var member = Members.FirstOrDefault(member => member.EmployeeId == employeeId);
            return member is null
                ? []
                : Slots.Where(slot => slot.AvailabilityGroupMemberId == member.Id).ToList();
        }

        public Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default)
            => Task.FromResult(Members);

        public Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
        {
            LastUpdatedMember = model;
            return Task.CompletedTask;
        }

        public Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default)
        {
            LastDeletedMemberId = memberId;
            return Task.CompletedTask;
        }

        public Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default)
            => Task.FromResult(Slots);

        public Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default)
            => Task.FromResult(model);

        public Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default)
        {
            LastUpdatedSlot = model;
            return Task.CompletedTask;
        }

        public Task DeleteSlotAsync(int groupId, int slotId, CancellationToken ct = default)
        {
            LastDeletedSlotId = slotId;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAdminDbService : IAdminDbService
    {
        public AdminDbMetadataDto Metadata { get; init; } = new();
        public string DbHash { get; init; } = string.Empty;
        public AdminDbImportResultDto ImportResult { get; init; } = new();
        public AdminDbFileEntryDto ManualCopy { get; init; } = new();
        public AdminDbFileEntryDto SelectedDatabase { get; init; } = new();
        public string? LastSelectedDatabasePath { get; private set; }
        public byte[]? LastImportedBytes { get; private set; }

        public Task<AdminDbMetadataDto> GetMetadataAsync(CancellationToken ct = default)
            => Task.FromResult(Metadata);

        public Task<string> GetDbHashAsync(CancellationToken ct = default)
            => Task.FromResult(DbHash);

        public Task<AdminDbQueryResultDto> ExecuteQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default)
            => Task.FromResult(new AdminDbQueryResultDto());

        public Task<int> ExecuteNonQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default)
            => Task.FromResult(1);

        public Task<AdminDbImportResultDto> ImportSqlAsync(byte[] fileBytes, int maxImportBytes, CancellationToken ct = default)
        {
            LastImportedBytes = fileBytes;
            return Task.FromResult(ImportResult);
        }

        public Task<AdminDbFileEntryDto> CreateManualCopyAsync(CancellationToken ct = default)
            => Task.FromResult(ManualCopy);

        public Task<AdminDbFileEntryDto> SelectDatabaseAsync(string databasePath, CancellationToken ct = default)
        {
            LastSelectedDatabasePath = databasePath;
            return Task.FromResult(SelectedDatabase);
        }
    }

    private sealed class RecordingContainerService : IContainerService
    {
        public ContainerModel? Container { get; init; }
        public ScheduleModel? Graph { get; init; }
        public List<SchedulePresetModel> Presets { get; init; } = [];
        public List<ScheduleSlotModel> Slots { get; init; } = [];
        public List<ScheduleEmployeeModel> Employees { get; init; } = [];
        public List<ScheduleCellStyleModel> Styles { get; init; } = [];
        public GenerateGraphResult PreviewResult { get; init; } = new();
        public ContainerModel? LastUpdatedContainer { get; private set; }
        public ScheduleModel? LastUpdatedGraph { get; private set; }
        public List<ScheduleSlotModel>? LastReplacedSlots { get; private set; }
        public ScheduleSlotModel? LastUpdatedGraphSlot { get; private set; }
        public ScheduleEmployeeModel? LastUpdatedGraphEmployee { get; private set; }
        public ScheduleCellStyleModel? LastUpsertedStyle { get; private set; }

        public Task<ContainerModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(Container?.Id == id ? Container : null);

        public Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Container is null ? new List<ContainerModel>() : [Container]);

        public Task<ContainerModel> CreateAsync(ContainerModel entity, CancellationToken ct = default)
        {
            entity.Id = 20;
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(ContainerModel entity, CancellationToken ct = default)
        {
            LastUpdatedContainer = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => Task.FromResult(new List<ContainerModel>());

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => Task.FromResult(DeleteOperationResult.Success());

        public Task<List<ScheduleModel>?> GetGraphsAsync(int containerId, CancellationToken ct = default)
            => Task.FromResult(Graph is null ? null : (List<ScheduleModel>?) [Graph]);

        public Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult(Graph?.Id == graphId ? Graph : null);

        public Task<List<ScheduleModel>> GetPublishedGraphsForEmployeeAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(Graph is null ? new List<ScheduleModel>() : [Graph]);

        public Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default)
        {
            model.Id = 21;
            return Task.FromResult(model);
        }

        public Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default)
        {
            LastUpdatedGraph = model;
            return Task.CompletedTask;
        }

        public Task<int> UpdateGraphPublicationAsync(int containerId, SchedulePublicationStatus publicationStatus, bool? allowSwap, CancellationToken ct = default)
            => Task.FromResult(Graph is null ? 0 : 1);

        public Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<SchedulePresetModel>?> GetSchedulePresetsAsync(int containerId, CancellationToken ct = default)
            => Task.FromResult<List<SchedulePresetModel>?>(Presets);

        public Task<SchedulePresetModel> CreateSchedulePresetAsync(int containerId, SchedulePresetModel model, CancellationToken ct = default)
        {
            model.Id = 22;
            return Task.FromResult(model);
        }

        public Task<GenerateGraphResult> GenerateGraphAsync(int containerId, int graphId, bool overwrite, bool dryRun, IProgress<int>? progress, CancellationToken ct = default)
            => Task.FromResult(PreviewResult);

        public Task<GenerateGraphResult> GenerateGraphPreviewAsync(int containerId, ScheduleModel model, IEnumerable<ScheduleEmployeeModel> employees, IProgress<int>? progress, CancellationToken ct = default)
            => Task.FromResult(PreviewResult);

        public Task<List<ScheduleSlotModel>?> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult<List<ScheduleSlotModel>?>(Slots);

        public Task ReplaceGraphSlotsAsync(int containerId, int graphId, IEnumerable<ScheduleSlotModel> slots, CancellationToken ct = default)
        {
            LastReplacedSlots = slots.ToList();
            return Task.CompletedTask;
        }

        public Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default)
        {
            model.Id = 23;
            return Task.FromResult(model);
        }

        public Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default)
        {
            LastUpdatedGraphSlot = model;
            return Task.CompletedTask;
        }

        public Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ScheduleEmployeeModel>?> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult<List<ScheduleEmployeeModel>?>(Employees);

        public Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default)
        {
            model.Id = 24;
            return Task.FromResult(model);
        }

        public Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default)
        {
            LastUpdatedGraphEmployee = model;
            return Task.CompletedTask;
        }

        public Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ScheduleCellStyleModel>?> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult<List<ScheduleCellStyleModel>?>(Styles);

        public Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default)
        {
            LastUpsertedStyle = model;
            model.Id = 25;
            return Task.FromResult(model);
        }

        public Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
