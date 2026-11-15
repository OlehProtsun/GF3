using System.Net;
using System.Net.Http.Json;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.EmploymentContracts;

namespace GF3.Tests;

public sealed class EmploymentContractSettingsTests
{
    [Fact]
    public async Task AuthorizationMissingFieldsPersistenceAndIndependentFlags()
    {
        const string endpoint = "/api/employment-contract-settings";
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync(endpoint)).StatusCode);
        await host.Login("employee"); Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync(endpoint, new { })).StatusCode);
        await host.Login("manager"); await host.Exchange("phone");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync(endpoint, new { })).StatusCode);
        await host.Exchange("pc");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync(endpoint, new { restrictSwapsToEightHours = false })).StatusCode);
        for (var index = 0; index < 4; index++)
        {
            var expected = new EmploymentContractSettingsDto(index != 0, index != 1, index != 2, index != 3);
            var response = await host.Client.PutAsJsonAsync(endpoint, expected); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, await response.Content.ReadFromJsonAsync<EmploymentContractSettingsDto>());
            Assert.Equal(expected, await host.Client.GetFromJsonAsync<EmploymentContractSettingsDto>(endpoint));
        }
        await using var db = host.Database.CreateContext(); Assert.Single(await db.EmploymentContractSettings.ToListAsync());
        Assert.True((await db.PriorityGenerationSettings.SingleAsync()).RespectAvailability);
    }

    [Fact]
    public async Task DefaultMissingSingletonAndDynamicRules()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee(); db.Add(employee); await db.SaveChangesAsync();
        var policy = new EmploymentContractPolicyService(db);
        Assert.Equal(new EmploymentContractSettingsSnapshot(), await policy.GetSettingsAsync());
        Assert.False(await policy.IsRuleActiveAsync(employee.Id, EmploymentContractRule.RequirePickupApproval));
        employee.ContractType = "o_prace"; await db.SaveChangesAsync();
        Assert.True(await policy.IsRuleActiveAsync(employee.Id, EmploymentContractRule.RequirePickupApproval));
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval, false));
        Assert.False(await policy.IsRuleActiveAsync(employee.Id, EmploymentContractRule.RequirePickupApproval));
        await db.EmploymentContractSettings.ExecuteDeleteAsync(); Assert.Equal(new EmploymentContractSettingsSnapshot(), await policy.GetSettingsAsync());
        employee.IsDeleted = true; await db.SaveChangesAsync(); await Assert.ThrowsAsync<KeyNotFoundException>(() => policy.GetContractTypeAsync(employee.Id));
        Assert.Empty(await policy.GetContractTypesAsync([employee.Id]));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => policy.IsRuleActiveAsync(employee.Id, (EmploymentContractRule)99));
    }
}
