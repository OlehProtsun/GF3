using BusinessLogicLayer.Contracts.Enums;

namespace BusinessLogicLayer.Contracts.Models;

public class BindModel
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ContainerModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class EmployeeModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class ShopModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AvailabilityGroupModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public ICollection<AvailabilityGroupMemberModel> Members { get; set; } = new List<AvailabilityGroupMemberModel>();
}

public class AvailabilityGroupMemberModel
{
    public int Id { get; set; }
    public int AvailabilityGroupId { get; set; }
    public int EmployeeId { get; set; }
    public EmployeeModel? Employee { get; set; }
    public ICollection<AvailabilityGroupDayModel> Days { get; set; } = new List<AvailabilityGroupDayModel>();
}

public class AvailabilityGroupDayModel
{
    public int Id { get; set; }
    public int AvailabilityGroupMemberId { get; set; }
    public int DayOfMonth { get; set; }
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
}

public class ScheduleModel
{
    public int Id { get; set; }
    public int ContainerId { get; set; }
    public ContainerModel? Container { get; set; }
    public int ShopId { get; set; }
    public ShopModel? Shop { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int PeoplePerShift { get; set; }
    public string Shift1Time { get; set; } = string.Empty;
    public string Shift2Time { get; set; } = string.Empty;
    public int MaxHoursPerEmpMonth { get; set; }
    public int MaxConsecutiveDays { get; set; }
    public int MaxConsecutiveFull { get; set; }
    public int MaxFullPerMonth { get; set; }
    public string? Note { get; set; }
    public int? AvailabilityGroupId { get; set; }
    public AvailabilityGroupModel? AvailabilityGroup { get; set; }
    public ICollection<ScheduleEmployeeModel> Employees { get; set; } = new List<ScheduleEmployeeModel>();
    public ICollection<ScheduleSlotModel> Slots { get; set; } = new List<ScheduleSlotModel>();
    public ICollection<ScheduleCellStyleModel> CellStyles { get; set; } = new List<ScheduleCellStyleModel>();
}

public class ScheduleEmployeeModel
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public EmployeeModel? Employee { get; set; }
    public int? MinHoursMonth { get; set; }
}

public class ScheduleSlotModel
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int DayOfMonth { get; set; }
    public int SlotNo { get; set; }
    public int? EmployeeId { get; set; }
    public EmployeeModel? Employee { get; set; }
    public SlotStatus Status { get; set; } = SlotStatus.UNFURNISHED;
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
}

public class ScheduleCellStyleModel
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int DayOfMonth { get; set; }
    public int EmployeeId { get; set; }
    public int? BackgroundColorArgb { get; set; }
    public int? TextColorArgb { get; set; }
}
