namespace WebApi.Contracts.EmployeeProfile;

/// <summary>
/// Response returned after the password-reset code email was sent.
/// </summary>
public sealed class PasswordResetCodeDispatchDto
{
    public string DeliveryHint { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
