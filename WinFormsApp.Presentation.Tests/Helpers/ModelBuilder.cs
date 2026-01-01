using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal static class ModelBuilder
{
    public static EmployeeModel Employee(int id = 1, string firstName = "Alex", string lastName = "River")
        => new()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = "alex@example.com",
            Phone = "+1234567"
        };

    public static ContainerModel Container(int id = 1, string name = "Container A")
        => new()
        {
            Id = id,
            Name = name,
            Note = "Notes"
        };

    public static ScheduleModel Schedule(int id = 1, int containerId = 1, string name = "Schedule A")
        => new()
        {
            Id = id,
            ContainerId = containerId,
            Name = name,
            Year = 2024,
            Month = 1,
            PeoplePerShift = 2,
            Shift1Time = "09:00 - 17:00",
            Shift2Time = "18:00 - 22:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10
        };

    public static AvailabilityGroupModel AvailabilityGroup(int id = 1, string name = "Group A", int year = 2024, int month = 1)
        => new()
        {
            Id = id,
            Name = name,
            Year = year,
            Month = month
        };

    public static AvailabilityGroupMemberModel AvailabilityMember(int id = 1, int groupId = 1, int employeeId = 1)
        => new()
        {
            Id = id,
            AvailabilityGroupId = groupId,
            EmployeeId = employeeId,
            Employee = Employee(employeeId, $"Emp{employeeId}", "Test")
        };

    public static AvailabilityGroupDayModel AvailabilityDay(int memberId, int day, AvailabilityKind kind, string? interval = null)
        => new()
        {
            AvailabilityGroupMemberId = memberId,
            DayOfMonth = day,
            Kind = kind,
            IntervalStr = interval
        };

    public static BindModel Bind(int id = 1, string key = "F1", string value = "+")
        => new()
        {
            Id = id,
            Key = key,
            Value = value,
            IsActive = true
        };

    public static ScheduleEmployeeModel ScheduleEmployee(int employeeId = 1, int? minHours = null)
        => new()
        {
            EmployeeId = employeeId,
            Employee = Employee(employeeId, $"Emp{employeeId}", "Test"),
            MinHoursMonth = minHours
        };

    public static ScheduleSlotModel ScheduleSlot(int day = 1, int slotNo = 1, string from = "09:00", string to = "17:00")
        => new()
        {
            DayOfMonth = day,
            SlotNo = slotNo,
            FromTime = from,
            ToTime = to
        };
}
