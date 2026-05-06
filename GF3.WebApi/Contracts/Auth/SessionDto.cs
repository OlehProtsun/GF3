namespace WebApi.Contracts.Auth;

/// <summary>
/// Session payload returned to the frontend after login or session restore.
/// </summary>
public sealed class SessionDto
{
    public string Role { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int? EmployeeId { get; set; }
}
