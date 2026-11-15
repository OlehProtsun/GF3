namespace BusinessLogicLayer.Contracts.Employees;

/// <summary>
/// One newly created password-reset challenge before it is delivered to the employee by email.
/// </summary>
public sealed class EmployeePasswordResetChallengeDto
{
    public string Code { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
