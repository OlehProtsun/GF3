using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using DalEnums = DataAccessLayer.Models.Enums;

namespace GF3.Tests.Infrastructure;

internal static class TestDataFactory
{
    public static DataAccessLayer.Models.ContainerModel CreateDalContainer(
        string name = "Main Container",
        string? note = "Container note")
        => new()
        {
            Name = name,
            Note = note,
        };

    public static DataAccessLayer.Models.EmployeeModel CreateDalEmployee(
        string firstName = "John",
        string lastName = "Smith",
        string? phone = "123456789",
        string? email = "john.smith@example.com")
        => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
        };

    public static DataAccessLayer.Models.ShopModel CreateDalShop(
        string name = "Central Shop",
        string address = "Main Street 1",
        string? description = "Flagship")
        => new()
        {
            Name = name,
            Address = address,
            Description = description,
        };

    public static DataAccessLayer.Models.ScheduleModel CreateDalSchedule(
        int containerId,
        int shopId,
        string name = "April Schedule",
        int year = 2026,
        int month = 4,
        int? availabilityGroupId = null)
        => new()
        {
            ContainerId = containerId,
            ShopId = shopId,
            Name = name,
            Year = year,
            Month = month,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            AvailabilityGroupId = availabilityGroupId,
        };

    public static DataAccessLayer.Models.ScheduleSlotModel CreateDalSlot(
        int scheduleId,
        int dayOfMonth,
        int slotNo,
        int? employeeId,
        string fromTime,
        string toTime)
        => new()
        {
            ScheduleId = scheduleId,
            DayOfMonth = dayOfMonth,
            SlotNo = slotNo,
            EmployeeId = employeeId,
            Status = employeeId.HasValue ? DalEnums.SlotStatus.ASSIGNED : DalEnums.SlotStatus.UNFURNISHED,
            FromTime = fromTime,
            ToTime = toTime,
        };

    public static EmployeeModel CreateEmployeeModel(
        int id = 0,
        string firstName = "John",
        string lastName = "Smith",
        string? phone = "123456789",
        string? email = "john.smith@example.com")
        => new()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
        };

    public static ShopModel CreateShopModel(
        int id = 0,
        string name = "Central Shop",
        string address = "Main Street 1",
        string? description = "Flagship")
        => new()
        {
            Id = id,
            Name = name,
            Address = address,
            Description = description,
        };

    public static BindModel CreateBindModel(
        int id = 0,
        string key = "A",
        string value = "+",
        bool isActive = true)
        => new()
        {
            Id = id,
            Key = key,
            Value = value,
            IsActive = isActive,
        };

    public static AvailabilityGroupModel CreateAvailabilityGroupModel(
        int id = 0,
        string name = "April Availability",
        int year = 2026,
        int month = 4)
        => new()
        {
            Id = id,
            Name = name,
            Year = year,
            Month = month,
        };

    public static AvailabilityGroupDayModel CreateAvailabilityDayModel(
        int dayOfMonth,
        AvailabilityKind kind = AvailabilityKind.ANY,
        string? interval = null,
        int memberId = 0,
        int id = 0)
        => new()
        {
            Id = id,
            AvailabilityGroupMemberId = memberId,
            DayOfMonth = dayOfMonth,
            Kind = kind,
            IntervalStr = interval,
        };

    public static ScheduleModel CreateScheduleModel(
        int id = 0,
        int containerId = 1,
        int shopId = 1,
        string name = "April Schedule",
        int year = 2026,
        int month = 4,
        int? availabilityGroupId = 1)
        => new()
        {
            Id = id,
            ContainerId = containerId,
            ShopId = shopId,
            Name = name,
            Year = year,
            Month = month,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            AvailabilityGroupId = availabilityGroupId,
        };

    public static ScheduleEmployeeModel CreateScheduleEmployeeModel(
        int employeeId,
        int scheduleId = 0,
        int id = 0,
        int displayOrder = 0,
        int minHoursMonth = 0)
        => new()
        {
            Id = id,
            ScheduleId = scheduleId,
            EmployeeId = employeeId,
            DisplayOrder = displayOrder,
            MinHoursMonth = minHoursMonth,
        };

    public static ScheduleSlotModel CreateScheduleSlotModel(
        int dayOfMonth,
        int slotNo,
        int? employeeId,
        string fromTime,
        string toTime,
        int scheduleId = 0,
        int id = 0)
        => new()
        {
            Id = id,
            ScheduleId = scheduleId,
            DayOfMonth = dayOfMonth,
            SlotNo = slotNo,
            EmployeeId = employeeId,
            Status = employeeId.HasValue ? SlotStatus.ASSIGNED : SlotStatus.UNFURNISHED,
            FromTime = fromTime,
            ToTime = toTime,
        };
}
