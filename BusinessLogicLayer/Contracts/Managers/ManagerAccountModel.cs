namespace BusinessLogicLayer.Contracts.Managers;

/// <summary>
/// Business-layer representation of one manager login account.
/// </summary>
public sealed class ManagerAccountModel
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public string? RecoveryEmail { get; set; }

    public DateTimeOffset PasswordUpdatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
