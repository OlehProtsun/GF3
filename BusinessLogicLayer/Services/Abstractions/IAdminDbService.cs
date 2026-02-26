using BusinessLogicLayer.Contracts.Database;

namespace BusinessLogicLayer.Services.Abstractions;

public interface IAdminDbService
{
    Task<AdminDbMetadataDto> GetMetadataAsync(CancellationToken ct = default);
    Task<string> GetDbHashAsync(CancellationToken ct = default);
    Task<AdminDbQueryResultDto> ExecuteQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default);
    Task<int> ExecuteNonQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default);
    Task<AdminDbImportResultDto> ImportSqlAsync(byte[] fileBytes, int maxImportBytes, CancellationToken ct = default);
}
