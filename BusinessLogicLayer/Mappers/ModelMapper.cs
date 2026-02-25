using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using Dal = DataAccessLayer.Models;
using DalEnums = DataAccessLayer.Models.Enums;

namespace BusinessLogicLayer.Mappers;

internal static class ModelMapper
{
    internal static ContainerModel ToContract(this Dal.ContainerModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Note = model.Note
    };

    internal static Dal.ContainerModel ToDal(this ContainerModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Note = model.Note
    };

    internal static EmployeeModel ToContract(this Dal.EmployeeModel model) => new()
    {
        Id = model.Id,
        FirstName = model.FirstName,
        LastName = model.LastName,
        Phone = model.Phone,
        Email = model.Email
    };

    internal static Dal.EmployeeModel ToDal(this EmployeeModel model) => new()
    {
        Id = model.Id,
        FirstName = model.FirstName,
        LastName = model.LastName,
        Phone = model.Phone,
        Email = model.Email
    };

    internal static ShopModel ToContract(this Dal.ShopModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Address = model.Address,
        Description = model.Description
    };

    internal static Dal.ShopModel ToDal(this ShopModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Address = model.Address,
        Description = model.Description
    };

    internal static BindModel ToContract(this Dal.BindModel model) => new()
    {
        Id = model.Id,
        Key = model.Key,
        Value = model.Value,
        IsActive = model.IsActive
    };

    internal static Dal.BindModel ToDal(this BindModel model) => new()
    {
        Id = model.Id,
        Key = model.Key,
        Value = model.Value,
        IsActive = model.IsActive
    };

    internal static AvailabilityKind ToContract(this DalEnums.AvailabilityKind value) => (AvailabilityKind)(int)value;
    internal static DalEnums.AvailabilityKind ToDal(this AvailabilityKind value) => (DalEnums.AvailabilityKind)(int)value;
    internal static SlotStatus ToContract(this DalEnums.SlotStatus value) => (SlotStatus)(int)value;
    internal static DalEnums.SlotStatus ToDal(this SlotStatus value) => (DalEnums.SlotStatus)(int)value;

    internal static AvailabilityGroupDayModel ToContract(this Dal.AvailabilityGroupDayModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupMemberId = model.AvailabilityGroupMemberId,
        DayOfMonth = model.DayOfMonth,
        Kind = model.Kind.ToContract(),
        IntervalStr = model.IntervalStr
    };

    internal static Dal.AvailabilityGroupDayModel ToDal(this AvailabilityGroupDayModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupMemberId = model.AvailabilityGroupMemberId,
        DayOfMonth = model.DayOfMonth,
        Kind = model.Kind.ToDal(),
        IntervalStr = model.IntervalStr
    };

    internal static AvailabilityGroupMemberModel ToContract(this Dal.AvailabilityGroupMemberModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupId = model.AvailabilityGroupId,
        EmployeeId = model.EmployeeId,
        Employee = model.Employee is null ? null : model.Employee.ToContract(),
        Days = model.Days?.Select(ToContract).ToList() ?? new List<AvailabilityGroupDayModel>()
    };

    internal static AvailabilityGroupModel ToContract(this Dal.AvailabilityGroupModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Year = model.Year,
        Month = model.Month,
        Members = model.Members?.Select(ToContract).ToList() ?? new List<AvailabilityGroupMemberModel>()
    };

    internal static Dal.AvailabilityGroupModel ToDal(this AvailabilityGroupModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Year = model.Year,
        Month = model.Month
    };

    internal static ScheduleEmployeeModel ToContract(this Dal.ScheduleEmployeeModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        EmployeeId = model.EmployeeId,
        Employee = model.Employee is null ? null : model.Employee.ToContract(),
        MinHoursMonth = model.MinHoursMonth
    };

    internal static Dal.ScheduleEmployeeModel ToDal(this ScheduleEmployeeModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        EmployeeId = model.EmployeeId,
        MinHoursMonth = model.MinHoursMonth
    };

    internal static ScheduleSlotModel ToContract(this Dal.ScheduleSlotModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        DayOfMonth = model.DayOfMonth,
        SlotNo = model.SlotNo,
        EmployeeId = model.EmployeeId,
        Employee = model.Employee is null ? null : model.Employee.ToContract(),
        Status = model.Status.ToContract(),
        FromTime = model.FromTime,
        ToTime = model.ToTime
    };

    internal static Dal.ScheduleSlotModel ToDal(this ScheduleSlotModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        DayOfMonth = model.DayOfMonth,
        SlotNo = model.SlotNo,
        EmployeeId = model.EmployeeId,
        Status = model.Status.ToDal(),
        FromTime = model.FromTime,
        ToTime = model.ToTime
    };

    internal static ScheduleCellStyleModel ToContract(this Dal.ScheduleCellStyleModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        DayOfMonth = model.DayOfMonth,
        EmployeeId = model.EmployeeId,
        BackgroundColorArgb = model.BackgroundColorArgb,
        TextColorArgb = model.TextColorArgb
    };

    internal static Dal.ScheduleCellStyleModel ToDal(this ScheduleCellStyleModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        DayOfMonth = model.DayOfMonth,
        EmployeeId = model.EmployeeId,
        BackgroundColorArgb = model.BackgroundColorArgb,
        TextColorArgb = model.TextColorArgb
    };

    internal static ScheduleModel ToContract(this Dal.ScheduleModel model) => new()
    {
        Id = model.Id,
        ContainerId = model.ContainerId,
        Container = model.Container is null ? null : model.Container.ToContract(),
        ShopId = model.ShopId,
        Shop = model.Shop is null ? null : model.Shop.ToContract(),
        Name = model.Name,
        Year = model.Year,
        Month = model.Month,
        PeoplePerShift = model.PeoplePerShift,
        Shift1Time = model.Shift1Time,
        Shift2Time = model.Shift2Time,
        MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = model.MaxConsecutiveDays,
        MaxConsecutiveFull = model.MaxConsecutiveFull,
        MaxFullPerMonth = model.MaxFullPerMonth,
        Note = model.Note,
        AvailabilityGroupId = model.AvailabilityGroupId,
        AvailabilityGroup = model.AvailabilityGroup is null ? null : model.AvailabilityGroup.ToContract(),
        Employees = model.Employees?.Select(ToContract).ToList() ?? new List<ScheduleEmployeeModel>(),
        Slots = model.Slots?.Select(ToContract).ToList() ?? new List<ScheduleSlotModel>(),
        CellStyles = model.CellStyles?.Select(ToContract).ToList() ?? new List<ScheduleCellStyleModel>()
    };

    internal static Dal.ScheduleModel ToDal(this ScheduleModel model) => new()
    {
        Id = model.Id,
        ContainerId = model.ContainerId,
        ShopId = model.ShopId,
        Name = model.Name,
        Year = model.Year,
        Month = model.Month,
        PeoplePerShift = model.PeoplePerShift,
        Shift1Time = model.Shift1Time,
        Shift2Time = model.Shift2Time,
        MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = model.MaxConsecutiveDays,
        MaxConsecutiveFull = model.MaxConsecutiveFull,
        MaxFullPerMonth = model.MaxFullPerMonth,
        Note = model.Note,
        AvailabilityGroupId = model.AvailabilityGroupId
    };
}
