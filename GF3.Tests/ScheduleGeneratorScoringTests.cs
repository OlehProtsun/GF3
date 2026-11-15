using System.Reflection;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class ScheduleGeneratorScoringTests
{
    private static readonly Type GeneratorType = ReflectionTestHelper.GetType(
        "BusinessLogicLayer",
        "BusinessLogicLayer.Generators.ScheduleGenerator");

    private static readonly Type AttemptScoreType = ReflectionTestHelper.GetType(
        "BusinessLogicLayer",
        "BusinessLogicLayer.Generators.ScheduleGenerator+AttemptScore");

    private static readonly Type DayCoverageMetricsType = ReflectionTestHelper.GetType(
        "BusinessLogicLayer",
        "BusinessLogicLayer.Generators.ScheduleGenerator+DayCoverageMetrics");

    private static readonly Type ExactDayScoreType = ReflectionTestHelper.GetType(
        "BusinessLogicLayer",
        "BusinessLogicLayer.Generators.ScheduleGenerator+ExactDayScore");

    private static readonly Type ExactDayOptionType = ReflectionTestHelper.GetType(
        "BusinessLogicLayer",
        "BusinessLogicLayer.Generators.ScheduleGenerator+ExactDayOption");

    [Fact]
    public void AttemptScore_IsBetterThan_OrdersByConflictCoverageDeficitsAndTieBreakers()
    {
        var baseline = CreateAttemptScore();

        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(conflictDays: 0), CreateAttemptScore(conflictDays: 1)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(coverageGap: 10), CreateAttemptScore(coverageGap: 20)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(minHourDeficit: 3), CreateAttemptScore(minHourDeficit: 5)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(minHourSquaredDeficit: 9), CreateAttemptScore(minHourSquaredDeficit: 16)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(desiredHourDeficit: 2), CreateAttemptScore(desiredHourDeficit: 4)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(desiredHourSquaredDeficit: 4), CreateAttemptScore(desiredHourSquaredDeficit: 9)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(restPenalty: 1), CreateAttemptScore(restPenalty: 2)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(unfurnishedSlots: 1), CreateAttemptScore(unfurnishedSlots: 2)));
        Assert.True(IsAttemptScoreBetter(CreateAttemptScore(overlapDays: 0), CreateAttemptScore(overlapDays: 1)));

        Assert.False(IsAttemptScoreBetter(baseline, baseline));
        Assert.False(IsAttemptScoreBetter(CreateAttemptScore(conflictDays: 2), CreateAttemptScore(conflictDays: 1)));
    }

    [Fact]
    public void DayCoverageMetrics_IsBetterThan_PrioritizesCoverageBalanceFillAndHours()
    {
        Assert.True(IsDayCoverageBetter(
            CreateDayCoverageMetrics(coverage1: 10, coverage2: 10, shift1GapMinutes: 5, shift2GapMinutes: 5),
            CreateDayCoverageMetrics(coverage1: 10, coverage2: 10, shift1GapMinutes: 10, shift2GapMinutes: 10)));
        Assert.True(IsDayCoverageBetter(
            CreateDayCoverageMetrics(coverage1: 10, coverage2: 10, shift1GapMinutes: 5, shift2GapMinutes: 5),
            CreateDayCoverageMetrics(coverage1: 10, coverage2: 10, shift1GapMinutes: 2, shift2GapMinutes: 8)));
        Assert.True(IsDayCoverageBetter(
            CreateDayCoverageMetrics(unfilledSlots: 0),
            CreateDayCoverageMetrics(unfilledSlots: 1)));
        Assert.True(IsDayCoverageBetter(
            CreateDayCoverageMetrics(coverage1: 12, coverage2: 12),
            CreateDayCoverageMetrics(coverage1: 8, coverage2: 8)));
        Assert.True(IsDayCoverageBetter(
            CreateDayCoverageMetrics(coverage1: 9, coverage2: 9, filled1: 2, filled2: 1),
            CreateDayCoverageMetrics(coverage1: 9, coverage2: 9, filled1: 1, filled2: 1)));
        Assert.True(IsDayCoverageBetter(
            CreateDayCoverageMetrics(totalHours: 16),
            CreateDayCoverageMetrics(totalHours: 12)));

        var baseline = CreateDayCoverageMetrics();
        Assert.False(IsDayCoverageBetter(baseline, baseline));
        Assert.False(IsDayCoverageBetter(
            CreateDayCoverageMetrics(totalHours: 8),
            CreateDayCoverageMetrics(totalHours: 12)));
    }

    [Fact]
    public void ExactDayScore_AddAndIsBetterForSameFill_PreserveScorePriorities()
    {
        var baseScore = CreateExactDayScore(coverage1: 10, coverage2: 5, hardGain: 1, softGain: 2, addedHours: 3, fullDayCount: 1);
        var option = CreateExactDayOption(stateCode: 1, shift1Slots: 1, shift2Slots: 0, coverage1: 4, coverage2: 3, addedHours: 2, hardGain: 2, softGain: 1, fullDayCount: 0);

        var addedScore = InvokeInstance(baseScore, "Add", option);

        Assert.Equal(14, GetProperty<int>(addedScore, "Coverage1"));
        Assert.Equal(8, GetProperty<int>(addedScore, "Coverage2"));
        Assert.Equal(3, GetProperty<double>(addedScore, "HardGain"));
        Assert.Equal(3, GetProperty<double>(addedScore, "SoftGain"));
        Assert.Equal(5, GetProperty<double>(addedScore, "AddedHours"));
        Assert.Equal(1, GetProperty<int>(addedScore, "FullDayCount"));

        Assert.True(IsExactDayScoreBetter(CreateExactDayScore(coverage1: 10, coverage2: 10), CreateExactDayScore(coverage1: 8, coverage2: 8)));
        Assert.True(IsExactDayScoreBetter(CreateExactDayScore(coverage1: 9, coverage2: 9, hardGain: 5), CreateExactDayScore(coverage1: 9, coverage2: 9, hardGain: 3)));
        Assert.True(IsExactDayScoreBetter(CreateExactDayScore(coverage1: 9, coverage2: 9, hardGain: 3, softGain: 5), CreateExactDayScore(coverage1: 9, coverage2: 9, hardGain: 3, softGain: 4)));
        Assert.True(IsExactDayScoreBetter(CreateExactDayScore(coverage1: 9, coverage2: 9, fullDayCount: 0), CreateExactDayScore(coverage1: 9, coverage2: 9, fullDayCount: 1)));
        Assert.True(IsExactDayScoreBetter(CreateExactDayScore(coverage1: 9, coverage2: 9, addedHours: 8), CreateExactDayScore(coverage1: 9, coverage2: 9, addedHours: 6)));

        Assert.False(IsExactDayScoreBetter(baseScore, baseScore));
    }

    [Fact]
    public void CandidatePriority_OrdersHardNeedAvailabilitySoftNeedAndRoundRobinTieBreakers()
    {
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(pairsOtherShiftSameSlot: true),
            CreateCandidatePriority(pairsOtherShiftSameSlot: false)));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(blocksOtherShiftPair: false),
            CreateCandidatePriority(blocksOtherShiftPair: true)));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(minHours: [8], desiredHours: [8], remainingPotentialHours: [0, 8, 8], totalHours: [0]),
            CreateCandidatePriority(minHours: [0], desiredHours: [8], remainingPotentialHours: [0, 8, 8], totalHours: [0])));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(minHours: [8], desiredHours: [8], remainingPotentialHours: [0, 8, 8], totalHours: [0], gainHours: 3),
            CreateCandidatePriority(minHours: [8], desiredHours: [8], remainingPotentialHours: [0, 20, 20], totalHours: [0], gainHours: 3)));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(unavailableBoundaryPressure: [0, 3, 0]),
            CreateCandidatePriority(unavailableBoundaryPressure: [0, 1, 0])));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [1]),
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [4])));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [4], gainHours: 8),
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [4], gainHours: 4)));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [4], fullDaysCount: [1]),
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [4], fullDaysCount: [2])));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [4], scarcityDays: [2]),
            CreateCandidatePriority(minHours: [0], desiredHours: [12], totalHours: [4], scarcityDays: [5])));
        Assert.True(IsBetterCandidate(
            CreateCandidatePriority(emp: 0, rrCursor: 0, n: 2, minHours: [0, 0], desiredHours: [12, 12], remainingPotentialHours: [0, 8, 8, 0, 8, 8], totalHours: [4, 4], fullDaysCount: [1, 1], scarcityDays: [2, 2], unavailableBoundaryPressure: [0, 0, 0, 0, 0, 0]),
            CreateCandidatePriority(emp: 1, rrCursor: 0, n: 2, minHours: [0, 0], desiredHours: [12, 12], remainingPotentialHours: [0, 8, 8, 0, 8, 8], totalHours: [4, 4], fullDaysCount: [1, 1], scarcityDays: [2, 2], unavailableBoundaryPressure: [0, 0, 0, 0, 0, 0])));

        var baseline = CreateCandidatePriority();
        Assert.False(IsBetterCandidate(baseline, baseline));
    }

    private static object CreateAttemptScore(
        int conflictDays = 0,
        int coverageGap = 0,
        double minHourDeficit = 0,
        double minHourSquaredDeficit = 0,
        double desiredHourDeficit = 0,
        double desiredHourSquaredDeficit = 0,
        double restPenalty = 0,
        int unfurnishedSlots = 0,
        int overlapDays = 0)
        => ReflectionTestHelper.Create(
            AttemptScoreType,
            conflictDays,
            coverageGap,
            minHourDeficit,
            minHourSquaredDeficit,
            desiredHourDeficit,
            desiredHourSquaredDeficit,
            restPenalty,
            unfurnishedSlots,
            overlapDays);

    private static object CreateDayCoverageMetrics(
        int coverage1 = 10,
        int coverage2 = 10,
        int shift1GapMinutes = 0,
        int shift2GapMinutes = 0,
        int filled1 = 1,
        int filled2 = 1,
        int unfilledSlots = 0,
        double totalHours = 8,
        bool hasSecondShift = true)
        => ReflectionTestHelper.Create(
            DayCoverageMetricsType,
            coverage1,
            coverage2,
            shift1GapMinutes,
            shift2GapMinutes,
            filled1,
            filled2,
            unfilledSlots,
            totalHours,
            hasSecondShift);

    private static object CreateExactDayScore(
        int coverage1 = 0,
        int coverage2 = 0,
        double hardGain = 0,
        double softGain = 0,
        double addedHours = 0,
        int fullDayCount = 0)
        => ReflectionTestHelper.Create(
            ExactDayScoreType,
            coverage1,
            coverage2,
            hardGain,
            softGain,
            addedHours,
            fullDayCount);

    private static object CreateExactDayOption(
        int stateCode,
        int shift1Slots,
        int shift2Slots,
        int coverage1,
        int coverage2,
        double addedHours,
        double hardGain,
        double softGain,
        int fullDayCount)
        => ReflectionTestHelper.Create(
            ExactDayOptionType,
            stateCode,
            shift1Slots,
            shift2Slots,
            coverage1,
            coverage2,
            addedHours,
            hardGain,
            softGain,
            fullDayCount);

    private static object CreateCandidatePriority(
        bool pairsOtherShiftSameSlot = false,
        bool blocksOtherShiftPair = false,
        int emp = 0,
        int day = 1,
        double gainHours = 4,
        double[]? minHours = null,
        double[]? desiredHours = null,
        double[]? remainingPotentialHours = null,
        double[]? totalHours = null,
        int[]? fullDaysCount = null,
        int[]? scarcityDays = null,
        int[]? unavailableBoundaryPressure = null,
        int rrCursor = 0,
        int n = 1,
        int stride = 3)
        => ReflectionTestHelper.InvokeStatic(
               GeneratorType,
               "BuildCandidatePriority",
               pairsOtherShiftSameSlot,
               blocksOtherShiftPair,
               emp,
               day,
               gainHours,
               minHours ?? [0],
               desiredHours ?? [8],
               remainingPotentialHours ?? [0, 8, 8],
               totalHours ?? [0],
               fullDaysCount ?? [0],
               scarcityDays ?? [5],
               unavailableBoundaryPressure ?? [0, 0, 0],
               rrCursor,
               n,
               stride)
           ?? throw new InvalidOperationException("Could not build candidate priority.");

    private static bool IsAttemptScoreBetter(object candidate, object other)
        => (bool)InvokeInstance(candidate, "IsBetterThan", other);

    private static bool IsDayCoverageBetter(object candidate, object other)
        => (bool)InvokeInstance(candidate, "IsBetterThan", other);

    private static bool IsExactDayScoreBetter(object candidate, object other)
        => (bool)InvokeInstance(candidate, "IsBetterForSameFill", other);

    private static bool IsBetterCandidate(object candidate, object best)
        => (bool)(ReflectionTestHelper.InvokeStatic(GeneratorType, "IsBetterCandidate", candidate, best)
            ?? throw new InvalidOperationException("Could not compare candidate priorities."));

    private static object InvokeInstance(object instance, string methodName, params object?[] arguments)
        => instance.GetType()
               .GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
               .Invoke(instance, arguments)
           ?? throw new InvalidOperationException($"Method {methodName} returned null.");

    private static T GetProperty<T>(object instance, string propertyName)
        => ReflectionTestHelper.GetPropertyValue<T>(instance, propertyName);
}
