namespace BusinessLogicLayer.Contracts.Database;

public sealed class AdminDbMetadataDto
{
    public string SqliteVersion { get; init; } = string.Empty;
    public IReadOnlyList<AdminDbObjectDto> Objects { get; init; } = Array.Empty<AdminDbObjectDto>();
}

public sealed class AdminDbObjectDto
{
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Sql { get; init; } = string.Empty;
}

public sealed class AdminDbQueryResultDto
{
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = Array.Empty<IReadOnlyList<object?>>();
    public int RowCount { get; init; }
}

public sealed class AdminDbImportResultDto
{
    public int StatementsExecuted { get; init; }
    public int? FailedStatementIndex { get; init; }
}
