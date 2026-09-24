using System.ComponentModel.DataAnnotations;
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Schedule;
using GF3.Tests.Infrastructure;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace GF3.Tests;

public sealed class AvailabilityAndSchedulePureTests
{
    [Fact]
    public void AvailabilityCodeParser_TryParse_HandlesMarksAndIntervals()
    {
        Assert.True(AvailabilityCodeParser.TryParse("+", out var anyKind, out var anyInterval));
        Assert.Equal(AvailabilityKind.ANY, anyKind);
        Assert.Null(anyInterval);

        Assert.True(AvailabilityCodeParser.TryParse("-", out var noneKind, out var noneInterval));
        Assert.Equal(AvailabilityKind.NONE, noneKind);
        Assert.Null(noneInterval);

        Assert.True(AvailabilityCodeParser.TryParse("8:00-17:30", out var intervalKind, out var interval));
        Assert.Equal(AvailabilityKind.INT, intervalKind);
        Assert.Equal("08:00 - 17:30", interval);
    }

    [Fact]
    public void AvailabilityCodeParser_TryParse_ReturnsFalseForInvalidIntervals()
    {
        Assert.False(AvailabilityCodeParser.TryParse("17:00-08:00", out _, out _));
        Assert.False(AvailabilityCodeParser.TryParse("not-a-time", out _, out _));
        Assert.False(AvailabilityCodeParser.TryNormalizeInterval("8:00", out _));
    }

