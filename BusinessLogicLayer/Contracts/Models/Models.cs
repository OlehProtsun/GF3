using BusinessLogicLayer.Contracts.Enums;

namespace BusinessLogicLayer.Contracts.Models;

/// <summary>
/// Simple key/value configuration entry used by bind-related flows.
/// </summary>
public class BindModel
{
    /// <summary>
    /// Persistent bind identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Availability code that will be inserted by the bind, for example <c>+</c>, <c>-</c>, or a normalized interval.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Keyboard shortcut string associated with the bind.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the bind is active and should be exposed to the editor.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Container aggregate root that groups schedule graphs and presets.
/// </summary>
public class ContainerModel
{
    /// <summary>
    /// Persistent container identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable container name used throughout the planner UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional free-form note attached to the container.
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Employee domain model used by scheduling, availability, and export flows.
/// </summary>
public class EmployeeModel
{
    /// <summary>
    /// Persistent employee identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Employee given name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Employee family name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Optional contact phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Optional contact email address.
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
/// Shop domain model used by schedule graphs and exports.
/// </summary>
public class ShopModel
{
    /// <summary>
    /// Persistent shop identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable shop name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the shop or location.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional description shown in management screens.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Availability group that defines monthly availability for a set of employees.
/// </summary>
public class AvailabilityGroupModel
{
    /// <summary>
    /// Persistent group identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable group name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Calendar year that the group applies to.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Calendar month that the group applies to.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Members that participate in this availability group.
    /// </summary>
    public ICollection<AvailabilityGroupMemberModel> Members { get; set; } = new List<AvailabilityGroupMemberModel>();
}

/// <summary>
/// One employee entry inside an availability group.
/// </summary>
public class AvailabilityGroupMemberModel
{
    /// <summary>
    /// Persistent member identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Group that owns the member row.
    /// </summary>
    public int AvailabilityGroupId { get; set; }

    /// <summary>
    /// Employee represented by the member row.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Stable visual order used by the planner UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Optional expanded employee payload used by richer read models.
    /// </summary>
    public EmployeeModel? Employee { get; set; }

    /// <summary>
    /// Day-level availability entries for this member.
    /// </summary>
    public ICollection<AvailabilityGroupDayModel> Days { get; set; } = new List<AvailabilityGroupDayModel>();
}

/// <summary>
/// One day-level availability entry for an employee inside an availability group.
/// </summary>
public class AvailabilityGroupDayModel
{
    /// <summary>
    /// Persistent day-entry identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Member row that owns the day entry.
    /// </summary>
    public int AvailabilityGroupMemberId { get; set; }

    /// <summary>
    /// Calendar day within the group month.
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Availability mode for the day.
    /// </summary>
    public AvailabilityKind Kind { get; set; }

    /// <summary>
    /// Optional normalized interval string used when <see cref="Kind"/> represents interval-based availability.
    /// </summary>
    public string? IntervalStr { get; set; }
}

/// <summary>
/// Persisted schedule graph together with its generation constraints and related collections.
/// </summary>
public class ScheduleModel
{
    /// <summary>
    /// Persistent schedule identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Container that owns the schedule.
    /// </summary>
    public int ContainerId { get; set; }

    /// <summary>
    /// Optional expanded container payload used by rich read scenarios.
    /// </summary>
    public ContainerModel? Container { get; set; }

    /// <summary>
    /// Shop or location that the schedule belongs to.
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>
    /// Optional expanded shop payload used by read models and exports.
    /// </summary>
    public ShopModel? Shop { get; set; }

    /// <summary>
    /// Human-readable schedule name shown in the planner UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Calendar year that the schedule applies to.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Calendar month that the schedule applies to.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Number of employees required in each shift interval.
    /// </summary>
    public int PeoplePerShift { get; set; }

    /// <summary>
    /// First shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift1Time { get; set; } = string.Empty;

    /// <summary>
    /// Second shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift2Time { get; set; } = string.Empty;

    /// <summary>
    /// Upper bound for employee hours within the month.
    /// </summary>
    public int MaxHoursPerEmpMonth { get; set; }

    /// <summary>
    /// Maximum allowed number of worked days in a row.
    /// </summary>
    public int MaxConsecutiveDays { get; set; }

    /// <summary>
    /// Maximum allowed number of consecutive full-shift days.
    /// </summary>
    public int MaxConsecutiveFull { get; set; }

    /// <summary>
    /// Maximum allowed number of full shifts during the whole month.
    /// </summary>
    public int MaxFullPerMonth { get; set; }

    /// <summary>
    /// Optional free-form planner note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Optional availability group that constrains generation.
    /// </summary>
    public int? AvailabilityGroupId { get; set; }

    /// <summary>
    /// Optional expanded availability group payload used by richer read flows.
    /// </summary>
    public AvailabilityGroupModel? AvailabilityGroup { get; set; }

