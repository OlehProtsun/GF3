using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.Shops;
using WebApi.Mappers;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController(IShopFacade shopFacade) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ShopDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ShopDto>>> GetAll(CancellationToken cancellationToken)
    {
        var shops = await shopFacade.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(shops.Select(x => x.ToApiDto()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShopDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var shop = await shopFacade.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (shop is null)
        {
            throw new KeyNotFoundException($"Shop with id {id} was not found.");
        }

        return Ok(shop.ToApiDto());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShopDto>> Create([FromBody] CreateShopRequest request, CancellationToken cancellationToken)
    {
        var created = await shopFacade.CreateAsync(request.ToSaveRequest(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToApiDto();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShopRequest request, CancellationToken cancellationToken)
    {
        var existing = await shopFacade.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Shop with id {id} was not found.");
        }

        await shopFacade.UpdateAsync(request.ToSaveRequest(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await shopFacade.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Shop with id {id} was not found.");
        }

        await shopFacade.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
