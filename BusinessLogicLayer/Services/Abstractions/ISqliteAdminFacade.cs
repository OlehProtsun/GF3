using BusinessLogicLayer.Contracts.Database;

namespace BusinessLogicLayer.Services.Abstractions;

public interface ISqliteAdminFacade
{
    string DatabasePath { get; }
    Task<SqlExecutionResultDto> ExecuteSqlAsync(string sql, CancellationToken ct);
    Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct);
    Task<DatabaseInfoDto> GetDatabaseInfoAsync(CancellationToken ct);
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct);
}
