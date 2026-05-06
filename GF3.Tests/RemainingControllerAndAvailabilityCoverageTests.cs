using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;
using WebApi.Contracts.AvailabilityBinds;
using WebApi.Contracts.Employees;
using WebApi.Contracts.Shops;

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
        var controller = new AvailabilityBindsController(
            new BindService(new DataAccessLayer.Repositories.BindRepository(context)));
        SetHttpContext(controller);

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
