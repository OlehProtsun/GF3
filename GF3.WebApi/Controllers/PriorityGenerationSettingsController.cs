using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.Priority;

namespace WebApi.Controllers;

[ApiController]
[Route("api/priority-generation-settings")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class PriorityGenerationSettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PriorityGenerationSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await db.PriorityGenerationSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == PriorityGenerationSettingsModel.SingletonId, cancellationToken);
        return Ok(new PriorityGenerationSettingsDto(settings?.ShiftHours ?? 8, settings?.WeekendsOff ?? true,
            settings?.RespectAvailability ?? true));
    }

    [HttpPut]
    public async Task<ActionResult<PriorityGenerationSettingsDto>> Update(
        UpdatePriorityGenerationSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request.ShiftHours is not (>= 1 and <= 12))
            ModelState.AddModelError(nameof(request.ShiftHours), "Shift hours must be an integer between 1 and 12.");
        if (!request.WeekendsOff.HasValue)
            ModelState.AddModelError(nameof(request.WeekendsOff), "The WeekendsOff field is required.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var hours = request.ShiftHours!.Value;
        var weekendsOff = request.WeekendsOff!.Value;
        var respectAvailability = request.RespectAvailability;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO priority_generation_settings (id, shift_hours, weekends_off, respect_availability)
            VALUES ({PriorityGenerationSettingsModel.SingletonId}, {hours}, {weekendsOff}, COALESCE({respectAvailability}, 1))
            ON CONFLICT(id) DO UPDATE SET shift_hours=excluded.shift_hours, weekends_off=excluded.weekends_off,
                respect_availability=COALESCE({respectAvailability}, priority_generation_settings.respect_availability)
            """, cancellationToken);
        return await Get(cancellationToken);
    }
}
