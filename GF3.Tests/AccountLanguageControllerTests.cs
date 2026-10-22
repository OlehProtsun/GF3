using System.Security.Claims;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class AccountLanguageControllerTests
{
    [Fact]
    public async Task Language_IsPersistedAndIsolatedBetweenManagersAndEmployees()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee();
        context.Employees.Add(employee);
        var first = new ManagerAccountModel { Username = "first", DisplayName = "First", PasswordHash = "hash" };
        var second = new ManagerAccountModel { Username = "second", DisplayName = "Second", PasswordHash = "hash" };
        context.ManagerAccounts.AddRange(first, second);
        await context.SaveChangesAsync();
        context.EmployeeAccounts.Add(new EmployeeAccountModel { EmployeeId = employee.Id, Username = "worker", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        Assert.Equal("pl", first.Language);
        Assert.Equal("pl", new EmployeeAccountModel().Language);
        var managerController = Controller(context, "manager", "manager_id", first.Id);
        var workerController = Controller(context, "employee", "employee_id", employee.Id);
        Assert.Equal("pl", Preference(await managerController.Get(default)));
        Assert.Equal("pl", Preference(await workerController.Get(default)));
        Assert.Equal("en", Preference(await managerController.Update(new("en"), default)));
        Assert.Equal("pl", Preference(await Controller(context, "manager", "manager_id", second.Id).Get(default)));

        await using var fresh = database.CreateContext();
        var reloaded = Controller(fresh, "manager", "manager_id", first.Id);
        Assert.Equal("en", Preference(await reloaded.Get(default)));
        Assert.Equal("pl", Preference(await reloaded.Update(new("pl"), default)));
        Assert.Equal("pl", Preference(await Controller(fresh, "employee", "employee_id", employee.Id).Get(default)));
        Assert.Equal("pl", Preference(await Controller(fresh, "manager", "manager_id", second.Id).Get(default)));
        Assert.False(fresh.Database.HasPendingModelChanges());
    }

    [Theory]
    [InlineData("de")]
    [InlineData("PL")]
    [InlineData("")]
    [InlineData(null)]
    public async Task UnsupportedLanguage_IsRejected(string? language)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = Controller(context, "manager", "manager_id", 1);
        var result = await controller.Update(new(language!), default);
        Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task MissingIdentity_CannotReadOrChangePreference()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = Controller(context, "manager", "wrong_claim", 1);
        Assert.IsType<UnauthorizedResult>((await controller.Get(default)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Update(new("pl"), default)).Result);
    }

    [Fact]
    public async Task Migration_PreservesExistingLanguagePreferencesAndUsesPolishSchemaDefaults()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.ManagerAccounts.Add(new ManagerAccountModel { Language = "en", Username = "existing", DisplayName = "Existing", PasswordHash = "hash" });
        var employee = TestDataFactory.CreateDalEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        context.EmployeeAccounts.Add(new EmployeeAccountModel { Language = "pl", EmployeeId = employee.Id, Username = "existing-worker", PasswordHash = "hash" });
        await context.SaveChangesAsync();
        var migrator = context.GetService<IMigrator>();
        var previous = context.Database.GetMigrations().Reverse().Skip(1).First();
        await migrator.MigrateAsync(previous);
        await migrator.MigrateAsync();
        context.ChangeTracker.Clear();
        Assert.Equal("en", (await context.ManagerAccounts.SingleAsync()).Language);
        Assert.Equal("pl", (await context.EmployeeAccounts.SingleAsync()).Language);
        await context.Database.OpenConnectionAsync();
        foreach (var table in new[] { "manager_account", "employee_account" })
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"PRAGMA table_info({table});";
            var found = false;
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    if (reader.GetString(1) != "language") continue;
                    found = true;
                    Assert.Equal("TEXT", reader.GetString(2));
                    Assert.Equal(1L, reader.GetInt64(3));
                    Assert.Equal("'pl'", reader.GetString(4));
                }
            }
            Assert.True(found);
            command.CommandText = $"PRAGMA index_list({table});";
            var uniqueIndexes = new HashSet<string>();
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    if (reader.GetInt64(2) == 1) uniqueIndexes.Add(reader.GetString(1));
            Assert.Contains($"ux_{table}_username", uniqueIndexes);
            Assert.Contains(table == "manager_account" ? "ux_manager_account_system" : "ux_employee_account_employee", uniqueIndexes);
        }
        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await command.ExecuteReaderAsync();
            Assert.False(await reader.ReadAsync());
        }
        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "PRAGMA integrity_check;";
            Assert.Equal("ok", await command.ExecuteScalarAsync());
        }
        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static string Preference(ActionResult<AccountLanguageController.LanguagePreference> result)
        => Assert.IsType<AccountLanguageController.LanguagePreference>(Assert.IsType<OkObjectResult>(result.Result).Value).Language;

    private static AccountLanguageController Controller(DataAccessLayer.Models.DataBaseContext.AppDbContext db, string role, string claim, int id)
        => new(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role), new Claim(claim, id.ToString())], "test"))
                }
            }
        };
}