    /// <summary>
    /// Employees explicitly assigned to the graph.
    /// </summary>
    public ICollection<ScheduleEmployeeModel> Employees { get; set; } = new List<ScheduleEmployeeModel>();

    /// <summary>
    /// Concrete generated or manually edited slots of the graph.
    /// </summary>
    public ICollection<ScheduleSlotModel> Slots { get; set; } = new List<ScheduleSlotModel>();

    /// <summary>
    /// Optional visual overrides for exported or edited matrix cells.
    /// </summary>
    public ICollection<ScheduleCellStyleModel> CellStyles { get; set; } = new List<ScheduleCellStyleModel>();
}

/// <summary>
/// Reusable schedule template saved under a container.
/// </summary>
public class SchedulePresetModel
{
    /// <summary>
    /// Persistent preset identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Container that owns the preset.
    /// </summary>
    public int ContainerId { get; set; }

    /// <summary>
    /// Human-readable preset name used in management screens.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default schedule name suggested when the preset is materialized.
    /// </summary>
    public string ScheduleName { get; set; } = string.Empty;

    /// <summary>
    /// Shop or location that the preset targets.
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>
    /// Default calendar year used when creating a schedule from the preset.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Default calendar month used when creating a schedule from the preset.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Number of employees required in each shift interval.
    /// </summary>
    public int PeoplePerShift { get; set; }

    /// <summary>
    /// First shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift1Time { get; set; } = string.Empty;

    /// <summary>
    /// Second shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift2Time { get; set; } = string.Empty;

    /// <summary>
    /// Upper bound for employee hours within the month.
    /// </summary>
    public int MaxHoursPerEmpMonth { get; set; }

    /// <summary>
    /// Maximum allowed number of worked days in a row.
    /// </summary>
    public int MaxConsecutiveDays { get; set; }

    /// <summary>
    /// Maximum allowed number of consecutive full-shift days.
    /// </summary>
    public int MaxConsecutiveFull { get; set; }

    /// <summary>
    /// Maximum allowed number of full shifts during the whole month.
    /// </summary>
    public int MaxFullPerMonth { get; set; }

    /// <summary>
    /// Optional availability group used as a default generation constraint.
    /// </summary>
    public int? AvailabilityGroupId { get; set; }

    /// <summary>
    /// Employees included in the preset.
    /// </summary>
    public ICollection<SchedulePresetEmployeeModel> Employees { get; set; } = new List<SchedulePresetEmployeeModel>();
}

/// <summary>
/// One employee assignment inside a schedule preset.
/// </summary>
public class SchedulePresetEmployeeModel
{
    /// <summary>
    /// Persistent preset-employee identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Preset that owns the employee assignment.
    /// </summary>
    public int SchedulePresetId { get; set; }

    /// <summary>
    /// Employee included in the preset.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Minimum monthly hours target applied when the preset is materialized.
    /// </summary>
    public int MinHoursMonth { get; set; }
}

/// <summary>
/// One employee assignment inside a persisted schedule graph.
/// </summary>
public class ScheduleEmployeeModel
{
    /// <summary>
    /// Persistent graph-employee identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Schedule that owns the employee assignment.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Employee included in the graph.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional expanded employee payload used by richer read models.
    /// </summary>
    public EmployeeModel? Employee { get; set; }

    /// <summary>
    /// Optional minimum monthly hours constraint for this employee in the current graph.
    /// </summary>
    public int? MinHoursMonth { get; set; }

    /// <summary>
    /// Stable row order used by the planner UI.
    /// </summary>
    public int DisplayOrder { get; set; }
}

/// <summary>
/// One concrete slot inside a schedule graph.
/// </summary>
public class ScheduleSlotModel
{
    /// <summary>
    /// Persistent slot identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Schedule that owns the slot.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Calendar day within the schedule month.
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Position index inside the same shift interval when multiple employees are required.
    /// </summary>
    public int SlotNo { get; set; }

    /// <summary>
    /// Optional employee assigned to the slot.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Optional expanded employee payload used by richer read models.
    /// </summary>
    public EmployeeModel? Employee { get; set; }

    /// <summary>
    /// Current staffing status of the slot.
    /// </summary>
    public SlotStatus Status { get; set; } = SlotStatus.UNFURNISHED;

    /// <summary>
    /// Slot start time in <c>HH:mm</c> format.
    /// </summary>
    public string FromTime { get; set; } = string.Empty;

    /// <summary>
    /// Slot end time in <c>HH:mm</c> format.
    /// </summary>
    public string ToTime { get; set; } = string.Empty;
}

/// <summary>
/// Optional visual override for one cell in the schedule matrix.
/// </summary>
public class ScheduleCellStyleModel
{
    /// <summary>
    /// Persistent style identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Schedule that owns the styled cell.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Calendar day that the style applies to.
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Employee row that the style applies to.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional ARGB background color override.
    /// </summary>
    public int? BackgroundColorArgb { get; set; }

    /// <summary>
    /// Optional ARGB text color override.
    /// </summary>
    public int? TextColorArgb { get; set; }
}
