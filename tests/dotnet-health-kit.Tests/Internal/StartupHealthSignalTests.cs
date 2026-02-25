using JG.HealthKit.Internal;

namespace JG.HealthKit.Tests.Internal;

public sealed class StartupHealthSignalTests
{
    [Fact]
    public void IsReady_InitiallyFalse()
    {
        var signal = new StartupHealthSignal();

        signal.IsReady.Should().BeFalse();
    }

    [Fact]
    public void MarkReady_SetsIsReadyToTrue()
    {
        var signal = new StartupHealthSignal();

        signal.MarkReady();

        signal.IsReady.Should().BeTrue();
    }

    [Fact]
    public void MarkReady_CalledMultipleTimes_StaysTrue()
    {
        var signal = new StartupHealthSignal();

        signal.MarkReady();
        signal.MarkReady();

        signal.IsReady.Should().BeTrue();
    }
}