    [Fact]
    public void AvailabilityPayloadBuilder_TryBuild_ReturnsPayload_ForValidCodes()
    {
        var raw = new[]
        {
            (employeeId: 10, codes: (IList<(int day, string code)>)new List<(int, string)>
            {
                (1, "+"),
                (2, "8:00-16:00"),
            }),
        };

        var success = AvailabilityPayloadBuilder.TryBuild(raw, out var payload, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Single(payload);
        Assert.Equal(10, payload[0].employeeId);
        Assert.Equal(AvailabilityKind.ANY, payload[0].days[0].Kind);
        Assert.Equal("08:00 - 16:00", payload[0].days[1].IntervalStr);
    }

    [Fact]
    public void AvailabilityPayloadBuilder_TryBuild_ReturnsError_ForInvalidCode()
    {
        var raw = new[]
        {
            (employeeId: 7, codes: (IList<(int day, string code)>)new List<(int, string)>
            {
                (3, "25:00-26:00"),
            }),
        };

        var success = AvailabilityPayloadBuilder.TryBuild(raw, out var payload, out var error);

        Assert.False(success);
        Assert.Empty(payload);
        Assert.Equal("Invalid code '25:00-26:00' for day 3 (employee #7).", error);
    }

    [Fact]
    public void AvailabilityGroupValidator_Validate_ReturnsExpectedErrors()
    {
        var model = TestDataFactory.CreateAvailabilityGroupModel(
            name: "",
            year: DateTime.Today.Year + 10,
            month: 13);

        var errors = AvailabilityGroupValidator.Validate(model);

        Assert.Equal("Indicate group name.", errors[nameof(AvailabilityGroupModel.Name)]);
        Assert.Equal("Month must be between 1 and 12.", errors[nameof(AvailabilityGroupModel.Month)]);
        Assert.Equal("Invalid year.", errors[nameof(AvailabilityGroupModel.Year)]);
    }

    [Fact]
    public void ScheduleMatrixEngine_MergeIntervalsForDisplay_MergesAndDeduplicates()
    {
        var intervals = ScheduleMatrixEngine.MergeIntervalsForDisplay(
            [
                TestDataFactory.CreateScheduleSlotModel(1, 1, 1, "08:00", "12:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 2, 1, "10:00", "14:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 3, 1, "10:00", "14:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 4, 1, "15:00", "16:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 5, 1, "16:00", "18:00"),
            ]);

        Assert.Equal(
            [
                ("08:00", "14:00"),
                ("15:00", "18:00"),
            ],
            intervals);
    }

    [Fact]
    public void ScheduleMatrixEngine_ComputeConflictForDay_DetectsMissingEmployeeAndOverlap()
    {
        var missingEmployeeConflict = ScheduleMatrixEngine.ComputeConflictForDay(
            [
                TestDataFactory.CreateScheduleSlotModel(2, 1, null, "08:00", "12:00"),
            ],
            day: 2);

        var overlapConflict = ScheduleMatrixEngine.ComputeConflictForDay(
            [
                TestDataFactory.CreateScheduleSlotModel(3, 1, 5, "08:00", "12:00"),
                TestDataFactory.CreateScheduleSlotModel(3, 2, 5, "11:00", "15:00"),
            ],
            day: 3);

        Assert.True(missingEmployeeConflict);
        Assert.True(overlapConflict);
    }

    [Fact]
    public void ScheduleMatrixEngine_ComputeConflictForDayWithStaffing_DetectsCoverageProblems()
    {
        var slots = new[]
        {
            TestDataFactory.CreateScheduleSlotModel(4, 1, 1, "08:00", "16:00"),
            TestDataFactory.CreateScheduleSlotModel(4, 2, 2, "08:00", "16:00"),
            TestDataFactory.CreateScheduleSlotModel(4, 3, 2, "16:00", "20:00"),
        };

        var hasConflict = ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(
            slots,
            day: 4,
            peoplePerShift: 2,
            shift1Range: "08:00 - 16:00",
            shift2Range: "16:00 - 20:00");

        var noConflict = ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(
            [
                TestDataFactory.CreateScheduleSlotModel(4, 1, 1, "08:00", "16:00"),
                TestDataFactory.CreateScheduleSlotModel(4, 2, 2, "08:00", "16:00"),
            ],
            day: 4,
            peoplePerShift: 2,
            shift1Range: "08:00 - 16:00",
            shift2Range: null);

        Assert.True(hasConflict);
        Assert.False(noConflict);
    }

    [Fact]
    public void ScheduleMatrixEngine_BuildScheduleTable_CreatesExpectedColumnsRowsAndValues()
    {
        var employees = new List<ScheduleEmployeeModel>
        {
            new() { EmployeeId = 2, Employee = new EmployeeModel { Id = 2, FirstName = "Zoe", LastName = "Brown" } },
            new() { EmployeeId = 1, Employee = new EmployeeModel { Id = 1, FirstName = "Adam", LastName = "Adams" } },
            new() { EmployeeId = 1, Employee = new EmployeeModel { Id = 1, FirstName = "Adam", LastName = "Adams" } },
        };

        var slots = new List<ScheduleSlotModel>
        {
            TestDataFactory.CreateScheduleSlotModel(4, 1, 1, "08:00", "12:00"),
            TestDataFactory.CreateScheduleSlotModel(4, 2, 1, "10:00", "14:00"),
            TestDataFactory.CreateScheduleSlotModel(4, 1, null, "12:00", "16:00"),
        };

        var table = ScheduleMatrixEngine.BuildScheduleTable(2026, 4, slots, employees, out var columnMap);

        Assert.Equal(30, table.Rows.Count);
        Assert.Equal(2, columnMap.Count);
        Assert.Equal(1, columnMap["emp_1"]);
        Assert.Equal(2, columnMap["emp_2"]);
        Assert.Equal("Adam Adams", table.Columns["emp_1"]!.Caption);
        Assert.Equal("Zoe Brown", table.Columns["emp_2"]!.Caption);

        var dayFour = table.Rows.Cast<System.Data.DataRow>().Single(row => (int)row[ScheduleMatrixConstants.DayColumnName] == 4);
        Assert.True((bool)dayFour[ScheduleMatrixConstants.ConflictColumnName]);
        Assert.Equal("08:00 - 14:00", dayFour["emp_1"]);
        Assert.Equal(ScheduleMatrixConstants.EmptyMark, dayFour["emp_2"]);
        Assert.Equal(
            new DateTime(2026, 4, 4).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            (bool)dayFour[ScheduleMatrixConstants.WeekendColumnName]);
    }

    [Fact]
    public void ScheduleTotalsCalculator_Calculate_HandlesOvernightAndUnknownEmployees()
    {
        var employees = new List<ScheduleEmployeeModel>
        {
            new() { EmployeeId = 1 },
            new() { Employee = new EmployeeModel { Id = 2 } },
        };

        var slots = new List<ScheduleSlotModel>
        {
            TestDataFactory.CreateScheduleSlotModel(1, 1, 1, "08:00", "12:30"),
            TestDataFactory.CreateScheduleSlotModel(1, 2, 2, "22:00", "02:00"),
            TestDataFactory.CreateScheduleSlotModel(1, 3, 99, "10:00", "11:00"),
            TestDataFactory.CreateScheduleSlotModel(1, 4, 1, "bad", "11:00"),
        };

        var totals = ScheduleTotalsCalculator.Calculate(employees, slots);

        Assert.Equal(2, totals.TotalEmployees);
        Assert.Equal(TimeSpan.FromHours(8.5), totals.TotalDuration);
        Assert.Equal(TimeSpan.FromHours(4.5), totals.PerEmployeeDuration[1]);
        Assert.Equal(TimeSpan.FromHours(4), totals.PerEmployeeDuration[2]);
        Assert.Equal("8h 30m", ScheduleTotalsCalculator.FormatHoursMinutes(totals.TotalDuration));
    }

    [Fact]
    public void ApiProblemDetailsFactory_BuildValidationErrors_SupportsDifferentExceptionShapes()
    {
        var businessException = BusinessLogicLayer.Common.ValidationException.ForField("Name", "Required.");
        var dataAnnotationsException = new DataAnnotationsValidationException(
            new ValidationResult("Bad email.", [nameof(EmployeeModel.Email)]),
            null,
            value: null);

        var businessErrors = WebApi.Infrastructure.ApiProblemDetailsFactory.BuildValidationErrors(businessException);
        var annotationErrors = WebApi.Infrastructure.ApiProblemDetailsFactory.BuildValidationErrors(dataAnnotationsException);
        var generalErrors = WebApi.Infrastructure.ApiProblemDetailsFactory.BuildValidationErrors(new InvalidOperationException("Boom."));

        Assert.Equal(["Required."], businessErrors["name"]);
        Assert.Equal(["Bad email."], annotationErrors[nameof(EmployeeModel.Email)]);
        Assert.Equal(["Boom."], generalErrors["general"]);
    }
}
