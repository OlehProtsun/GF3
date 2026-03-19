using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Employees;

public sealed class AddGraphEmployeeRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    public int? MinHoursMonth { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
