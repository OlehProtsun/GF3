using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class EmployeeUiStateControllerTests
{
    [Fact]
    public async Task State_PersistsAcrossControllerAndDbContextInstances()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int employeeId;
        int scheduleId;
        int swapId;

        await using (var seedContext = database.CreateContext())
        {
            var employee = TestDataFactory.CreateDalEmployee("Sync", "Worker");
            var container = TestDataFactory.CreateDalContainer("Sync Container");
            var shop = TestDataFactory.CreateDalShop("Sync Shop");
            seedContext.AddRange(employee, container, shop);
            await seedContext.SaveChangesAsync();

            var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Published Sync");
            schedule.PublicationStatus = SchedulePublicationStatus.Public;
            seedContext.Schedules.Add(schedule);
            await seedContext.SaveChangesAsync();
            seedContext.ScheduleEmployees.Add(new ScheduleEmployeeModel
            {
                EmployeeId = employee.Id,
                ScheduleId = schedule.Id,
                DisplayOrder = 0,
                MinHoursMonth = 80,
            });
            await seedContext.SaveChangesAsync();
            var slot = TestDataFactory.CreateDalSlot(schedule.Id, 1, 1, employee.Id, "08:00", "16:00");
            seedContext.ScheduleSlots.Add(slot);
            await seedContext.SaveChangesAsync();
            var swap = new ShiftSwapRequestModel
            {
                ScheduleId = schedule.Id,
                ScheduleSlotId = slot.Id,
                FromEmployeeId = employee.Id,
                Visibility = ShiftSwapVisibility.Public,
                Status = ShiftSwapStatus.Open,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            seedContext.ShiftSwapRequests.Add(swap);
            await seedContext.SaveChangesAsync();
            employeeId = employee.Id;
            scheduleId = schedule.Id;
            swapId = swap.Id;
        }

        await using (var writeContext = database.CreateContext())
        {
            var controller = CreateController(writeContext, employeeId);
            Assert.IsType<NoContentResult>(await controller.SaveScheduleColumnOrder(
                scheduleId,
                new SaveEmployeeScheduleColumnOrderRequest { ColumnOrder = [employeeId, 99, employeeId] },
                CancellationToken.None));
            Assert.IsType<NoContentResult>(await controller.MarkNotificationsRead(
                new MarkEmployeeNotificationsReadRequest
                {
                    NotificationIds = ["schedule-public:12", "open-shift:7", "open-shift:7"],
                },
                CancellationToken.None));
            Assert.IsType<NoContentResult>(await controller.PinSwap(swapId, CancellationToken.None));
        }

        await using (var readContext = database.CreateContext())
        {
            var controller = CreateController(readContext, employeeId);
            var result = await controller.GetCurrent(CancellationToken.None);
            var dto = Assert.IsType<EmployeeUiStateDto>(Assert.IsType<OkObjectResult>(result.Result).Value);

            Assert.Equal([employeeId, 99], dto.ScheduleColumnOrders[scheduleId]);
            Assert.Equal(2, dto.ReadNotificationIds.Count);
            Assert.Contains("schedule-public:12", dto.ReadNotificationIds);
            Assert.Contains("open-shift:7", dto.ReadNotificationIds);
            Assert.Equal([swapId], dto.PinnedSwapIds);
        }

        await using (var deleteContext = database.CreateContext())
        {
            var controller = CreateController(deleteContext, employeeId);
            Assert.IsType<NoContentResult>(await controller.UnpinSwap(swapId, CancellationToken.None));
        }

        await using (var verificationContext = database.CreateContext())
        {
            Assert.Empty(verificationContext.EmployeePinnedSwaps);
        }
    }

    [Fact]
    public async Task AvailabilityPresets_PersistAcrossDevices_StayAccountScoped_AndImportNeverOverwritesServer()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int employeeId, colleagueId;
        await using (var seed = database.CreateContext())
        {
            var employee = TestDataFactory.CreateDalEmployee("Presets", "Owner");
            var colleague = TestDataFactory.CreateDalEmployee("Other", "Owner");
            seed.Employees.AddRange(employee, colleague);
            await seed.SaveChangesAsync();
            employeeId = employee.Id; colleagueId = colleague.Id;
            seed.EmployeeAccounts.AddRange(
                new EmployeeAccountModel { EmployeeId = employeeId, Username = "preset-owner", PasswordHash = "test", PasswordUpdatedAtUtc = DateTimeOffset.UtcNow },
                new EmployeeAccountModel { EmployeeId = colleagueId, Username = "other-owner", PasswordHash = "test", PasswordUpdatedAtUtc = DateTimeOffset.UtcNow });
            await seed.SaveChangesAsync();
        }
        string[] presets = ["08:30 - 16:30", "15:00 - 21:00", "09:00 - 21:00", "10:00 - 18:00"];
        await using (var firstDevice = database.CreateContext())
        {
            var controller = CreateController(firstDevice, employeeId);
            var initial = await controller.GetAvailabilityTimePresets(CancellationToken.None);
            Assert.Null(Assert.IsType<EmployeeAvailabilityTimePresetsDto>(Assert.IsType<OkObjectResult>(initial.Result).Value).Presets);
            var saved = await controller.SaveAvailabilityTimePresets(new SaveEmployeeAvailabilityTimePresetsRequest { Presets = presets }, CancellationToken.None);
            Assert.Equal(presets, Assert.IsType<EmployeeAvailabilityTimePresetsDto>(Assert.IsType<OkObjectResult>(saved.Result).Value).Presets);
        }
        await using (var secondDevice = database.CreateContext())
        {
            var controller = CreateController(secondDevice, employeeId);
            var restored = await controller.GetAvailabilityTimePresets(CancellationToken.None);
            Assert.Equal(presets, Assert.IsType<EmployeeAvailabilityTimePresetsDto>(Assert.IsType<OkObjectResult>(restored.Result).Value).Presets);
            var imported = await controller.SaveAvailabilityTimePresets(new SaveEmployeeAvailabilityTimePresetsRequest { Presets = ["09:00 - 15:00", "15:00 - 21:00", "09:00 - 21:00"], OnlyIfUnset = true }, CancellationToken.None);
            Assert.Equal(presets, Assert.IsType<EmployeeAvailabilityTimePresetsDto>(Assert.IsType<OkObjectResult>(imported.Result).Value).Presets);
            var other = await CreateController(secondDevice, colleagueId).GetAvailabilityTimePresets(CancellationToken.None);
            Assert.Null(Assert.IsType<EmployeeAvailabilityTimePresetsDto>(Assert.IsType<OkObjectResult>(other.Result).Value).Presets);
            var firstImport = await CreateController(secondDevice, colleagueId).SaveAvailabilityTimePresets(new SaveEmployeeAvailabilityTimePresetsRequest { Presets = presets, OnlyIfUnset = true }, CancellationToken.None);
            Assert.Equal(presets, Assert.IsType<EmployeeAvailabilityTimePresetsDto>(Assert.IsType<OkObjectResult>(firstImport.Result).Value).Presets);
        }
        await using (var verification = database.CreateContext())
        {
            var controller = CreateController(verification, employeeId);
            foreach (var invalid in new string[]?[] { null, [], ["09:00 - 15:00"], ["21:00 - 09:00", "15:00 - 21:00", "09:00 - 21:00"], ["day off", "15:00 - 21:00", "09:00 - 21:00"], ["25:00 - 26:00", "15:00 - 21:00", "09:00 - 21:00"], Enumerable.Repeat("09:00 - 15:00", 101).ToArray() })
            {
                var rejected = await controller.SaveAvailabilityTimePresets(new SaveEmployeeAvailabilityTimePresetsRequest { Presets = invalid }, CancellationToken.None);
                var problem = Assert.IsType<ValidationProblemDetails>(Assert.IsType<BadRequestObjectResult>(rejected.Result).Value);
                Assert.Equal(400, problem.Status);
                Assert.NotEmpty(problem.Errors);
            }
            var unchanged = await controller.GetAvailabilityTimePresets(CancellationToken.None);
            Assert.Equal(presets, Assert.IsType<EmployeeAvailabilityTimePresetsDto>(Assert.IsType<OkObjectResult>(unchanged.Result).Value).Presets);
            Assert.IsType<UnauthorizedResult>((await CreateController(verification, 999999).GetAvailabilityTimePresets(CancellationToken.None)).Result);
            Assert.Equal(["OnlyIfUnset", "Presets"], typeof(SaveEmployeeAvailabilityTimePresetsRequest).GetProperties().Select(property => property.Name).Order().ToArray());
            Assert.Equal(AuthRoles.Employee, Assert.Single(typeof(EmployeeUiStateController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()).Roles);
        }
    }

    private static EmployeeUiStateController CreateController(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        int employeeId)
    {
        var controller = new EmployeeUiStateController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "sync-worker"),
                    new Claim(ClaimTypes.Role, AuthRoles.Employee),
                    new Claim("employee_id", employeeId.ToString()),
                ],
                JwtAuthenticationDefaults.SchemeName)),
            },
        };
        return controller;
    }
}
