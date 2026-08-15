using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class ManagerGraphFillColorBindsControllerTests
{
    [Fact]
    public async Task Binds_AreUpsertedAndIsolatedPerManager()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int firstManagerId;
        int secondManagerId;

        await using (var seedContext = database.CreateContext())
        {
            var now = DateTimeOffset.UtcNow;
            var first = CreateManager("first.manager", now);
            var second = CreateManager("second.manager", now);
            seedContext.ManagerAccounts.AddRange(first, second);
            await seedContext.SaveChangesAsync();
            firstManagerId = first.Id;
            secondManagerId = second.Id;
        }

        await using (var firstContext = database.CreateContext())
        {
            var controller = CreateController(firstContext, firstManagerId);
            var firstBind = await Upsert(controller, "F4", "#dbeafe");
            Assert.Equal("#DBEAFE", firstBind.FillColor);

            var rebound = await Upsert(controller, "F5", "#DBEAFE");
            Assert.Equal(firstBind.Id, rebound.Id);
            Assert.Equal("F5", rebound.Key);

            var valueBindService = new BindService(new BindRepository(firstContext));
            await Assert.ThrowsAsync<ValidationException>(() => valueBindService.CreateAsync(
                new BusinessLogicLayer.Contracts.Models.BindModel
                {
                    Key = "F5",
                    Value = "+",
                    IsActive = true,
                },
                firstManagerId,
                CancellationToken.None));

            var valueBind = await valueBindService.CreateAsync(
                new BusinessLogicLayer.Contracts.Models.BindModel
                {
                    Key = "F6",
                    Value = "-",
                    IsActive = true,
                },
                firstManagerId,
                CancellationToken.None);
            Assert.Equal(firstManagerId, valueBind.ManagerAccountId);

            var conflictingColorResult = await controller.Upsert(new SaveManagerGraphFillColorBindRequest
            {
                Key = "F6",
                FillColor = "#BAE6FD",
            }, CancellationToken.None);
            var validationResult = Assert.IsType<ObjectResult>(conflictingColorResult.Result);
            var validationProblem = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
            Assert.Contains(nameof(SaveManagerGraphFillColorBindRequest.Key), validationProblem.Errors.Keys);
        }

        await using (var secondContext = database.CreateContext())
        {
            var controller = CreateController(secondContext, secondManagerId);
            var secondBind = await Upsert(controller, "F5", "#FECDD3");
            Assert.Equal("#FECDD3", secondBind.FillColor);

            var valueBind = await new BindService(new BindRepository(secondContext)).CreateAsync(
                new BusinessLogicLayer.Contracts.Models.BindModel
                {
                    Key = "F6",
                    Value = "+",
                    IsActive = true,
                },
                secondManagerId,
                CancellationToken.None);
            Assert.Equal(secondManagerId, valueBind.ManagerAccountId);
        }

        await using (var verificationContext = database.CreateContext())
        {
            var firstResult = await CreateController(verificationContext, firstManagerId).GetCurrent(CancellationToken.None);
            var firstBinds = Assert.IsAssignableFrom<IReadOnlyList<ManagerGraphFillColorBindDto>>(
                Assert.IsType<OkObjectResult>(firstResult.Result).Value);
            var firstBind = Assert.Single(firstBinds);
            Assert.Equal("F5", firstBind.Key);
            Assert.Equal("#DBEAFE", firstBind.FillColor);

            var secondResult = await CreateController(verificationContext, secondManagerId).GetCurrent(CancellationToken.None);
            var secondBinds = Assert.IsAssignableFrom<IReadOnlyList<ManagerGraphFillColorBindDto>>(
                Assert.IsType<OkObjectResult>(secondResult.Result).Value);
            Assert.Equal("#FECDD3", Assert.Single(secondBinds).FillColor);
        }
    }

    private static async Task<ManagerGraphFillColorBindDto> Upsert(
        ManagerGraphFillColorBindsController controller,
        string key,
        string fillColor)
    {
        var result = await controller.Upsert(new SaveManagerGraphFillColorBindRequest
        {
            Key = key,
            FillColor = fillColor,
        }, CancellationToken.None);
        return Assert.IsType<ManagerGraphFillColorBindDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    private static ManagerAccountModel CreateManager(string username, DateTimeOffset now) => new()
    {
        Username = username,
        DisplayName = username,
        PasswordHash = "test-hash",
        PasswordUpdatedAtUtc = now,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static ManagerGraphFillColorBindsController CreateController(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        int managerId)
    {
        var controller = new ManagerGraphFillColorBindsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, $"manager-{managerId}"),
                    new Claim(ClaimTypes.Role, AuthRoles.Manager),
                    new Claim("manager_id", managerId.ToString()),
                ],
                JwtAuthenticationDefaults.SchemeName)),
            },
        };
        return controller;
    }
}
