namespace WebApi.Contracts.EmployeeProfile;

/// <summary>
/// Self-service update payload for the employee profile.
/// </summary>
public sealed class UpdateEmployeeProfileRequest
{
    public string? RecoveryEmail { get; set; }

    public string? Phone { get; set; }
}
