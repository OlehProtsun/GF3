using System.Security.Claims;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class ManagerNotepadControllerTests
{
    [Fact]
    public async Task NotesAndWindowState_ArePersistedAndIsolatedPerManager()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int firstManagerId;
        int secondManagerId;
        int firstNoteId;

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
            var createdResult = await controller.CreateNote(new SaveManagerNoteRequest
            {
                Title = "Release checklist",
                Content = "Verify the schedule before publishing.",
                Color = "blue",
            }, CancellationToken.None);
            var created = Assert.IsType<ManagerNoteDto>(Assert.IsType<ObjectResult>(createdResult.Result).Value);
            Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(createdResult.Result).StatusCode);
            firstNoteId = created.Id;

            Assert.IsType<NoContentResult>(await controller.SaveState(new SaveManagerNotepadStateRequest
            {
                IsExpanded = true,
                IsPinned = true,
                Height = 680,
            }, CancellationToken.None));
        }

        await using (var secondContext = database.CreateContext())
        {
            var controller = CreateController(secondContext, secondManagerId);
            await controller.CreateNote(new SaveManagerNoteRequest
            {
                Title = "Private manager note",
                Content = "Only the second manager can see this.",
                Color = "rose",
            }, CancellationToken.None);
            var foreignUpdate = await controller.UpdateNote(firstNoteId, new SaveManagerNoteRequest
            {
                Title = "Changed",
                Content = "Changed",
                Color = "slate",
            }, CancellationToken.None);
            Assert.IsType<NotFoundResult>(foreignUpdate.Result);
        }

        await using (var verificationContext = database.CreateContext())
        {
            var controller = CreateController(verificationContext, firstManagerId);
            var result = await controller.GetCurrent(CancellationToken.None);
            var notepad = Assert.IsType<ManagerNotepadDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
            var note = Assert.Single(notepad.Notes);
            Assert.Equal("Release checklist", note.Title);
            Assert.Equal("blue", note.Color);
            Assert.True(notepad.State.IsExpanded);
            Assert.True(notepad.State.IsPinned);
            Assert.Equal(680, notepad.State.Height);
        }
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

    private static ManagerNotepadController CreateController(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        int managerId)
    {
        var controller = new ManagerNotepadController(context);
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
