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

/// <summary>
/// Request payload used when the admin UI selects another SQLite database file.
/// </summary>
public sealed class AdminDbSelectDatabaseRequest
{
    /// <summary>
    /// Fully-qualified path to the database file that should become active.
    /// </summary>
    public string DatabasePath { get; init; } = string.Empty;
}
