namespace WebApi.Contracts.AdminDb;

public sealed class AdminDbSqlRequest
{
    public string Sql { get; init; } = string.Empty;
}
