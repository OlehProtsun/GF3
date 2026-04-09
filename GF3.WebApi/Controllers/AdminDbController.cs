using System.ComponentModel.DataAnnotations;
using BusinessLogicLayer.Contracts.Database;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApi.Contracts.AdminDb;
using WebApi.Options;

namespace WebApi.Controllers;

/// <summary>
/// Provides operational endpoints for inspecting and maintaining the SQLite database.
/// The controller stays intentionally conservative: write operations are guarded by configuration and all
/// SQL requests are validated before execution so accidental misuse fails fast with a clear problem response.
/// </summary>
[ApiController]
[Route("api/admin/db")]
public sealed class AdminDbController : ControllerBase
{
    private readonly IAdminDbService _adminDbService;
    private readonly AdminToolsOptions _options;

    public AdminDbController(IAdminDbService adminDbService, IOptions<AdminToolsOptions> adminOptions)
    {
        _adminDbService = adminDbService;
        _options = adminOptions.Value;
    }

    [HttpGet("metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Metadata(CancellationToken cancellationToken)
    {
        var metadata = await _adminDbService.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        return Ok(CreateMetadataResponse(metadata));
    }

    [HttpGet("hash")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Hash(CancellationToken cancellationToken) =>
        Ok(new { hash = await _adminDbService.GetDbHashAsync(cancellationToken).ConfigureAwait(false) });

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
            .ExecuteQueryAsync(request.Sql, _options.MaxSqlLength, cancellationToken)
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

        var affectedRows = await _adminDbService
            .ExecuteNonQueryAsync(request.Sql, _options.MaxSqlLength, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new { affectedRows });
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

        var bytes = await ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
        var result = await _adminDbService
            .ImportSqlAsync(bytes, _options.MaxImportBytes, cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    private object CreateMetadataResponse(AdminDbMetadataDto metadata) => new
    {
        metadata.SqliteVersion,
        metadata.DatabasePath,
        metadata.FileSizeBytes,
        metadata.LastModifiedUtc,
        metadata.UserVersion,
        metadata.Tables,
        metadata.Objects,
        allowWriteSql = _options.AllowWriteSql,
        maxSqlLength = _options.MaxSqlLength,
        maxImportBytes = _options.MaxImportBytes,
    };

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
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
        if (!_options.AllowWriteSql)
        {
            throw new ValidationException("Write SQL operations are disabled.");
        }
    }
}
