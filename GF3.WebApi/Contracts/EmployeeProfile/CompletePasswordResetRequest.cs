namespace WebApi.Contracts.EmployeeProfile;

/// <summary>
/// Confirms an employee password change using the emailed verification code.
/// </summary>
public sealed class CompletePasswordResetRequest
{
    public string Code { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
