using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Schedule;
using BusinessLogicLayer.Services.Export;
using Xunit;

namespace ArchitectureTests;

public class ScheduleExcelContextBuilderTests
{
    [Fact]
    public void BuildScheduleContext_WithSeededLikeSlots_ShouldRenderIntervalsAndPositiveTotals()
    {
        var graph = new ScheduleModel
        {
            Id = 7,
            Name = "Seeded graph",
            Year = 2025,
            Month = 1,
            PeoplePerShift = 1,
            Shift1Time = "09:00 - 15:00",
            Shift2Time = "15:00 - 21:00"
        };

        var employees = new List<ScheduleEmployeeModel>
        {
            new()
            {
                Id = 1001,
                ScheduleId = 7,
                EmployeeId = 101,
                Employee = new EmployeeModel { Id = 101, FirstName = "Ivan", LastName = "Petrenko" }
            }
        };

        var slots = new List<ScheduleSlotModel>
        {
            new()
            {
                Id = 1,
                ScheduleId = 7,
                DayOfMonth = 1,
                SlotNo = 1,
                EmployeeId = 101,
                Status = SlotStatus.ASSIGNED,
                FromTime = "09:00",
                ToTime = "15:00"
            },
            new()
            {
                Id = 2,
                ScheduleId = 7,
                DayOfMonth = 2,
                SlotNo = 1,
                EmployeeId = 101,
                Status = SlotStatus.ASSIGNED,
                FromTime = "09:00:00",
                ToTime = "12:00:00"
            }
        };

        var sut = new ScheduleExcelContextBuilder();
        var context = sut.BuildScheduleContext(graph, shop: null, employees, slots);

        var table = context.ScheduleMatrix.Table;
        Assert.NotNull(table);

        var employeeColumns = table!.Columns
            .Cast<System.Data.DataColumn>()
            .Where(c => c.ColumnName.StartsWith("emp_", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(employeeColumns);

        var allEmployeeCellValues = table.Rows
            .Cast<System.Data.DataRow>()
            .SelectMany(r => employeeColumns.Select(c => r[c]?.ToString() ?? string.Empty))
            .ToList();

        Assert.Contains(allEmployeeCellValues, value => !string.Equals(value, ScheduleMatrixConstants.EmptyMark, StringComparison.Ordinal));
        Assert.Contains(allEmployeeCellValues, value => value.Contains(':') && value.Contains(" - ", StringComparison.Ordinal));

        var totals = ScheduleTotalsCalculator.Calculate(employees, slots);
        Assert.True(totals.TotalDuration > TimeSpan.Zero);
    }
}
