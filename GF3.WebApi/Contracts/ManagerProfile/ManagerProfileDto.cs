namespace WebApi.Contracts.ManagerProfile;

public sealed class ManagerProfileDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? RecoveryEmail { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public bool IsOnline { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
