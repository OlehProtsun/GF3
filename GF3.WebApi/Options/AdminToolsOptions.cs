namespace WebApi.Options;

public sealed class AdminToolsOptions
{
    public bool Enabled { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool AllowWriteSql { get; set; }
    public int MaxSqlLength { get; set; } = 20_000;
    public int MaxImportBytes { get; set; } = 2_000_000;
}
