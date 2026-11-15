using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Options;

namespace WebApi.Controllers;

[ApiController]
[Route("api/employee-push")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeePushController(AppDbContext db, IOptions<PushOptions> options, TimeProvider timeProvider) : ControllerBase
{
    public sealed record BrowserKeys([Required, MaxLength(512)] string P256dh, [Required, MaxLength(256)] string Auth);
    public sealed record SubscriptionRequest([Required, MaxLength(2048)] string Endpoint, [Required] BrowserKeys Keys);
    public sealed record DeleteRequest([Required, MaxLength(2048)] string Endpoint);

    [HttpGet("config")]
    public IActionResult GetConfig() => Ok(new { enabled = options.Value.Enabled, publicKey = options.Value.Enabled ? options.Value.VapidPublicKey : null });

    [HttpPut("subscriptions")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Put(SubscriptionRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) || employeeId <= 0) return Forbid();
        if (!options.Value.Enabled) return Problem(statusCode: 503, title: "Push is not configured on the server.");
        if (request.Keys is null || !options.Value.AllowsEndpoint(request.Endpoint) ||
            PushOptions.DecodeKey(request.Keys.P256dh, 65) is not { } publicKey || publicKey[0] != 4 ||
            PushOptions.DecodeKey(request.Keys.Auth, 16) is null) return BadRequest();
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var subscription = await db.EmployeePushSubscriptions.SingleOrDefaultAsync(x => x.Endpoint == request.Endpoint, ct);
        if (subscription is null)
        {
            subscription = new EmployeePushSubscriptionModel { Endpoint = request.Endpoint, EmployeeId = employeeId, CreatedAtUtc = now };
            db.EmployeePushSubscriptions.Add(subscription);
        }
        else if (subscription.EmployeeId != employeeId)
        {
            await db.EmployeePushDeliveries.Where(x => x.SubscriptionId == subscription.Id).ExecuteDeleteAsync(ct);
            subscription.EmployeeId = employeeId;
            subscription.CreatedAtUtc = now;
        }
        subscription.P256dh = request.Keys.P256dh;
        subscription.Auth = request.Keys.Auth;
        subscription.UpdatedAtUtc = now;
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException) { return Conflict(new ProblemDetails { Title = "Subscription changed; please retry.", Status = 409 }); }
        return NoContent();
    }

    [HttpDelete("subscriptions")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Delete(DeleteRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) || employeeId <= 0) return Forbid();
        if (!options.Value.AllowsEndpoint(request.Endpoint)) return BadRequest();
        await db.EmployeePushSubscriptions.Where(x => x.EmployeeId == employeeId && x.Endpoint == request.Endpoint).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}
