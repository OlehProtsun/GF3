namespace WebApi.Contracts.AdminDb;

/// <summary>
/// Request payload used by admin endpoints that execute ad-hoc SQL.
/// </summary>
public sealed class AdminDbSqlRequest
{
    /// <summary>
    /// Raw SQL text that should be executed by the admin endpoint.
    /// Validation and command-type restrictions are applied later by the service layer.
    /// </summary>
    public string Sql { get; init; } = string.Empty;
}
