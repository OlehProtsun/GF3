using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Persistence model for an employee who can participate in schedules and availability groups.
/// The entity intentionally stays small because scheduling-specific settings live in the join tables.
/// </summary>
[Table("employee")]
public class EmployeeModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Given name displayed throughout the UI and export documents.
    /// </summary>
    [Required]
    [Column("first_name")]
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Family name displayed throughout the UI and export documents.
    /// </summary>
    [Required]
    [Column("last_name")]
    public string LastName { get; set; } = null!;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Optional login account that lets the employee sign into the application.
    /// Authentication data is kept in a separate table so scheduling/profile data stays isolated.
    /// </summary>
    public EmployeeAccountModel? Account { get; set; }

    /// <summary>
    /// Assignments that enroll the employee into concrete schedules.
    /// </summary>
    public ICollection<ScheduleEmployeeModel> ScheduleEmployees { get; set; } = new List<ScheduleEmployeeModel>();

    /// <summary>
    /// Concrete shift slots currently assigned to the employee.
    /// </summary>
    public ICollection<ScheduleSlotModel> ScheduleSlots { get; set; } = new List<ScheduleSlotModel>();
}
