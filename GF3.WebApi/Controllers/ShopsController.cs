using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Shops;
using WebApi.Infrastructure;
using WebApi.Mappers;
using WebApi.Realtime;

namespace WebApi.Controllers;

/// <summary>
/// CRUD endpoints for shops.
/// The controller stays intentionally thin and delegates all business decisions to the facade layer,
/// while still keeping the HTTP contract explicit and easy to follow for API consumers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AuthRoles.Manager)]
public class ShopsController(
    IShopFacade shopFacade,
    IRealtimeNotifier? realtimeNotifier = null,
    IManagerEditLockService? editLockService = null) : ControllerBase
{
    /// <summary>
    /// Returns all shops as API DTOs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ShopDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ShopDto>>> GetAll(CancellationToken cancellationToken)
    {
        var shops = await shopFacade.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(shops.Select(shop => shop.ToApiDto()));
    }

    /// <summary>
    /// Returns one shop by id or fails with a not-found problem.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShopDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var shop = await GetRequiredShopAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(shop.ToApiDto());
    }

    /// <summary>
    /// Creates a new shop and returns its canonical API representation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShopDto>> Create([FromBody] CreateShopRequest request, CancellationToken cancellationToken)
    {
        var created = await shopFacade.CreateAsync(request.ToSaveRequest(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToApiDto();
        await NotifyShopChangedAsync(dto.Id, "manager-shop-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Updates an existing shop.
    /// We explicitly check existence first so the endpoint always responds with a clear 404 when
    /// the resource is missing, regardless of the underlying persistence behavior.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShopRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(id) is { } conflict)
        {
            return conflict;
        }

        await EnsureShopExistsAsync(id, cancellationToken).ConfigureAwait(false);
        await shopFacade.UpdateAsync(request.ToSaveRequest(id), cancellationToken).ConfigureAwait(false);
        await NotifyShopChangedAsync(id, "manager-shop-updated").ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Deletes a shop when no schedules or presets still reference it.
    /// Expected business-rule failures are returned as validation problems so the frontend
    /// can present a stable error state without relying on unhandled exceptions.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(id) is { } conflict)
        {
            return conflict;
        }

        await EnsureShopExistsAsync(id, cancellationToken).ConfigureAwait(false);

        var result = await shopFacade.TryDeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return CreateDeleteValidationResult(result);
        }

        await NotifyShopChangedAsync(id, "manager-shop-deleted").ConfigureAwait(false);
        return NoContent();
    }

    private ActionResult? CreateEditLockConflictResult(int shopId)
        => ManagerEditLockHttp.CreateConflictResult(
            this,
            editLockService,
            ManagerEditLockTargets.Shop(shopId),
            "This shop");

    private Task NotifyShopChangedAsync(int shopId, string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.Shop,
            shopId.ToString(),
            reason) ?? Task.CompletedTask;

    private async Task EnsureShopExistsAsync(int id, CancellationToken cancellationToken)
        => _ = await GetRequiredShopAsync(id, cancellationToken).ConfigureAwait(false);

    private async Task<BusinessLogicLayer.Contracts.Shops.ShopDto> GetRequiredShopAsync(int id, CancellationToken cancellationToken)
        => await shopFacade.GetAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shop with id {id} was not found.");

    private BadRequestObjectResult CreateDeleteValidationResult(DeleteOperationResult result)
        => BadRequest(ApiProblemDetailsFactory.CreateValidationProblem(
            HttpContext,
            result.Errors,
            result.Message));
}
