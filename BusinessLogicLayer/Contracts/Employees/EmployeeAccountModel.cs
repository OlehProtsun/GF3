namespace BusinessLogicLayer.Contracts.Employees;

/// <summary>
/// Business-layer representation of one employee login account.
/// </summary>
public sealed class EmployeeAccountModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTimeOffset PasswordUpdatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public DateTimeOffset? LastSeenAtUtc { get; set; }

    public int SessionVersion { get; set; }
}
