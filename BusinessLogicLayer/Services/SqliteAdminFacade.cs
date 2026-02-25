using BusinessLogicLayer.Contracts.Database;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;

namespace BusinessLogicLayer.Services;

public sealed class SqliteAdminFacade : ISqliteAdminFacade
{
    private readonly ISqliteAdminService _service;

    public SqliteAdminFacade(ISqliteAdminService service)
    {
        _service = service;
    }

    public string DatabasePath => _service.DatabasePath;

    public async Task<SqlExecutionResultDto> ExecuteSqlAsync(string sql, CancellationToken ct)
    {
        var result = await _service.ExecuteSqlAsync(sql, ct).ConfigureAwait(false);
        return new SqlExecutionResultDto
        {
            IsSelect = result.IsSelect,
            ResultTable = result.ResultTable,
            AffectedRows = result.AffectedRows,
            Message = result.Message
        };
    }

    public Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct)
        => _service.ImportSqlScriptAsync(sqlScript, ct);

    public async Task<DatabaseInfoDto> GetDatabaseInfoAsync(CancellationToken ct)
    {
        var result = await _service.GetDatabaseInfoAsync(ct).ConfigureAwait(false);
        return new DatabaseInfoDto
        {
            DatabasePath = result.DatabasePath,
            FileSizeBytes = result.FileSizeBytes,
            LastModifiedUtc = result.LastModifiedUtc,
            UserVersion = result.UserVersion,
            Tables = result.Tables
        };
    }

    public Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
        => _service.ComputeFileHashAsync(filePath, ct);
}
