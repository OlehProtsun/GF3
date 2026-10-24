namespace BusinessLogicLayer.Contracts.Employees;

/// <summary>
/// Result returned after a password-reset code was emailed successfully.
/// </summary>
public sealed class PasswordResetDispatchResult
{
    public string DeliveryHint { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
