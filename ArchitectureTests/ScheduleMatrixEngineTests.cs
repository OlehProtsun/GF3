using BusinessLogicLayer.Schedule;
using Xunit;

namespace ArchitectureTests;

public class ScheduleMatrixEngineTests
{
    [Fact]
    public void TryParseTime_ShouldSupportHourMinuteAndSecondsFormats()
    {
        Assert.True(ScheduleMatrixEngine.TryParseTime("09:00", out _));
        Assert.True(ScheduleMatrixEngine.TryParseTime("9:00", out _));
        Assert.True(ScheduleMatrixEngine.TryParseTime("09:00:00", out _));
    }
}
