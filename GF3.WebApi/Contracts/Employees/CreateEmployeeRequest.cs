using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Employees;

/// <summary>
/// Request payload used to create a new employee.
/// Validation attributes document the API contract and are enforced before the request reaches
/// the business layer.
/// </summary>
public sealed class CreateEmployeeRequest
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
}
