using System.Text.Json;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;

namespace GF3.Tests.Infrastructure;

internal enum AvailabilityPattern
{
    AllAvailable, AllUnavailable, Alternating, MorningOnly, EveningOnly,
    MixedPartial, Scarce, ExplicitFullDay, InvalidInterval
}

internal sealed record GenerationScenario(
    string ScenarioId, ScheduleModel Schedule, IReadOnlyList<ScheduleEmployeeModel> Employees,
    IReadOnlyList<AvailabilityGroupModel> AvailabilityGroups,
    IReadOnlyList<ScheduleSlotModel>? ExternalSlots = null);

internal static class ScheduleGenerationScenarioFactory
{
    public static GenerationScenario CreateAllAvailable(int year, int month, int employeeCount, int peoplePerShift, int seed)
    {
        var schedule = TestDataFactory.CreateScheduleModel(year: year, month: month, availabilityGroupId: null);
        schedule.PeoplePerShift = peoplePerShift;
        schedule.MaxHoursPerEmpMonth = 400;
        schedule.MaxConsecutiveDays = schedule.MaxConsecutiveFull = schedule.MaxFullPerMonth = 31;
        return new($"available-{year}-{month}-n{employeeCount}-p{peoplePerShift}-seed{seed}", schedule,
            Enumerable.Range(1, employeeCount).Select(id => TestDataFactory.CreateScheduleEmployeeModel(id, displayOrder: id)).ToArray(), []);
    }

    public static GenerationScenario CreateAvailabilityPattern(int year, int month, int employeeCount,
        int peoplePerShift, AvailabilityPattern pattern, int seed)
    {
        var scenario = CreateAllAvailable(year, month, employeeCount, peoplePerShift, seed);
        var random = new Random(seed);
        var group = TestDataFactory.CreateAvailabilityGroupModel(1, "synthetic", year, month);
        foreach (var employee in scenario.Employees)
        {
            var member = new AvailabilityGroupMemberModel { EmployeeId = employee.EmployeeId, DisplayOrder = employee.DisplayOrder };
            for (var day = 1; day <= DateTime.DaysInMonth(year, month); day++)
            {
                var kind = AvailabilityKind.ANY;
                string? interval = null;
                switch (pattern)
                {
                    case AvailabilityPattern.AllUnavailable: kind = AvailabilityKind.NONE; break;
                    case AvailabilityPattern.Alternating:
                        kind = (day + employee.EmployeeId) % 2 == 0 ? AvailabilityKind.ANY : AvailabilityKind.NONE; break;
                    case AvailabilityPattern.MorningOnly: kind = AvailabilityKind.INT; interval = "08:00 - 16:00"; break;
                    case AvailabilityPattern.EveningOnly: kind = AvailabilityKind.INT; interval = "16:00 - 20:00"; break;
                    case AvailabilityPattern.ExplicitFullDay: kind = AvailabilityKind.INT; interval = "08:00 - 20:00"; break;
                    case AvailabilityPattern.InvalidInterval: kind = AvailabilityKind.INT; interval = "invalid"; break;
                    case AvailabilityPattern.Scarce: kind = random.Next(100) < 20 ? AvailabilityKind.ANY : AvailabilityKind.NONE; break;
                    case AvailabilityPattern.MixedPartial:
                        var choice = random.Next(5);
                        kind = choice == 0 ? AvailabilityKind.NONE : AvailabilityKind.INT;
                        interval = choice switch { 1 => "08:00 - 13:00", 2 => "13:00 - 20:00", 3 => "10:00 - 18:00", _ => "08:00 - 20:00" };
                        break;
                }
                member.Days.Add(TestDataFactory.CreateAvailabilityDayModel(day, kind, interval));
            }
            group.Members.Add(member);
        }
        return scenario with { ScenarioId = $"{pattern}-{scenario.ScenarioId}", AvailabilityGroups = new[] { group } };
    }

    public static GenerationScenario WithLimits(GenerationScenario source, int maxHours = 400,
        int maxWorkStreak = 31, int maxFullStreak = 31, int maxFullDays = 31, int minimumHours = 0)
    {
        var copy = Copy(source);
        copy.Schedule.MaxHoursPerEmpMonth = maxHours;
        copy.Schedule.MaxConsecutiveDays = maxWorkStreak;
        copy.Schedule.MaxConsecutiveFull = maxFullStreak;
        copy.Schedule.MaxFullPerMonth = maxFullDays;
        foreach (var employee in copy.Employees) employee.MinHoursMonth = minimumHours;
        return copy with { ScenarioId = $"{source.ScenarioId}-limits-{maxHours}-{maxWorkStreak}-{maxFullStreak}-{maxFullDays}-{minimumHours}" };
    }

    public static GenerationScenario WithShiftWindows(GenerationScenario source, string first, string? second)
    {
        var copy = Copy(source);
        copy.Schedule.Shift1Time = first;
        copy.Schedule.Shift2Time = second ?? "";
        return copy with { ScenarioId = $"{source.ScenarioId}-windows-{first}-{second}" };
    }

    public static GenerationScenario WithExternalCommitments(GenerationScenario source, IReadOnlyList<ScheduleSlotModel> slots)
        => Copy(source with { ExternalSlots = slots });

    public static GenerationScenario Copy(GenerationScenario source)
        => JsonSerializer.Deserialize<GenerationScenario>(JsonSerializer.Serialize(source))!;
}
