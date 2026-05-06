namespace WebApi.Contracts.EmployeeProfile;

/// <summary>
/// Employee-facing profile payload returned to the mobile workspace.
/// </summary>
public sealed class EmployeeProfileDto
{
    public int EmployeeId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? RecoveryEmail { get; set; }

    public string? Phone { get; set; }
}
