using System.Security.Claims;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;

namespace WebApi.Controllers;

[ApiController]
[Route("api/account-language")]
[Authorize(Roles = AuthRoles.Manager + "," + AuthRoles.Employee)]
public sealed class AccountLanguageController(AppDbContext db) : ControllerBase
{
    public sealed record LanguagePreference(string Language);

    [HttpGet]
    public async Task<ActionResult<LanguagePreference>> Get(CancellationToken cancellationToken)
    {
        var language = User.IsInRole(AuthRoles.Manager)
            ? await db.ManagerAccounts.Where(account => account.Id == AccountId("manager_id"))
                .Select(account => account.Language).SingleOrDefaultAsync(cancellationToken)
            : await db.EmployeeAccounts.Where(account => account.EmployeeId == AccountId("employee_id"))
                .Select(account => account.Language).SingleOrDefaultAsync(cancellationToken);

        return language is null ? Unauthorized() : Ok(new LanguagePreference(language));
    }

    [HttpPut]
    public async Task<ActionResult<LanguagePreference>> Update(
        LanguagePreference preference, CancellationToken cancellationToken)
    {
        if (preference.Language is not ("en" or "pl"))
        {
            ModelState.AddModelError("language", "Choose English or Polish.");
            return ValidationProblem(ModelState);
        }

        var updated = User.IsInRole(AuthRoles.Manager)
            ? await db.ManagerAccounts.Where(account => account.Id == AccountId("manager_id"))
                .ExecuteUpdateAsync(setters => setters.SetProperty(account => account.Language, preference.Language), cancellationToken)
            : await db.EmployeeAccounts.Where(account => account.EmployeeId == AccountId("employee_id"))
                .ExecuteUpdateAsync(setters => setters.SetProperty(account => account.Language, preference.Language), cancellationToken);

        return updated == 0 ? Unauthorized() : Ok(preference);
    }

    private int AccountId(string claim) => int.TryParse(User.FindFirstValue(claim), out var id) ? id : 0;
}
