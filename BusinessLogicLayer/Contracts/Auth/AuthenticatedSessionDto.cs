namespace BusinessLogicLayer.Contracts.Auth;

/// <summary>
/// Authenticated session returned after a successful login or session lookup.
/// </summary>
public sealed class AuthenticatedSessionDto
{
    public string Role { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int? EmployeeId { get; set; }
}
