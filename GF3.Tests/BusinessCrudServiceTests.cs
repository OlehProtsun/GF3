using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class BusinessCrudServiceTests
{
    [Fact]
    public async Task EmployeeService_Create_NormalizesNames_AndRejectsDuplicates()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new EmployeeService(new DataAccessLayer.Repositories.EmployeeRepository(context));

        var created = await service.CreateAsync(TestDataFactory.CreateEmployeeModel(firstName: " Alice ", lastName: " Brown "));

        Assert.Equal("Alice", created.FirstName);
        Assert.Equal("Brown", created.LastName);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(TestDataFactory.CreateEmployeeModel(firstName: "alice", lastName: "brown", email: "other@example.com")));

        Assert.Equal("An employee with the same first and last name already exists.", exception.Message);
    }

    [Fact]
    public async Task EmployeeService_Delete_GuardsAgainstReferencedEmployee()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(employee, container, shop);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(schedule.Id, 1, 1, employee.Id, "08:00", "12:00"));
        await context.SaveChangesAsync();

        var service = new EmployeeService(new DataAccessLayer.Repositories.EmployeeRepository(context));
        var result = await service.TryDeleteAsync(employee.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "To delete this employee, first delete all Availability and Schedule entries where this employee is used.",
            result.Message);

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(employee.Id));
    }

    [Fact]
    public async Task ShopService_Create_NormalizesFields_AndDeleteGuardWorks()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer();
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var service = new ShopService(new DataAccessLayer.Repositories.ShopRepository(context));
        var created = await service.CreateAsync(TestDataFactory.CreateShopModel(name: " Mega Shop ", address: " Main St ", description: " "));

        Assert.Equal("Mega Shop", created.Name);
        Assert.Equal("Main St", created.Address);
        Assert.Null(created.Description);

        context.Schedules.Add(TestDataFactory.CreateDalSchedule(container.Id, created.Id));
        await context.SaveChangesAsync();

        var result = await service.TryDeleteAsync(created.Id);
        Assert.False(result.Succeeded);

        var duplicateException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(TestDataFactory.CreateShopModel(name: "mega shop", address: "Another")));
        Assert.Equal("A shop with the same name already exists.", duplicateException.Message);
    }

    [Fact]
    public async Task BindService_ValidatesAndUpsertsByKey()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new BindService(new DataAccessLayer.Repositories.BindRepository(context));

        var created = await service.CreateAsync(TestDataFactory.CreateBindModel(key: " A ", value: " + "));
        var upserted = await service.UpsertByKeyAsync(TestDataFactory.CreateBindModel(key: "A", value: "08:00 - 12:00"));

        Assert.Equal("A", created.Key);
        Assert.Equal("+", created.Value);
        Assert.Equal(created.Id, upserted.Id);
        Assert.Equal("08:00 - 12:00", upserted.Value);

        var requiredException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(TestDataFactory.CreateBindModel(key: "", value: "")));
        Assert.Equal(["Bind key is required."], requiredException.Errors["Key"]);

        await service.CreateAsync(TestDataFactory.CreateBindModel(key: "B", value: "-"));
        var duplicateException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(TestDataFactory.CreateBindModel(id: created.Id, key: "B", value: "+")));
        Assert.Equal(["A bind with key 'B' already exists."], duplicateException.Errors["Key"]);
    }

    [Fact]
    public async Task ScheduleEmployeeAndSlotServices_PerformCrudOperations()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee();
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(employee, container, shop);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        var employeeService = new ScheduleEmployeeService(new DataAccessLayer.Repositories.ScheduleEmployeeRepository(context));
        var slotService = new ScheduleSlotService(new DataAccessLayer.Repositories.ScheduleSlotRepository(context));

        var graphEmployee = await employeeService.CreateAsync(TestDataFactory.CreateScheduleEmployeeModel(employee.Id, schedule.Id, displayOrder: 2));
        var graphSlot = await slotService.CreateAsync(TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "08:00", "12:00", schedule.Id));

        Assert.Single(await employeeService.GetAllAsync());
        Assert.Single(await slotService.GetAllAsync());

        graphEmployee.DisplayOrder = 5;
        await employeeService.UpdateAsync(graphEmployee);
        graphSlot.ToTime = "13:00";
        await slotService.UpdateAsync(graphSlot);

        var updatedEmployee = await employeeService.GetAsync(graphEmployee.Id);
        var updatedSlot = await slotService.GetAsync(graphSlot.Id);
        Assert.NotNull(updatedEmployee);
        Assert.Equal(5, updatedEmployee!.DisplayOrder);
        Assert.NotNull(updatedSlot);
        Assert.Equal("13:00", updatedSlot!.ToTime);

        await slotService.DeleteAsync(graphSlot.Id);
        await employeeService.DeleteAsync(graphEmployee.Id);

        Assert.Empty(await employeeService.GetAllAsync());
        Assert.Empty(await slotService.GetAllAsync());
    }
}
