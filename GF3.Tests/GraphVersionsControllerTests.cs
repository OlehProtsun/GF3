using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class GraphVersionsControllerTests
{
    [Fact]
    public async Task CommitCheckoutAndDelete_PreserveTreeAndCreateBranchFromOldCommit()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedScheduleAsync(db);
        var controller = CreateController(db);

        await controller.GetTree(fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);
        var initial = Assert.Single(await db.ScheduleVersions.AsNoTracking().ToListAsync());
        Assert.Equal("Production import", initial.CreatedByManagerName);

        await controller.Commit(fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);
        Assert.Single(await db.ScheduleVersions.AsNoTracking().ToListAsync());

        var schedule = await db.Schedules.SingleAsync(item => item.Id == fixture.ScheduleId);
        schedule.Name = "Second snapshot";
        await db.SaveChangesAsync();
        await controller.Commit(fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);

        var mainTip = await db.ScheduleVersions.AsNoTracking().SingleAsync(version => version.VersionNumber == 2);
        Assert.Equal("main", mainTip.BranchName);
        Assert.Equal(initial.Id, mainTip.ParentVersionId);

        await controller.Checkout(fixture.ContainerId, fixture.ScheduleId, initial.Id, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal("Initial snapshot", (await db.Schedules.AsNoTracking().SingleAsync(item => item.Id == fixture.ScheduleId)).Name);

        schedule = await db.Schedules.SingleAsync(item => item.Id == fixture.ScheduleId);
        schedule.Name = "Branched snapshot";
        await db.SaveChangesAsync();
        await controller.Commit(fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);

        var branchTip = await db.ScheduleVersions.AsNoTracking().SingleAsync(version => version.VersionNumber == 3);
        Assert.Equal("branch-2", branchTip.BranchName);
        Assert.Equal(initial.Id, branchTip.ParentVersionId);
        Assert.Equal("Ada Manager", branchTip.CreatedByManagerName);

        var deleteResult = await controller.Delete(fixture.ContainerId, fixture.ScheduleId, mainTip.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);
        Assert.DoesNotContain(await db.ScheduleVersions.AsNoTracking().ToListAsync(), version => version.Id == mainTip.Id);

        var currentDelete = await controller.Delete(fixture.ContainerId, fixture.ScheduleId, branchTip.Id, CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(currentDelete);
    }

    [Fact]
    public async Task Checkout_WithOpenSwap_IsRejectedWithoutChangingSchedule()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedScheduleAsync(db);
        var controller = CreateController(db);

        await controller.GetTree(fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);
        var initial = await db.ScheduleVersions.AsNoTracking().SingleAsync();
        var schedule = await db.Schedules.SingleAsync(item => item.Id == fixture.ScheduleId);
        schedule.Name = "Current name";
        await db.SaveChangesAsync();
        await controller.Commit(fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);

        db.ShiftSwapRequests.Add(new ShiftSwapRequestModel
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.SlotId,
            FromEmployeeId = fixture.EmployeeId,
            IsManagerCreated = false,
            Visibility = ShiftSwapVisibility.Public,
            Status = ShiftSwapStatus.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await controller.Checkout(fixture.ContainerId, fixture.ScheduleId, initial.Id, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        db.ChangeTracker.Clear();
        Assert.Equal("Current name", (await db.Schedules.AsNoTracking().SingleAsync(item => item.Id == fixture.ScheduleId)).Name);
    }

    private static GraphVersionsController CreateController(DataAccessLayer.Models.DataBaseContext.AppDbContext db)
    {
        var controller = new GraphVersionsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "ada"),
                    new Claim(ClaimTypes.Role, AuthRoles.Manager),
                    new Claim("manager_id", "7"),
                    new Claim("display_name", "Ada Manager"),
                ],
                JwtAuthenticationDefaults.SchemeName)),
            },
        };
        return controller;
    }

    private static async Task<ScheduleFixture> SeedScheduleAsync(DataAccessLayer.Models.DataBaseContext.AppDbContext db)
    {
        var container = new ContainerModel { Name = $"Versions {Guid.NewGuid():N}" };
        var shop = new ShopModel { Name = $"Shop {Guid.NewGuid():N}", Address = "Main" };
        var employee = new EmployeeModel { FirstName = "Version", LastName = "Employee" };
        db.AddRange(container, shop, employee);
        await db.SaveChangesAsync();

        var schedule = new ScheduleModel
        {
            ContainerId = container.Id,
            ShopId = shop.Id,
            Name = "Initial snapshot",
            Year = 2026,
            Month = 8,
            PublicationStatus = SchedulePublicationStatus.Private,
            AllowSwap = true,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 22:00",
            MaxHoursPerEmpMonth = 180,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 12,
            Note = "Version note",
        };
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();

        db.ScheduleEmployees.Add(new ScheduleEmployeeModel
        {
            ScheduleId = schedule.Id,
            EmployeeId = employee.Id,
            MinHoursMonth = 80,
            DisplayOrder = 0,
        });
        var slot = new ScheduleSlotModel
        {
            ScheduleId = schedule.Id,
            DayOfMonth = 1,
            SlotNo = 1,
            FromTime = "08:00",
            ToTime = "16:00",
            EmployeeId = employee.Id,
            Status = SlotStatus.ASSIGNED,
        };
        db.ScheduleSlots.Add(slot);
        db.ScheduleCellStyles.Add(new ScheduleCellStyleModel
        {
            ScheduleId = schedule.Id,
            DayOfMonth = 1,
            EmployeeId = employee.Id,
            BackgroundColorArgb = 10,
        });
        await db.SaveChangesAsync();
        return new ScheduleFixture(container.Id, schedule.Id, employee.Id, slot.Id);
    }

    private sealed record ScheduleFixture(int ContainerId, int ScheduleId, int EmployeeId, int SlotId);
}
