namespace WebApi.Options;

/// <summary>
/// Configuration options that control access to admin database tooling.
/// </summary>
public sealed class AdminToolsOptions
{
    /// <summary>
    /// Enables or disables the admin database endpoints completely.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Allows admin database endpoints to be called from remote machines.
    /// Keeping this disabled by default preserves the safer local-only posture unless the
    /// launcher explicitly opts into LAN access.
    /// </summary>
    public bool AllowRemoteAccess { get; set; }

    /// <summary>
    /// Indicates whether write-oriented SQL operations are allowed.
    /// </summary>
    public bool AllowWriteSql { get; set; }

    /// <summary>
    /// Maximum allowed SQL command length accepted by the admin endpoints.
    /// </summary>
    public int MaxSqlLength { get; set; } = 20_000;

    /// <summary>
    /// Maximum allowed upload size for SQL import operations.
    /// </summary>
    public int MaxImportBytes { get; set; } = 2_000_000;
}
