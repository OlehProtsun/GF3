using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.EmploymentContracts;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/employment-contract-settings")]
[Authorize(Roles = AuthRoles.Manager + "," + AuthRoles.Employee)]
public sealed class EmploymentContractSettingsController(AppDbContext db,
    IEmploymentContractPolicyService policy, IRealtimeNotifier notifier) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EmploymentContractSettingsDto>> Get(CancellationToken ct)
    {
        var settings = await policy.GetSettingsAsync(ct);
        return Ok(new EmploymentContractSettingsDto(settings.RestrictSwapsToEightHours, settings.RequirePickupApproval,
            settings.RequireBelowMinimumGiveApproval, settings.AutomaticWeekdayAvailability));
    }

    [HttpPut]
    [Authorize(Roles = AuthRoles.Manager)]
    public async Task<ActionResult<EmploymentContractSettingsDto>> Update(UpdateEmploymentContractSettingsRequest request, CancellationToken ct)
    {
        if (!request.RestrictSwapsToEightHours.HasValue || !request.RequirePickupApproval.HasValue ||
            !request.RequireBelowMinimumGiveApproval.HasValue || !request.AutomaticWeekdayAvailability.HasValue)
        {
            ModelState.AddModelError("settings", "All employment contract settings are required.");
            return ValidationProblem(ModelState);
        }
        var value = new EmploymentContractSettingsDto(request.RestrictSwapsToEightHours.Value, request.RequirePickupApproval.Value,
            request.RequireBelowMinimumGiveApproval.Value, request.AutomaticWeekdayAvailability.Value);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO employment_contract_settings (id, restrict_swaps_to_eight_hours, require_pickup_approval,
                require_below_minimum_give_approval, automatic_weekday_availability)
            VALUES (1, {value.RestrictSwapsToEightHours}, {value.RequirePickupApproval},
                {value.RequireBelowMinimumGiveApproval}, {value.AutomaticWeekdayAvailability})
            ON CONFLICT(id) DO UPDATE SET restrict_swaps_to_eight_hours=excluded.restrict_swaps_to_eight_hours,
                require_pickup_approval=excluded.require_pickup_approval,
                require_below_minimum_give_approval=excluded.require_below_minimum_give_approval,
                automatic_weekday_availability=excluded.automatic_weekday_availability
            """, ct);
        await PostCommitActions.RunAsync(HttpContext, () => notifier.NotifyManagerDataChangedAsync(
            "employment-contract-settings", "1", "employment-contract-settings-updated"));
        return Ok(value);
    }
}
