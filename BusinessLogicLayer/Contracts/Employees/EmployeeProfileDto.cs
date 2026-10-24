namespace BusinessLogicLayer.Contracts.Employees;

/// <summary>
/// Employee-facing profile payload used by self-service account screens.
/// </summary>
public sealed class EmployeeProfileDto
{
    public int EmployeeId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? RecoveryEmail { get; set; }

    public string? Phone { get; set; }
}
