using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Employees;

public sealed class UpdateGraphEmployeeRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    public int? MinHoursMonth { get; set; }
}
