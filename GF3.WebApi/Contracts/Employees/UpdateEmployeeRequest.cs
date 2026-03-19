using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Employees;

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
}
