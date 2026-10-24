using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Auth;

/// <summary>
/// Login payload used by both manager and employee sign-in.
/// </summary>
public sealed class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
