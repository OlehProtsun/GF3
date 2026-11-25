using System.Security.Claims;
using BusinessLogicLayer;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.EmployeeSchedules;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class EmployeePublicSchedulesTests
{
    [Fact]
    public async Task PublicListAndDetailsOnlyExposeRelatedPublishedSchedulesInOrder()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await Seed(db);
        using var services = new ServiceCollection().AddLogging().AddSingleton(TimeProvider.System)
            .AddBusinessLogicStack(database.ConnectionString).BuildServiceProvider();
        using var scope = services.CreateScope();
        var controller = new EmployeeSchedulesController(scope.ServiceProvider.GetRequiredService<IContainerService>())
        {
            ControllerContext = Context(fixture.Owner),
        };
        var list = await controller.GetPublicSchedules(db, default);
        var summaries = Assert.IsAssignableFrom<IReadOnlyList<PublicScheduleSummaryDto>>(Assert.IsType<OkObjectResult>(list.Result).Value);
        Assert.Equal(new[] { fixture.Preview, fixture.OtherPreview }, summaries.Select(row => row.Id));
        Assert.All(summaries, x => Assert.Equal((2026, 8, "public"), (x.Year, x.Month, x.PublicationStatus)));
        Assert.Equal(new[] { "Alpha", "Zulu" }, summaries.Select(row => row.Name));
        var preview = Assert.IsType<EmployeeScheduleDto>(Assert.IsType<OkObjectResult>(
            (await controller.GetPublicSchedule(fixture.Preview, db, default)).Result).Value);
        Assert.False(preview.AllowSwap);
        Assert.Equal(fixture.Container, preview.ContainerId);
        Assert.Equal("Colleague", Assert.Single(preview.Employees).FirstName);
        Assert.Equal((7, "09:00", "13:00"), (Assert.Single(preview.Slots).DayOfMonth, preview.Slots[0].FromTime, preview.Slots[0].ToTime));
        var related = Assert.Single(preview.RelatedScheduleAssignments);
        Assert.Equal(fixture.OtherPreview, related.ScheduleId);
        Assert.Equal(8, related.DayOfMonth);
        foreach (var id in fixture.Hidden)
            Assert.IsType<NotFoundResult>((await controller.GetPublicSchedule(id, db, default)).Result);

        var owner = await db.Employees.FindAsync(fixture.Owner);
        owner!.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<PublicScheduleSummaryDto>>(
            Assert.IsType<OkObjectResult>((await controller.GetPublicSchedules(db, default)).Result).Value));
        Assert.IsType<NotFoundResult>((await controller.GetPublicSchedule(fixture.Preview, db, default)).Result);
        controller.ControllerContext = Context(null);
        await Assert.ThrowsAsync<BadHttpRequestException>(() => controller.GetPublicSchedules(db, default));
    }

    [Fact]
    public void PreviewRoutesAreEmployeeOnlyReadOnlyAndNoStore()
    {
        var type = typeof(EmployeeSchedulesController);
        Assert.Equal(AuthRoles.Employee, Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);
        foreach (var name in new[] { nameof(EmployeeSchedulesController.GetPublicSchedule), nameof(EmployeeSchedulesController.GetPublicSchedules) })
        {
            var method = type.GetMethod(name)!;
            Assert.Single(method.GetCustomAttributes(typeof(HttpGetAttribute), true));
            Assert.Empty(method.GetCustomAttributes(typeof(HttpPostAttribute), true));
            var cache = Assert.Single(method.GetCustomAttributes(typeof(ResponseCacheAttribute), true).Cast<ResponseCacheAttribute>());
            Assert.True(cache.NoStore);
            Assert.Equal(ResponseCacheLocation.None, cache.Location);
        }
    }

    internal static ControllerContext Context(int? employee) => new()
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            employee is int id ? [new Claim("employee_id", id.ToString())] : [], "test")) },
    };

    internal static async Task<(int Owner, int Container, int Preview, int OtherPreview, int[] Hidden)> Seed(AppDbContext db)
    {
        var owner = TestDataFactory.CreateDalEmployee("Owner", "Fixture");
        var colleague = TestDataFactory.CreateDalEmployee("Colleague", "Fixture");
        var container = TestDataFactory.CreateDalContainer("Shared container");
        var unrelated = TestDataFactory.CreateDalContainer("Unrelated container");
        var shop = TestDataFactory.CreateDalShop();
        db.AddRange(owner, colleague, container, unrelated, shop);
        await db.SaveChangesAsync();
        var own = Graph("Own", container.Id, owner.Id);
        var zulu = Graph("Zulu", container.Id, colleague.Id);
        var alpha = Graph("Alpha", container.Id, colleague.Id);
        var privateGraph = Graph("Private", container.Id, colleague.Id);
        privateGraph.PublicationStatus = SchedulePublicationStatus.Private;
        var wrongMonth = Graph("July", container.Id, colleague.Id);
        wrongMonth.Month = 7;
        var wrongContainer = Graph("Other container", unrelated.Id, colleague.Id);
        db.AddRange(own, zulu, alpha, privateGraph, wrongMonth, wrongContainer);
        await db.SaveChangesAsync();
        db.AddRange(TestDataFactory.CreateDalSlot(own.Id, 6, 1, owner.Id, "08:00", "14:00"),
            TestDataFactory.CreateDalSlot(alpha.Id, 7, 1, colleague.Id, "09:00", "13:00"),
            TestDataFactory.CreateDalSlot(zulu.Id, 8, 1, colleague.Id, "14:00", "18:00"));
        await db.SaveChangesAsync();
        return (owner.Id, container.Id, alpha.Id, zulu.Id, [own.Id, privateGraph.Id, wrongMonth.Id, wrongContainer.Id, int.MaxValue]);

        ScheduleModel Graph(string name, int containerId, int employeeId)
        {
            var graph = TestDataFactory.CreateDalSchedule(containerId, shop.Id, name: name, year: 2026, month: 8);
            graph.PublicationStatus = SchedulePublicationStatus.Public;
            graph.AllowSwap = true;
            graph.Employees.Add(new ScheduleEmployeeModel { EmployeeId = employeeId });
            return graph;
        }
    }
}
