using System.Net;
using System.Net.Http.Json;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Contracts.Employees;
using WebApi.Mappers;

namespace GF3.Tests;

public sealed class EmploymentContractEmployeeTests
{
    [Fact]
    public async Task DefaultsMappingsLegacyUpdateAndSelfContactPreserveContract()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var service = new EmployeeService(new EmployeeRepository(db));
        var facade = TestEmployeeFacadeFactory.Create(service);
        var created = await facade.CreateAsync(new SaveEmployeeRequest { FirstName = "Contract", LastName = "Worker" });
        Assert.Equal("zlecenie", created.ContractType);
        Assert.Equal("zlecenie", created.ToApiDto().ContractType);
        await facade.UpdateAsync(new SaveEmployeeRequest { Id = created.Id, FirstName = "Contract", LastName = "Worker", ContractType = "o_prace" });
        await facade.UpdateAsync(new SaveEmployeeRequest { Id = created.Id, FirstName = "Updated", LastName = "Worker" });
        Assert.Equal("o_prace", (await facade.GetAsync(created.Id))!.ContractType);
        Assert.Equal("o_prace", (await service.UpdateContactAsync(created.Id, "new@example.invalid", "123")).ContractType);
        var mapped = new UpdateEmployeeRequest { FirstName = "Updated", LastName = "Worker" }.ToSaveRequest(created.Id);
        Assert.Null(mapped.ContractType);
        await service.DeleteAsync(created.Id);
        await Assert.ThrowsAsync<ValidationException>(() => facade.UpdateAsync(mapped));
    }

    [Theory]
    [InlineData("O_PRACE")][InlineData("o_prace ")][InlineData("")][InlineData("unknown")]
    public async Task InvalidValuesAreAtomic(string value)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        await host.Login("manager"); await host.Exchange("pc");
        var response = await host.Client.PutAsJsonAsync("/api/employees/1", new { firstName = "MustNotPersist", lastName = "Employee", contractType = value });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = host.Database.CreateContext();
        Assert.Equal("Synthetic", (await db.Employees.SingleAsync()).FirstName);
        Assert.Equal("zlecenie", (await db.Employees.SingleAsync()).ContractType);
    }

    [Fact]
    public async Task EmployeeAndPhoneCannotAssignContract()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync("/api/employees/1", new { firstName = "Synthetic", lastName = "Employee", contractType = "o_prace" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/employee-profile/me", new { phone = "123", contractType = "o_prace" })).StatusCode);
        await host.Login("manager"); await host.Exchange("phone");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync("/api/employees/1", new { firstName = "Synthetic", lastName = "Employee", contractType = "o_prace" })).StatusCode);
        await using var db = host.Database.CreateContext(); Assert.Equal("zlecenie", (await db.Employees.SingleAsync()).ContractType);
    }

    [Fact]
    public async Task MigrationPreservesLegacyRowsChecksAndDowngrade()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var baseline = db.Database.GetMigrations().TakeWhile(x => x != "20261028180000_AddEmployeeContractType").Last();
        await db.GetService<IMigrator>().MigrateAsync(baseline);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO employee (first_name,last_name,is_deleted) VALUES ('Legacy','Active',0),('Legacy','Archived',1)");
        await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(2, await db.Employees.CountAsync()); Assert.All(await db.Employees.ToListAsync(), x => Assert.Equal("zlecenie", x.ContractType));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE employee SET contract_type='invalid'"));
        await db.Database.OpenConnectionAsync();
        await using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_check";
            await using var reader = await command.ExecuteReaderAsync(); Assert.False(await reader.ReadAsync());
        }
        await using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "PRAGMA quick_check"; Assert.Equal("ok", await command.ExecuteScalarAsync());
        }
        db.ChangeTracker.Clear(); await db.GetService<IMigrator>().MigrateAsync(baseline);
        await using var count = db.Database.GetDbConnection().CreateCommand(); count.CommandText = "SELECT COUNT(*) FROM employee";
        Assert.Equal(2L, await count.ExecuteScalarAsync());
    }
    [Fact]
    public async Task StartupMigrationOnBackedUpLegacyCopyPreservesSwapsHistoryVersionsAndForeignKeys()
    {
        await using var database=await SqliteTestDatabase.CreateAsync(); await using var source=database.CreateContext();
        var fixture=await ShiftSwapScenarioFactory.SeedAsync(source,new());
        source.EmployeeAccounts.Add(new() { EmployeeId=fixture.Owner,Username="legacy-owner",PasswordHash="synthetic-only",PasswordUpdatedAtUtc=DateTimeOffset.UtcNow });
        await source.SaveChangesAsync();
        var engine=ContractTestFactory.Engine(source);
        var offer=await engine.Create(fixture.Owner,ContractTestFactory.Employee(fixture.Owner),ShiftSwapScenarioFactory.Request(fixture,new()),default);
        await engine.Accept(fixture.Target,ContractTestFactory.Employee(fixture.Target),offer.Id,default);
        var history=(await source.ShiftSwapHistories.AsNoTracking().SingleAsync()).AfterSnapshotJson;
        var baseline=source.Database.GetMigrations().TakeWhile(x=>x!="20261028180000_AddEmployeeContractType").Last();
        source.ChangeTracker.Clear(); await source.GetService<IMigrator>().MigrateAsync(baseline);
        var tables=new[] {"employee","employee_account","schedule","schedule_employee","schedule_slot","shift_swap_request","shift_swap_history","schedule_version","schedule_version_state"};
        var counts=new Dictionary<string,long>(); await source.Database.OpenConnectionAsync();
        foreach(var table in tables)
        {
            await using var command=source.Database.GetDbConnection().CreateCommand(); command.CommandText=$"SELECT COUNT(*) FROM \"{table}\"";
            counts[table]=(long)(await command.ExecuteScalarAsync())!;
        }
        var copyPath=Path.Combine(database.RootPath,"legacy-copy.db");
        using(var destination=new SqliteConnection($"Data Source={copyPath}"))
        { await destination.OpenAsync(); ((SqliteConnection)source.Database.GetDbConnection()).BackupDatabase(destination); }
        var options=new DbContextOptionsBuilder<DataAccessLayer.Models.DataBaseContext.AppDbContext>().UseSqlite($"Data Source={copyPath}").Options;
        await using var copy=new DataAccessLayer.Models.DataBaseContext.AppDbContext(options);
        var workspace=new DataAccessLayer.Administration.SqliteDatabaseWorkspace(copyPath);
        var migration=await WebApi.Infrastructure.DatabaseMigrationStartup.ApplyAsync(copy,workspace,true,true,Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        Assert.NotNull(migration.BackupPath); Assert.True(File.Exists(migration.BackupPath));
        Assert.Equal(4,migration.AppliedMigrations.Count);
        await WebApi.Infrastructure.DatabaseMigrationStartup.ApplyAsync(copy,workspace,true,true,Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        Assert.All(await copy.Employees.AsNoTracking().ToListAsync(),employee=>Assert.Equal("zlecenie",employee.ContractType));
        Assert.Equal(history,(await copy.ShiftSwapHistories.AsNoTracking().SingleAsync()).AfterSnapshotJson);
        await copy.Database.OpenConnectionAsync();
        foreach(var table in tables)
        {
            await using var command=copy.Database.GetDbConnection().CreateCommand(); command.CommandText=$"SELECT COUNT(*) FROM \"{table}\"";
            Assert.Equal(counts[table],(long)(await command.ExecuteScalarAsync())!);
        }
        await using(var command=copy.Database.GetDbConnection().CreateCommand())
        { command.CommandText="PRAGMA foreign_key_check"; await using var reader=await command.ExecuteReaderAsync(); Assert.False(await reader.ReadAsync()); }
        await using(var command=copy.Database.GetDbConnection().CreateCommand())
        { command.CommandText="PRAGMA quick_check"; Assert.Equal("ok",await command.ExecuteScalarAsync()); }
        Assert.DoesNotContain("20261028180000_AddEmployeeContractType",await source.Database.GetAppliedMigrationsAsync());
    }

}
