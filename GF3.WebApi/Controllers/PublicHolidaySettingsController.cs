using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.PublicHolidays;

namespace WebApi.Controllers;

[ApiController]
[Route("api/public-holiday-settings")]
[Authorize(Roles = AuthRoles.Manager + "," + AuthRoles.Employee)]
public sealed class PublicHolidaySettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicHolidayDisplaySettingsDto>> Get(CancellationToken cancellationToken)
    {
        var enabled = await db.PublicHolidaySettings.AsNoTracking()
            .Where(settings => settings.Id == PublicHolidaySettingsModel.SingletonId)
            .Select(settings => (bool?)settings.ShowPublicHolidays)
            .SingleOrDefaultAsync(cancellationToken);
        return Ok(new PublicHolidayDisplaySettingsDto(enabled ?? true));
    }

    [HttpPut]
    [Authorize(Roles = AuthRoles.Manager)]
    public async Task<ActionResult<PublicHolidayDisplaySettingsDto>> Update(
        UpdatePublicHolidayDisplaySettingsRequest request, CancellationToken cancellationToken)
    {
        if (!request.ShowPublicHolidays.HasValue)
        {
            ModelState.AddModelError(nameof(request.ShowPublicHolidays), "The ShowPublicHolidays field is required.");
            return ValidationProblem(ModelState);
        }

        var enabled = request.ShowPublicHolidays.Value;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public_holiday_settings (id, show_public_holidays)
            VALUES ({PublicHolidaySettingsModel.SingletonId}, {enabled})
            ON CONFLICT(id) DO UPDATE SET show_public_holidays=excluded.show_public_holidays
            """, cancellationToken);
        return Ok(new PublicHolidayDisplaySettingsDto(enabled));
    }
}
