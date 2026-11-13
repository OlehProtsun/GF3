using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApi.Auth;
using WebApi.Contracts.ManagerHome;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/manager-home")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerHomeController(AppDbContext db) : ControllerBase
{
    [HttpGet("nav-badges")]
    [ProducesResponseType<ManagerNavBadgesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagerNavBadgesDto>> NavBadges(CancellationToken cancellationToken)
        => Ok(await ManagerHomeActivityReader.ReadNavBadgesAsync(db, cancellationToken));

    [HttpGet("activity")]
    [ProducesResponseType<ManagerHomeActivityPageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagerHomeActivityPageDto>> Activity(
        [FromQuery, BindRequired, Range(1, 9999)] int year,
        [FromQuery, BindRequired, Range(1, 12)] int month,
        CancellationToken cancellationToken,
        [FromQuery, RegularExpression("^(all|approvals)$")] string tab = "all",
        [FromQuery, Range(0, 10000)] int offset = 0,
        [FromQuery, Range(1, 50)] int limit = 7)
        => Ok(await ManagerHomeActivityReader.ReadAsync(db, year, month, tab, offset, limit, cancellationToken));
}
