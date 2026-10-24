using System.Security.Claims;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Controllers;
using WebApi.Contracts.AvailabilityBinds;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;
using WebApi.Contracts.Containers;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Contracts.Containers.Graphs.Slots;
using WebApi.Contracts.Employees;
using WebApi.Contracts.Shops;
using WebApi.Auth;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class RemainingControllerAndAvailabilityCoverageTests
{
    [Fact]
    public async Task AvailabilityGroupService_UpdateDeleteAndSearchPaths_WorkForMembersAndSlots()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateAvailabilityGroupService(context);

        var employee1 = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        var employee2 = TestDataFactory.CreateDalEmployee("Bob", "Smith");
        context.Employees.AddRange(employee1, employee2);
        await context.SaveChangesAsync();

        var group = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel(name: "April"));
        var member1 = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel
        {
            EmployeeId = employee1.Id,
            DisplayOrder = 0,
        });
        var member2 = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel
        {
            EmployeeId = employee2.Id,
            DisplayOrder = 1,
        });
        var slot1 = await service.CreateSlotAsync(group.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member1.Id,
            DayOfMonth = 2,
            Kind = AvailabilityKind.ANY,
        });
        var slot2 = await service.CreateSlotAsync(group.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member2.Id,
            DayOfMonth = 3,
            Kind = AvailabilityKind.INT,
            IntervalStr = "08:00 - 12:00",
        });

        await service.UpdateAsync(new AvailabilityGroupModel
        {
            Id = group.Id,
            Name = " April Final ",
            Year = 2026,
            Month = 5,
        });
        await service.UpdateMemberAsync(group.Id, member1.Id, new AvailabilityGroupMemberModel
        {
            EmployeeId = employee1.Id,
            DisplayOrder = 5,
        });
        await service.UpdateSlotAsync(group.Id, slot1.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member1.Id,
            DayOfMonth = 4,
            Kind = AvailabilityKind.INT,
            IntervalStr = "10:00 - 14:00",
        });

        var groupsByValue = await service.GetByValueAsync("final");
        var members = await service.GetMembersAsync(group.Id);
        var slots = await service.GetSlotsAsync(group.Id);

        Assert.Single(groupsByValue);
        Assert.Equal("April Final", groupsByValue[0].Name);
        Assert.Contains(members, member => member.Id == member1.Id && member.DisplayOrder == 5);
        Assert.Contains(slots, slot => slot.Id == slot1.Id && slot.DayOfMonth == 4 && slot.IntervalStr == "10:00 - 14:00");

        await service.DeleteSlotAsync(group.Id, slot1.Id);
        await service.DeleteMemberAsync(group.Id, member2.Id);

        var membersAfterDelete = await service.GetMembersAsync(group.Id);
        var slotsAfterDelete = await service.GetSlotsAsync(group.Id);

        Assert.Single(membersAfterDelete);
        Assert.Empty(slotsAfterDelete);
        Assert.DoesNotContain(membersAfterDelete, member => member.Id == member2.Id);
        Assert.DoesNotContain(slotsAfterDelete, slot => slot.Id == slot1.Id || slot.Id == slot2.Id);

        var missingMemberException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteMemberAsync(group.Id, 999));
        Assert.Equal("Availability group member with id 999 was not found.", missingMemberException.Message);

        var missingSlotException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteSlotAsync(group.Id, 999));
        Assert.Equal("Availability slot with id 999 was not found.", missingSlotException.Message);
    }

    [Fact]
    public async Task ContainerService_UpdateDeleteAndSearchPaths_WorkForRootContainer()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new ContainerService(
            new DataAccessLayer.Repositories.ContainerRepository(context),
            new DataAccessLayer.Repositories.ScheduleRepository(context),
            new DataAccessLayer.Repositories.SchedulePresetRepository(context),
            new DataAccessLayer.Repositories.ScheduleSlotRepository(context),
            new DataAccessLayer.Repositories.ScheduleEmployeeRepository(context),
            new DataAccessLayer.Repositories.ScheduleCellStyleRepository(context),
            new DataAccessLayer.Repositories.AvailabilityGroupRepository(context),
            new DummyGenerator());

        var created = await service.CreateAsync(new ContainerModel
        {
            Name = " Main Container ",
            Note = " Note ",
        });

        created.Name = " Updated Container ";
        created.Note = " Updated note ";
        await service.UpdateAsync(created);

        var search = await service.GetByValueAsync("updated");
        Assert.Single(search);
        Assert.Equal("Updated Container", search[0].Name);
        Assert.Equal("Updated note", search[0].Note);

        await service.DeleteAsync(created.Id);
        Assert.Empty(await service.GetAllAsync());

        var invalidNameException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new ContainerModel { Name = " " }));
        Assert.Equal(["Container name is required."], invalidNameException.Errors[nameof(ContainerModel.Name)]);
    }

    [Fact]
    public async Task EmployeesAndShopsControllers_GetAllGetByIdAndUpdate_WorkWithRealFacades()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employeeController = new EmployeesController(
            TestEmployeeFacadeFactory.Create(new EmployeeService(new DataAccessLayer.Repositories.EmployeeRepository(context))));
        var shopController = new ShopsController(
            new ShopFacade(new ShopService(new DataAccessLayer.Repositories.ShopRepository(context))));
        SetHttpContext(employeeController);
        SetHttpContext(shopController);

        var employeeCreated = await employeeController.Create(new CreateEmployeeRequest
        {
            FirstName = "Alice",
            LastName = "Brown",
            Email = "alice@example.com",
        }, CancellationToken.None);
        var employeeDto = Assert.IsType<WebApi.Contracts.Employees.EmployeeDto>(Assert.IsType<CreatedAtActionResult>(employeeCreated.Result).Value);

        var shopCreated = await shopController.Create(new CreateShopRequest
        {
            Name = "Mega Shop",
            Address = "Main Street",
            Description = "Desc",
        }, CancellationToken.None);
        var shopDto = Assert.IsType<WebApi.Contracts.Shops.ShopDto>(Assert.IsType<CreatedAtActionResult>(shopCreated.Result).Value);

        var employeesResult = await employeeController.GetAll(CancellationToken.None);
        var employeeByIdResult = await employeeController.GetById(employeeDto.Id, CancellationToken.None);
        var employeeUpdateResult = await employeeController.Update(employeeDto.Id, new UpdateEmployeeRequest
        {
            FirstName = "Alicia",
            LastName = "Brown",
            Email = "alice@example.com",
        }, CancellationToken.None);

        var shopsResult = await shopController.GetAll(CancellationToken.None);
        var shopByIdResult = await shopController.GetById(shopDto.Id, CancellationToken.None);
        var shopUpdateResult = await shopController.Update(shopDto.Id, new UpdateShopRequest
        {
            Name = "Mega Shop Updated",
            Address = "Updated Street",
            Description = "Updated",
        }, CancellationToken.None);

        Assert.Single(Assert.IsAssignableFrom<IEnumerable<WebApi.Contracts.Employees.EmployeeDto>>(Assert.IsType<OkObjectResult>(employeesResult.Result).Value));
        Assert.Equal(employeeDto.Id, Assert.IsType<WebApi.Contracts.Employees.EmployeeDto>(Assert.IsType<OkObjectResult>(employeeByIdResult.Result).Value).Id);
        Assert.IsType<NoContentResult>(employeeUpdateResult);

        Assert.Single(Assert.IsAssignableFrom<IEnumerable<WebApi.Contracts.Shops.ShopDto>>(Assert.IsType<OkObjectResult>(shopsResult.Result).Value));
        Assert.Equal(shopDto.Id, Assert.IsType<WebApi.Contracts.Shops.ShopDto>(Assert.IsType<OkObjectResult>(shopByIdResult.Result).Value).Id);
        Assert.IsType<NoContentResult>(shopUpdateResult);
    }

    [Fact]
    public async Task AvailabilityBindsController_GetAllActiveGetByIdAndUpdate_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var manager = new DataAccessLayer.Models.ManagerAccountModel
        {
            Username = "availability.bind.manager",
            DisplayName = "Availability bind manager",
            PasswordHash = "test-hash",
            PasswordUpdatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        context.ManagerAccounts.Add(manager);
        await context.SaveChangesAsync();
        var controller = new AvailabilityBindsController(
            new BindService(new DataAccessLayer.Repositories.BindRepository(context)));
        SetManagerHttpContext(controller, manager.Id, "/api/availability-binds");

        var createdActive = await controller.Create(new CreateAvailabilityBindRequest
        {
            Key = "A",
            Value = "+",
            IsActive = true,
        }, CancellationToken.None);
        var activeDto = Assert.IsType<AvailabilityBindDto>(Assert.IsType<CreatedAtActionResult>(createdActive.Result).Value);

        await controller.Create(new CreateAvailabilityBindRequest
        {
            Key = "B",
            Value = "-",
            IsActive = false,
        }, CancellationToken.None);

        var allResult = await controller.GetAll(CancellationToken.None);
        var activeResult = await controller.GetActive(CancellationToken.None);
        var byIdResult = await controller.GetById(activeDto.Id, CancellationToken.None);
        var updateResult = await controller.Update(activeDto.Id, new UpdateAvailabilityBindRequest
        {
            Key = "A",
            Value = "08:00 - 12:00",
            IsActive = true,
        }, CancellationToken.None);

        var allBinds = Assert.IsAssignableFrom<IEnumerable<AvailabilityBindDto>>(Assert.IsType<OkObjectResult>(allResult.Result).Value);
        var activeBinds = Assert.IsAssignableFrom<IEnumerable<AvailabilityBindDto>>(Assert.IsType<OkObjectResult>(activeResult.Result).Value);
        var updatedBind = Assert.IsType<AvailabilityBindDto>(Assert.IsType<OkObjectResult>(byIdResult.Result).Value);

        Assert.Equal(2, allBinds.Count());
        Assert.Single(activeBinds);
        Assert.Equal(activeDto.Id, updatedBind.Id);
        Assert.IsType<NoContentResult>(updateResult);

        var refreshed = await controller.GetById(activeDto.Id, CancellationToken.None);
        var refreshedDto = Assert.IsType<AvailabilityBindDto>(Assert.IsType<OkObjectResult>(refreshed.Result).Value);
        Assert.Equal("08:00 - 12:00", refreshedDto.Value);
    }

    [Fact]
    public async Task AvailabilityGroupsController_ReturnsConflictForLockedGroupMutations()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateAvailabilityGroupService(context);
        var lockService = new ManagerEditLockService();
        var group = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel(name: "May Availability"));
        lockService.SetLocks(
            "manager-a",
            managerId: 1,
            "Alice Manager",
            [ManagerEditLockTargets.AvailabilityGroup(group.Id)]);
        var controller = new AvailabilityGroupsController(service, editLockService: lockService);
        SetManagerHttpContext(controller, managerId: 2, path: $"/api/availability-groups/{group.Id}");

        var updateResult = await controller.Update(group.Id, new UpdateAvailabilityGroupRequest
        {
            Name = "Blocked Update",
            Year = group.Year,
            Month = group.Month,
            PublicationStatus = "public",
        }, CancellationToken.None);
        var memberResult = await controller.CreateMember(group.Id, new CreateAvailabilityGroupMemberRequest
        {
            EmployeeId = 999,
            DisplayOrder = 0,
        }, CancellationToken.None);
        var slotResult = await controller.CreateSlot(group.Id, new CreateAvailabilitySlotRequest
        {
            AvailabilityGroupMemberId = 999,
            DayOfMonth = 1,
            Kind = AvailabilityKind.ANY,
        }, CancellationToken.None);

        var updateConflict = Assert.IsType<ConflictObjectResult>(updateResult);
        var memberConflict = Assert.IsType<ConflictObjectResult>(memberResult.Result);
        var slotConflict = Assert.IsType<ConflictObjectResult>(slotResult.Result);
        Assert.All([updateConflict, memberConflict, slotConflict], result =>
        {
            var problem = Assert.IsType<ProblemDetails>(result.Value);
            Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
            Assert.Equal("edit_lock_conflict", problem.Type);
            Assert.Equal("This availability group is currently being edited by Alice Manager.", problem.Detail);
        });
        Assert.Equal("May Availability", (await service.GetAsync(group.Id))!.Name);
        Assert.Empty(await service.GetMembersAsync(group.Id));
        Assert.Empty(await service.GetSlotsAsync(group.Id));
    }

    [Fact]
    public async Task EmployeesAndShopsControllers_ReturnConflictForLockedMutations()
    {
        var lockService = new ManagerEditLockService();
        lockService.SetLocks(
            "manager-a",
            managerId: 1,
            "Alice Manager",
            [ManagerEditLockTargets.Employee(7), ManagerEditLockTargets.Shop(4)]);
        var employeeFacade = new RecordingEmployeeFacade();
        var shopFacade = new RecordingShopFacade();
        var employeesController = new EmployeesController(employeeFacade, editLockService: lockService);
        var shopsController = new ShopsController(shopFacade, editLockService: lockService);
        SetManagerHttpContext(employeesController, managerId: 2, path: "/api/employees/7");
        SetManagerHttpContext(shopsController, managerId: 2, path: "/api/shops/4");

        var employeeUpdate = await employeesController.Update(7, new UpdateEmployeeRequest
        {
            FirstName = "Blocked",
            LastName = "Worker",
        }, CancellationToken.None);
        var employeeDelete = await employeesController.Delete(7, CancellationToken.None);
        var shopUpdate = await shopsController.Update(4, new UpdateShopRequest
        {
            Name = "Blocked Shop",
            Address = "Main Street",
        }, CancellationToken.None);
        var shopDelete = await shopsController.Delete(4, CancellationToken.None);

        AssertEditLockConflict(employeeUpdate, "This employee is currently being edited by Alice Manager.");
        AssertEditLockConflict(employeeDelete, "This employee is currently being edited by Alice Manager.");
        AssertEditLockConflict(shopUpdate, "This shop is currently being edited by Alice Manager.");
        AssertEditLockConflict(shopDelete, "This shop is currently being edited by Alice Manager.");
        Assert.Equal(0, employeeFacade.Calls);
        Assert.Equal(0, shopFacade.Calls);
    }

    [Fact]
    public async Task ContainersController_ReturnsConflictForLockedContainerAndGraphMutations()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var containerService = new ContainerService(
            new DataAccessLayer.Repositories.ContainerRepository(context),
            new DataAccessLayer.Repositories.ScheduleRepository(context),
            new DataAccessLayer.Repositories.SchedulePresetRepository(context),
            new DataAccessLayer.Repositories.ScheduleSlotRepository(context),
            new DataAccessLayer.Repositories.ScheduleEmployeeRepository(context),
            new DataAccessLayer.Repositories.ScheduleCellStyleRepository(context),
            new DataAccessLayer.Repositories.AvailabilityGroupRepository(context),
            new DummyGenerator());
        var lockService = new ManagerEditLockService();
        lockService.SetLocks(
            "manager-a",
            managerId: 1,
            "Alice Manager",
            [ManagerEditLockTargets.Container(2), ManagerEditLockTargets.Schedule(2, 66)]);
        var controller = new ContainersController(
            containerService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier(),
            lockService);
        SetManagerHttpContext(controller, managerId: 2, path: "/api/containers/2");

        var containerUpdate = await controller.Update(2, new UpdateContainerRequest
        {
            Name = "Blocked Container",
        }, CancellationToken.None);
        var containerDelete = await controller.Delete(2, CancellationToken.None);
        var graphUpdate = await controller.UpdateGraph(2, 66, new UpdateGraphRequest
        {
            ShopId = 3,
            Name = "Blocked Graph",
            Year = 2026,
            Month = 5,
            PeoplePerShift = 2,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 22:00",
            MaxHoursPerEmpMonth = 180,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 18,
        }, CancellationToken.None);
        var graphDelete = await controller.DeleteGraph(2, 66, CancellationToken.None);
        var graphGenerate = await controller.GenerateGraph(2, 66, new GenerateGraphRequest
        {
            DryRun = false,
            Overwrite = true,
        }, CancellationToken.None);
        var replaceSlots = await controller.ReplaceGraphSlots(2, 66, new ReplaceGraphSlotsRequest(), CancellationToken.None);

        AssertEditLockConflict(containerUpdate, "This container is currently being edited by Alice Manager.");
        AssertEditLockConflict(containerDelete, "This container is currently being edited by Alice Manager.");
        AssertEditLockConflict(graphUpdate, "This schedule is currently being edited by Alice Manager.");
        AssertEditLockConflict(graphDelete, "This schedule is currently being edited by Alice Manager.");
        AssertEditLockConflict(graphGenerate.Result!, "This schedule is currently being edited by Alice Manager.");
        AssertEditLockConflict(replaceSlots, "This schedule is currently being edited by Alice Manager.");
        Assert.Empty(await context.Containers.ToListAsync());
        Assert.Empty(await context.Schedules.ToListAsync());
    }

    private static AvailabilityGroupService CreateAvailabilityGroupService(DataAccessLayer.Models.DataBaseContext.AppDbContext context)
        => new(
            new DataAccessLayer.Repositories.AvailabilityGroupRepository(context),
            new DataAccessLayer.Repositories.AvailabilityGroupMemberRepository(context),
            new DataAccessLayer.Repositories.AvailabilityGroupDayRepository(context));

    private static void SetHttpContext(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
    }

    private static void SetManagerHttpContext(ControllerBase controller, int managerId, string path)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, $"manager-{managerId}"),
                    new Claim(ClaimTypes.Role, AuthRoles.Manager),
                    new Claim("manager_id", managerId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)),
                Request =
                {
                    Path = path,
                },
            },
        };
    }

    private static void AssertEditLockConflict(IActionResult result, string expectedDetail)
    {
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);

        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("edit_lock_conflict", problem.Type);
        Assert.Equal(expectedDetail, problem.Detail);
    }

    private sealed class RecordingEmployeeFacade : IEmployeeFacade
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<BusinessLogicLayer.Contracts.Employees.EmployeeDto>> GetAllAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<BusinessLogicLayer.Contracts.Employees.EmployeeDto>>([]);
        }

        public Task<IReadOnlyList<BusinessLogicLayer.Contracts.Employees.EmployeeDto>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<BusinessLogicLayer.Contracts.Employees.EmployeeDto>>([]);
        }

        public Task<BusinessLogicLayer.Contracts.Employees.EmployeeDto?> GetAsync(int id, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<BusinessLogicLayer.Contracts.Employees.EmployeeDto?>(new BusinessLogicLayer.Contracts.Employees.EmployeeDto { Id = id });
        }

        public Task<BusinessLogicLayer.Contracts.Employees.EmployeeDto> CreateAsync(BusinessLogicLayer.Contracts.Employees.SaveEmployeeRequest request, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new BusinessLogicLayer.Contracts.Employees.EmployeeDto { Id = 1 });
        }

        public Task UpdateAsync(BusinessLogicLayer.Contracts.Employees.SaveEmployeeRequest request, CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(DeleteOperationResult.Success());
        }
    }

    private sealed class RecordingShopFacade : IShopFacade
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>> GetAllAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>>([]);
        }

        public Task<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<BusinessLogicLayer.Contracts.Shops.ShopDto>>([]);
        }

        public Task<BusinessLogicLayer.Contracts.Shops.ShopDto?> GetAsync(int id, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<BusinessLogicLayer.Contracts.Shops.ShopDto?>(new BusinessLogicLayer.Contracts.Shops.ShopDto { Id = id });
        }

        public Task<BusinessLogicLayer.Contracts.Shops.ShopDto> CreateAsync(BusinessLogicLayer.Contracts.Shops.SaveShopRequest request, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new BusinessLogicLayer.Contracts.Shops.ShopDto { Id = 1 });
        }

        public Task UpdateAsync(BusinessLogicLayer.Contracts.Shops.SaveShopRequest request, CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(DeleteOperationResult.Success());
        }
    }

    private sealed class DummyGenerator : IScheduleGenerator
    {
        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress = null,
            CancellationToken ct = default)
            => Task.FromResult<IList<ScheduleSlotModel>>([]);
    }
}
