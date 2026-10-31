using System.Reflection;
using BusinessLogicLayer.Contracts.Enums;
using Contracts = BusinessLogicLayer.Contracts.Models;
using Dal = DataAccessLayer.Models;
using DalEnums = DataAccessLayer.Models.Enums;

namespace GF3.Tests;

public sealed class ModelMapperCoverageTests
{
    private static readonly Type MapperType = typeof(BusinessLogicLayer.Services.EmployeeService).Assembly
        .GetType("BusinessLogicLayer.Mappers.ModelMapper", throwOnError: true)!;

    [Fact]
    public void ToContract_MapsFullScheduleAggregateWithNestedCollections()
    {
        var visibleFrom = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        var dalSchedule = new Dal.ScheduleModel
        {
            Id = 50,
            ContainerId = 5,
            Container = new Dal.ContainerModel { Id = 5, Name = "Container", Note = "Container note" },
            ShopId = 7,
            Shop = new Dal.ShopModel { Id = 7, Name = "Central", Address = "Main", Description = "Flagship" },
            Name = "May graph",
            Year = 2026,
            Month = 5,
            PublicationStatus = DalEnums.SchedulePublicationStatus.Public,
            PeoplePerShift = 2,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            Note = "Planner note",
            AvailabilityGroupId = 12,
            AvailabilityGroup = new Dal.AvailabilityGroupModel
            {
                Id = 12,
                Name = "May availability",
                Year = 2026,
                Month = 5,
                PublicationStatus = DalEnums.AvailabilityPublicationStatus.Public,
                VisibleFromUtc = visibleFrom,
                Members =
                [
                    new Dal.AvailabilityGroupMemberModel
                    {
                        Id = 80,
                        AvailabilityGroupId = 12,
                        EmployeeId = 30,
                        DisplayOrder = 2,
                        Employee = new Dal.EmployeeModel { Id = 30, FirstName = "Ada", LastName = "Lovelace" },
                        EmployeeLastModifiedAtUtc = visibleFrom.AddHours(2),
                        Days =
                        [
                            new Dal.AvailabilityGroupDayModel
                            {
                                Id = 90,
                                AvailabilityGroupMemberId = 80,
                                DayOfMonth = 10,
                                Kind = DalEnums.AvailabilityKind.INT,
                                IntervalStr = "09:00 - 13:00",
                            },
                        ],
                    },
                ],
            },
            Employees =
            [
                new Dal.ScheduleEmployeeModel
                {
                    Id = 60,
                    ScheduleId = 50,
                    EmployeeId = 30,
                    MinHoursMonth = 80,
                    DisplayOrder = 1,
                    Employee = new Dal.EmployeeModel
                    {
                        Id = 30,
                        FirstName = "Ada",
                        LastName = "Lovelace",
                        Phone = "123",
                        Email = "ada@example.com",
                    },
                },
            ],
            Slots =
            [
                new Dal.ScheduleSlotModel
                {
                    Id = 70,
                    ScheduleId = 50,
                    DayOfMonth = 10,
                    SlotNo = 1,
                    EmployeeId = 30,
                    Employee = new Dal.EmployeeModel { Id = 30, FirstName = "Ada", LastName = "Lovelace" },
                    Status = DalEnums.SlotStatus.ASSIGNED,
                    FromTime = "08:00",
                    ToTime = "16:00",
                },
            ],
            CellStyles =
            [
                new Dal.ScheduleCellStyleModel
                {
                    Id = 75,
                    ScheduleId = 50,
                    DayOfMonth = 10,
                    EmployeeId = 30,
                    BackgroundColorArgb = -65536,
                    TextColorArgb = -16777216,
                },
            ],
        };

        var contract = InvokeMap<Dal.ScheduleModel, Contracts.ScheduleModel>("ToContract", dalSchedule);

        Assert.Equal(50, contract.Id);
        Assert.Equal(SchedulePublicationStatus.Public, contract.PublicationStatus);
        Assert.Equal("Container", contract.Container!.Name);
        Assert.Equal("Central", contract.Shop!.Name);
        Assert.Equal("May availability", contract.AvailabilityGroup!.Name);
        Assert.Equal(AvailabilityPublicationStatus.Public, contract.AvailabilityGroup.PublicationStatus);
        Assert.Equal(visibleFrom, contract.AvailabilityGroup.VisibleFromUtc);

        var member = Assert.Single(contract.AvailabilityGroup.Members);
        Assert.Equal("Ada", member.Employee!.FirstName);
        Assert.Equal(visibleFrom.AddHours(2), member.EmployeeLastModifiedAtUtc);
        Assert.Equal(AvailabilityKind.INT, Assert.Single(member.Days).Kind);

        var employee = Assert.Single(contract.Employees);
        Assert.Equal("ada@example.com", employee.Employee!.Email);
        Assert.Equal(80, employee.MinHoursMonth);

        var slot = Assert.Single(contract.Slots);
        Assert.Equal(SlotStatus.ASSIGNED, slot.Status);
        Assert.Equal("Ada", slot.Employee!.FirstName);

        var style = Assert.Single(contract.CellStyles);
        Assert.Equal(-65536, style.BackgroundColorArgb);
        Assert.Equal(-16777216, style.TextColorArgb);
    }

