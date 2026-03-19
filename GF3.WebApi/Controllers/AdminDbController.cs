using System.ComponentModel.DataAnnotations;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApi.Contracts.AdminDb;
using WebApi.Options;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin/db")]
public sealed class AdminDbController : ControllerBase
{
    private readonly IAdminDbService _adminDbService;
    private readonly IOptions<AdminToolsOptions> _adminOptions;

    public AdminDbController(IAdminDbService adminDbService, IOptions<AdminToolsOptions> adminOptions)
    {
        _adminDbService = adminDbService;
        _adminOptions = adminOptions;
    }

    [HttpGet("metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Metadata(CancellationToken cancellationToken)
        => Ok(await _adminDbService.GetMetadataAsync(cancellationToken).ConfigureAwait(false));

    [HttpGet("hash")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Hash(CancellationToken cancellationToken)
        => Ok(new { hash = await _adminDbService.GetDbHashAsync(cancellationToken).ConfigureAwait(false) });

    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Query([FromBody] AdminDbSqlRequest request, CancellationToken cancellationToken)
    {
        EnsureSqlProvided(request.Sql);
        var result = await _adminDbService
            .ExecuteQueryAsync(request.Sql, _adminOptions.Value.MaxSqlLength, cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute([FromBody] AdminDbSqlRequest request, CancellationToken cancellationToken)
    {
        EnsureWriteEnabled();
        EnsureSqlProvided(request.Sql);

        var affected = await _adminDbService
            .ExecuteNonQueryAsync(request.Sql, _adminOptions.Value.MaxSqlLength, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new { affectedRows = affected });
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        EnsureWriteEnabled();

        if (file is null || file.Length <= 0)
        {
            throw new ValidationException("SQL file is required.");
        }

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

        var result = await _adminDbService
            .ImportSqlAsync(memory.ToArray(), _adminOptions.Value.MaxImportBytes, cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    private static void EnsureSqlProvided(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ValidationException("SQL is required.");
        }
    }

    private void EnsureWriteEnabled()
    {
        if (!_adminOptions.Value.AllowWriteSql)
        {
            throw new ValidationException("Write SQL operations are disabled.");
        }
    }
}
