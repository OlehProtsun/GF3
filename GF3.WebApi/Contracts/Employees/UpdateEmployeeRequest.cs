using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Employees;

/// <summary>
/// Request payload used to update an existing employee.
/// The route supplies the employee id, while this payload carries the editable fields.
/// </summary>
public sealed class UpdateEmployeeRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(200)]
    public string? Password { get; set; }
}