    [Fact]
    public void ToDal_MapsScheduleAndPresetContractsWithoutLeakingReadOnlyNavigationGraphs()
    {
        var contractSchedule = new Contracts.ScheduleModel
        {
            Id = 10,
            ContainerId = 1,
            Container = new Contracts.ContainerModel { Id = 1, Name = "Ignored container" },
            ShopId = 2,
            Shop = new Contracts.ShopModel { Id = 2, Name = "Ignored shop" },
            Name = "May graph",
            Year = 2026,
            Month = 5,
            PublicationStatus = SchedulePublicationStatus.Public,
            PeoplePerShift = 2,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            Note = "Planner note",
            AvailabilityGroupId = 4,
            Employees =
            [
                new Contracts.ScheduleEmployeeModel { Id = 99, ScheduleId = 10, EmployeeId = 5 },
            ],
            Slots =
            [
                new Contracts.ScheduleSlotModel { Id = 100, ScheduleId = 10, DayOfMonth = 1, SlotNo = 1, EmployeeId = 5 },
            ],
        };
        var contractPreset = new Contracts.SchedulePresetModel
        {
            Id = 20,
            ContainerId = 1,
            Name = "Default preset",
            ScheduleName = "Generated graph",
            ShopId = 2,
            Year = 2026,
            Month = 5,
            PeoplePerShift = 1,
            Shift1Time = "09:00 - 17:00",
            Shift2Time = "",
            MaxHoursPerEmpMonth = 120,
            MaxConsecutiveDays = 4,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 8,
            AvailabilityGroupId = null,
            Employees =
            [
                new Contracts.SchedulePresetEmployeeModel
                {
                    Id = 30,
                    SchedulePresetId = 20,
                    EmployeeId = 5,
                    MinHoursMonth = 90,
                },
            ],
        };

        var dalSchedule = InvokeMap<Contracts.ScheduleModel, Dal.ScheduleModel>("ToDal", contractSchedule);
        var dalPreset = InvokeMap<Contracts.SchedulePresetModel, Dal.SchedulePresetModel>("ToDal", contractPreset);

        Assert.Equal(10, dalSchedule.Id);
        Assert.Equal(DalEnums.SchedulePublicationStatus.Public, dalSchedule.PublicationStatus);
        Assert.Equal(4, dalSchedule.AvailabilityGroupId);
        Assert.Equal("Planner note", dalSchedule.Note);
        Assert.Empty(dalSchedule.Employees);
        Assert.Empty(dalSchedule.Slots);
        Assert.Empty(dalSchedule.CellStyles);

        Assert.Equal(20, dalPreset.Id);
        Assert.Null(dalPreset.AvailabilityGroupId);
        var presetEmployee = Assert.Single(dalPreset.Employees);
        Assert.Equal(30, presetEmployee.Id);
        Assert.Equal(20, presetEmployee.SchedulePresetId);
        Assert.Equal(5, presetEmployee.EmployeeId);
        Assert.Equal(90, presetEmployee.MinHoursMonth);
    }

    private static TResult InvokeMap<TSource, TResult>(string methodName, TSource source)
    {
        var method = MapperType
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(candidate =>
            {
                var parameters = candidate.GetParameters();
                return candidate.Name == methodName &&
                       candidate.ReturnType == typeof(TResult) &&
                       parameters.Length == 1 &&
                       parameters[0].ParameterType == typeof(TSource);
            });

        return (TResult)method.Invoke(null, [source])!;
    }
}
