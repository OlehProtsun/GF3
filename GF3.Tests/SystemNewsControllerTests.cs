using System.Security.Claims;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class SystemNewsControllerTests
{
    [Fact]
    public async Task News_IsFilteredByAudience_AndReadStateIsPerAccount()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int managerId;
        int employeeId;
        int allId;

        await using (var seed = database.CreateContext())
        {
            var now = DateTimeOffset.UtcNow;
            var manager = new ManagerAccountModel
            {
                Username = "news.manager", DisplayName = "News Manager", PasswordHash = "hash",
                PasswordUpdatedAtUtc = now, CreatedAtUtc = now, UpdatedAtUtc = now,
            };
            var employee = TestDataFactory.CreateDalEmployee("News", "Employee");
            seed.AddRange(manager, employee);
            await seed.SaveChangesAsync();
            managerId = manager.Id;
            employeeId = employee.Id;
        }

        await using (var write = database.CreateContext())
        {
            var admin = new AdminSystemNewsController(write);
            allId = (await Create(admin, "For everyone", "all")).Id;
            await Create(admin, "For managers", "managers");
            await Create(admin, "For employees", "employees");
        }

        await using (var managerContext = database.CreateContext())
        {
            var manager = CreateReader(managerContext, AuthRoles.Manager, managerId);
            var result = await manager.GetVisible(CancellationToken.None);
            var items = Assert.IsAssignableFrom<IReadOnlyList<SystemNewsDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal(2, items.Count);
            Assert.DoesNotContain(items, item => item.Audience == "employees");
            Assert.IsType<NoContentResult>(await manager.MarkRead(allId, CancellationToken.None));
        }

        await using (var managerVerification = database.CreateContext())
        {
            var items = GetItems(await CreateReader(managerVerification, AuthRoles.Manager, managerId).GetVisible(CancellationToken.None));
            Assert.True(items.Single(item => item.Id == allId).IsRead);
        }

        await using (var employeeContext = database.CreateContext())
        {
            var items = GetItems(await CreateReader(employeeContext, AuthRoles.Employee, employeeId).GetVisible(CancellationToken.None));
            Assert.Equal(2, items.Count);
            Assert.False(items.Single(item => item.Id == allId).IsRead);
            Assert.DoesNotContain(items, item => item.Audience == "managers");
        }
    }

    private static async Task<SystemNewsDto> Create(AdminSystemNewsController controller, string title, string audience)
    {
        var result = await controller.Create(new SaveSystemNewsRequest
        {
            Title = title,
            Body = "A useful system update.",
            Audience = audience,
        }, CancellationToken.None);
        return Assert.IsType<SystemNewsDto>(Assert.IsType<ObjectResult>(result.Result).Value);
    }

    private static IReadOnlyList<SystemNewsDto> GetItems(ActionResult<IReadOnlyList<SystemNewsDto>> result)
        => Assert.IsAssignableFrom<IReadOnlyList<SystemNewsDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);

    private static SystemNewsController CreateReader(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        string role,
        int accountId)
    {
        var claimName = role == AuthRoles.Manager ? "manager_id" : "employee_id";
        return new SystemNewsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, $"{role}-{accountId}"),
                        new Claim(ClaimTypes.Role, role),
                        new Claim(claimName, accountId.ToString()),
                    ], JwtAuthenticationDefaults.SchemeName)),
                },
            },
        };
    }
}
