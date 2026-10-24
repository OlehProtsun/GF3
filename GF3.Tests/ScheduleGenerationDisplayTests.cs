using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Schedule;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class ScheduleGenerationDisplayTests
{
    [Theory]
    [InlineData(2026, 2, 1, "15:00", "09:00 - 21:00")]
    [InlineData(2028, 2, 2, "15:00", "09:00 - 21:00")]
    [InlineData(2026, 4, 2, "15:00", "09:00 - 21:00")]
    [InlineData(2026, 7, 3, "15:00", "09:00 - 21:00")]
    public async Task GeneratedMonth_DisplaysContinuousWorkAsOneRange_AndPreservesHoursAndCoverage(
        int year, int month, int peoplePerShift, string secondStart, string expected)
    {
        var schedule = TestDataFactory.CreateScheduleModel(year: year, month: month);
        schedule.PeoplePerShift = peoplePerShift;
        schedule.Shift1Time = "09:00 - 15:00";
        schedule.Shift2Time = $"{secondStart} - 21:00";
        schedule.MaxHoursPerEmpMonth = 400;
        schedule.MaxConsecutiveDays = 31;
        schedule.MaxConsecutiveFull = 31;
        schedule.MaxFullPerMonth = 31;
        var employees = Enumerable.Range(1, peoplePerShift).Select(id => new ScheduleEmployeeModel
        {
            EmployeeId = id, DisplayOrder = id, MinHoursMonth = 0,
            Employee = TestDataFactory.CreateEmployeeModel(id, "Employee", id.ToString()),
        }).ToArray();

        var slots = (await new ScheduleGenerator().GenerateAsync(
            schedule, [], employees, null, CancellationToken.None)).ToList();
        var table = ScheduleMatrixEngine.BuildScheduleTable(year, month, slots, employees, out var columns);
        var days = DateTime.DaysInMonth(year, month);
        Assert.Equal(days * peoplePerShift * 2, slots.Count);
        Assert.Equal(peoplePerShift, columns.Count);
        Assert.Equal(days, table.Rows.Count);
        for (var day = 1; day <= days; day++)
        {
            Assert.False(ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(
                slots, day, peoplePerShift, schedule.Shift1Time, schedule.Shift2Time));
            foreach (var (column, employeeId) in columns)
            {
                Assert.Equal(expected, table.Rows[day - 1][column]);
                var assigned = slots.Where(slot => slot.DayOfMonth == day && slot.EmployeeId == employeeId).ToList();
                Assert.Equal(2, assigned.Count);
                var hours = assigned.Sum(slot => (TimeSpan.Parse(slot.ToTime) - TimeSpan.Parse(slot.FromTime)).TotalHours);
                Assert.Equal(6 + (TimeSpan.FromHours(21) - TimeSpan.Parse(secondStart)).TotalHours, hours, 6);
            }
        }
    }

    [Theory]
    [InlineData("15:00", "09:00 - 21:00")]
    [InlineData("15:01", "09:00 - 15:00, 15:01 - 21:00")]
    [InlineData("16:00", "09:00 - 15:00, 16:00 - 21:00")]
    public void Display_PreservesActualBreaks(string secondStart, string expected)
    {
        var slots = new[]
        {
            TestDataFactory.CreateScheduleSlotModel(1, 1, 1, secondStart, "21:00"),
            TestDataFactory.CreateScheduleSlotModel(1, 2, 1, "09:00", "15:00"),
        };
        var intervals = ScheduleMatrixEngine.MergeIntervalsForDisplay(slots);
        Assert.Equal(expected, string.Join(", ", intervals.Select(interval => $"{interval.from} - {interval.to}")));
    }
}
