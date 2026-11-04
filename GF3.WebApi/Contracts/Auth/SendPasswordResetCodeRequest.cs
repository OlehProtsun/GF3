using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Auth;

/// <summary>
/// Anonymous password-recovery request keyed by the employee login username.
/// </summary>
public sealed class SendPasswordResetCodeRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
}
