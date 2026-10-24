using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Communications;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GF3.Tests;

public sealed class CommunicationServiceTests
{
    [Fact]
    public async Task CreateAndDismissCommunication_ForEmployeePendingMessages()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var service = new CommunicationService(new CommunicationRepository(context));
        var created = await service.CreateAsync(
            new CreateCommunicationMessageRequest
            {
                Title = " Team update ",
                Body = " Read before the shift. ",
                VisibleFromUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                DeadlineAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            },
            managerId: null,
            managerDisplayName: " Lead Manager ",
            ct: CancellationToken.None);

        var pendingBeforeDismiss = await service.GetPendingForEmployeeAsync(employee.Id);
        await service.DismissForEmployeeAsync(employee.Id, created.Id);
        await service.DismissForEmployeeAsync(employee.Id, created.Id);
        var pendingAfterDismiss = await service.GetPendingForEmployeeAsync(employee.Id);
        var dismissals = await context.EmployeeCommunicationDismissals.AsNoTracking().ToListAsync();

        Assert.Equal("Team update", created.Title);
        Assert.Equal("Read before the shift.", created.Body);
        Assert.Equal("Lead Manager", created.CreatedByManagerName);
        Assert.True(created.IsActive);
        Assert.Single(pendingBeforeDismiss);
        Assert.Empty(pendingAfterDismiss);
        Assert.Single(dismissals);
        Assert.Equal(employee.Id, dismissals[0].EmployeeId);
        Assert.Equal(created.Id, dismissals[0].CommunicationMessageId);
    }

    [Fact]
    public async Task PendingMessages_ExcludeExpiredAndDismissedMessages()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var active = CreateDalCommunication("Active", now.AddMinutes(-10), now.AddHours(1));
        var expired = CreateDalCommunication("Expired", now.AddHours(-3), now.AddMinutes(-1));
        var scheduled = CreateDalCommunication("Scheduled", now.AddMinutes(30), now.AddHours(2));
        var dismissed = CreateDalCommunication("Dismissed", now.AddMinutes(-5), now.AddHours(2));
        context.CommunicationMessages.AddRange(active, expired, scheduled, dismissed);
        await context.SaveChangesAsync();
        context.EmployeeCommunicationDismissals.Add(new EmployeeCommunicationDismissalModel
        {
            CommunicationMessageId = dismissed.Id,
            EmployeeId = employee.Id,
            DismissedAtUtc = now,
        });
        await context.SaveChangesAsync();

        var repository = new CommunicationRepository(context);
        var pending = await repository.GetPendingForEmployeeAsync(employee.Id, now);

        Assert.Single(pending);
        Assert.Equal(active.Id, pending[0].Id);
    }

    [Fact]
    public async Task ManagerList_OrdersNewestFirst_OnSqlite()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var older = CreateDalCommunication("Older", now.AddMinutes(-20), now.AddHours(1));
        var newer = CreateDalCommunication("Newer", now.AddMinutes(-10), now.AddHours(2));
        context.CommunicationMessages.AddRange(older, newer);
        await context.SaveChangesAsync();

        var service = new CommunicationService(new CommunicationRepository(context));
        var messages = await service.ListForManagerAsync();

        Assert.Equal([newer.Id, older.Id], messages.Select(message => message.Id));
    }

    [Fact]
    public async Task UpdateAndDeleteCommunication_PreserveCreatorAndRemoveDismissals()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var service = new CommunicationService(new CommunicationRepository(context));
        var created = await service.CreateAsync(
            new CreateCommunicationMessageRequest
            {
                Title = "Original",
                Body = "Original body",
                VisibleFromUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                DeadlineAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            },
            managerId: null,
            managerDisplayName: "Lead Manager");
        await service.DismissForEmployeeAsync(employee.Id, created.Id);

        var updated = await service.UpdateAsync(created.Id, new UpdateCommunicationMessageRequest
        {
            Title = " Updated title ",
            Body = " Updated body ",
            VisibleFromUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            DeadlineAtUtc = DateTimeOffset.UtcNow.AddHours(4),
        });

        Assert.Equal("Updated title", updated.Title);
        Assert.Equal("Updated body", updated.Body);
        Assert.Equal("Lead Manager", updated.CreatedByManagerName);
        Assert.Equal(created.CreatedAtUtc, updated.CreatedAtUtc);
        Assert.False(updated.IsActive);

        await service.DeleteAsync(created.Id);

        Assert.Empty(await context.CommunicationMessages.AsNoTracking().ToListAsync());
        Assert.Empty(await context.EmployeeCommunicationDismissals.AsNoTracking().ToListAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(created.Id, new UpdateCommunicationMessageRequest()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task CreateCommunication_RejectsInvalidInput()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new CommunicationService(new CommunicationRepository(context));

        var missingTitle = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCommunicationMessageRequest
            {
                Title = " ",
                Body = "Message",
                VisibleFromUtc = DateTimeOffset.UtcNow,
                DeadlineAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            }, null, "Manager"));
        var pastDeadline = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCommunicationMessageRequest
            {
                Title = "Title",
                Body = "Message",
                VisibleFromUtc = DateTimeOffset.UtcNow.AddHours(1),
                DeadlineAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            }, null, "Manager"));
        var invalidRange = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCommunicationMessageRequest
            {
                Title = "Title",
                Body = "Message",
                VisibleFromUtc = DateTimeOffset.UtcNow.AddHours(2),
                DeadlineAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            }, null, "Manager"));

        Assert.Equal(["Title is required."], missingTitle.Errors["title"]);
        Assert.Equal(["Visible to must be in the future."], pastDeadline.Errors["deadlineAtUtc"]);
        Assert.Equal(["Visible to must be later than visible from."], invalidRange.Errors["deadlineAtUtc"]);
    }

    private static CommunicationMessageModel CreateDalCommunication(
        string title,
        DateTimeOffset createdAtUtc,
        DateTimeOffset deadlineAtUtc)
        => new()
        {
            Title = title,
            Body = $"{title} body",
            VisibleFromUtc = createdAtUtc,
            CreatedAtUtc = createdAtUtc,
            DeadlineAtUtc = deadlineAtUtc,
            CreatedByManagerName = "Manager",
        };
}
