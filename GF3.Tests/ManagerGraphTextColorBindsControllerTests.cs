using System.Security.Claims;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class ManagerGraphTextColorBindsControllerTests
{
    [Fact]
    public async Task TextColorBinds_AreIsolatedAndCannotReuseValueOrFillKeys()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int firstManagerId;
        int secondManagerId;

        await using (var seedContext = database.CreateContext())
        {
            var now = DateTimeOffset.UtcNow;
            var first = CreateManager("first.text.manager", now);
            var second = CreateManager("second.text.manager", now);
            seedContext.ManagerAccounts.AddRange(first, second);
            await seedContext.SaveChangesAsync();
            firstManagerId = first.Id;
            secondManagerId = second.Id;
        }

        await using (var firstContext = database.CreateContext())
        {
            var textController = CreateTextController(firstContext, firstManagerId);
            var firstBind = await UpsertText(textController, "F5", "#0f172a");
            Assert.Equal("#0F172A", firstBind.TextColor);

            var rebound = await UpsertText(textController, "F7", "#0F172A");
            Assert.Equal(firstBind.Id, rebound.Id);
            Assert.Equal("F7", rebound.Key);

            var valueBindService = new BindService(new BindRepository(firstContext));
            await Assert.ThrowsAsync<ValidationException>(() => valueBindService.CreateAsync(
                new BusinessLogicLayer.Contracts.Models.BindModel
                {
                    Key = "F7",
                    Value = "+",
                    IsActive = true,
                },
                firstManagerId,
                CancellationToken.None));

            var fillConflict = await CreateFillController(firstContext, firstManagerId).Upsert(
                new SaveManagerGraphFillColorBindRequest { Key = "F7", FillColor = "#DBEAFE" },
                CancellationToken.None);
            AssertValidationError(fillConflict.Result, nameof(SaveManagerGraphFillColorBindRequest.Key));

            await UpsertFill(CreateFillController(firstContext, firstManagerId), "F4", "#DBEAFE");
            var textConflict = await textController.Upsert(
                new SaveManagerGraphTextColorBindRequest { Key = "F4", TextColor = "#2563EB" },
                CancellationToken.None);
            AssertValidationError(textConflict.Result, nameof(SaveManagerGraphTextColorBindRequest.Key));
        }

        await using (var secondContext = database.CreateContext())
        {
            var secondBind = await UpsertText(CreateTextController(secondContext, secondManagerId), "F7", "#E11D48");
            Assert.Equal("#E11D48", secondBind.TextColor);
        }

        await using (var verificationContext = database.CreateContext())
        {
            var firstResult = await CreateTextController(verificationContext, firstManagerId).GetCurrent(CancellationToken.None);
            var firstBinds = Assert.IsAssignableFrom<IReadOnlyList<ManagerGraphTextColorBindDto>>(
                Assert.IsType<OkObjectResult>(firstResult.Result).Value);
            Assert.Equal("#0F172A", Assert.Single(firstBinds).TextColor);

            var secondResult = await CreateTextController(verificationContext, secondManagerId).GetCurrent(CancellationToken.None);
            var secondBinds = Assert.IsAssignableFrom<IReadOnlyList<ManagerGraphTextColorBindDto>>(
                Assert.IsType<OkObjectResult>(secondResult.Result).Value);
            Assert.Equal("#E11D48", Assert.Single(secondBinds).TextColor);
        }
    }

    private static async Task<ManagerGraphTextColorBindDto> UpsertText(
        ManagerGraphTextColorBindsController controller,
        string key,
        string textColor)
    {
        var result = await controller.Upsert(
            new SaveManagerGraphTextColorBindRequest { Key = key, TextColor = textColor },
            CancellationToken.None);
        return Assert.IsType<ManagerGraphTextColorBindDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    private static async Task<ManagerGraphFillColorBindDto> UpsertFill(
        ManagerGraphFillColorBindsController controller,
        string key,
        string fillColor)
    {
        var result = await controller.Upsert(
            new SaveManagerGraphFillColorBindRequest { Key = key, FillColor = fillColor },
            CancellationToken.None);
        return Assert.IsType<ManagerGraphFillColorBindDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    private static void AssertValidationError(IActionResult? result, string field)
    {
        var validationResult = Assert.IsType<ObjectResult>(result);
        var validationProblem = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
        Assert.Contains(field, validationProblem.Errors.Keys);
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

    private static ManagerGraphTextColorBindsController CreateTextController(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        int managerId)
    {
        var controller = new ManagerGraphTextColorBindsController(context);
        SetManagerPrincipal(controller, managerId);
        return controller;
    }

    private static ManagerGraphFillColorBindsController CreateFillController(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        int managerId)
    {
        var controller = new ManagerGraphFillColorBindsController(context);
        SetManagerPrincipal(controller, managerId);
        return controller;
    }

    private static void SetManagerPrincipal(ControllerBase controller, int managerId)
    {
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
    }
}
