using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("employee_pinned_swap")]
public sealed class EmployeePinnedSwapModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("shift_swap_id")]
    public int ShiftSwapId { get; set; }

    [Column("pinned_at_utc")]
    public DateTimeOffset PinnedAtUtc { get; set; }

    public EmployeeModel Employee { get; set; } = null!;

    public ShiftSwapRequestModel ShiftSwap { get; set; } = null!;
}
