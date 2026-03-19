using System.Data;

namespace BusinessLogicLayer.Contracts.Database;

public sealed class SqlExecutionResultDto
{
    public bool IsSelect { get; init; }
    public DataTable? ResultTable { get; init; }
    public int AffectedRows { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class DatabaseInfoDto
{
    public string DatabasePath { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime LastModifiedUtc { get; init; }
    public int UserVersion { get; init; }
    public IReadOnlyList<string> Tables { get; init; } = Array.Empty<string>();
}
