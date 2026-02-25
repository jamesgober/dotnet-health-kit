namespace JG.HealthKit.Tests.Checks;

public sealed class MemoryHealthCheckTests
{
    [Fact]
    public void Constructor_ZeroUnhealthy_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new JG.HealthKit.Checks.MemoryHealthCheck(0, 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("unhealthyThresholdBytes");
    }

    [Fact]
    public void Constructor_NegativeDegraded_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new JG.HealthKit.Checks.MemoryHealthCheck(100, -1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("degradedThresholdBytes");
    }

    [Fact]
    public void Constructor_DegradedExceedsUnhealthy_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new JG.HealthKit.Checks.MemoryHealthCheck(100, 200);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("degradedThresholdBytes");
    }

    [Fact]
    public async Task CheckAsync_BelowDegraded_ReturnsHealthy()
    {
        // Use very high thresholds so current memory is well below
        var check = new JG.HealthKit.Checks.MemoryHealthCheck(
            unhealthyThresholdBytes: long.MaxValue,
            degradedThresholdBytes: long.MaxValue);
        var context = CreateContext("memory");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("allocatedBytes");
        result.Data.Should().ContainKey("gen0Collections");
        result.Data.Should().ContainKey("gen1Collections");
        result.Data.Should().ContainKey("gen2Collections");
    }

    [Fact]
    public async Task CheckAsync_AboveUnhealthy_ReturnsUnhealthy()
    {
        // Threshold of 1 byte means current memory will always exceed it
        var check = new JG.HealthKit.Checks.MemoryHealthCheck(
            unhealthyThresholdBytes: 1,
            degradedThresholdBytes: 1);
        var context = CreateContext("memory");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_BetweenDegradedAndUnhealthy_ReturnsDegraded()
    {
        long currentMemory = GC.GetTotalMemory(false);

        // Degraded: below current, Unhealthy: above current
        var check = new JG.HealthKit.Checks.MemoryHealthCheck(
            unhealthyThresholdBytes: currentMemory * 100,
            degradedThresholdBytes: 1);
        var context = CreateContext("memory");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    private static HealthCheckContext CreateContext(string name)
    {
        var reg = new HealthCheckRegistration(
            name,
            _ => Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            new HashSet<string>(StringComparer.Ordinal));
        return new HealthCheckContext(reg);
    }
}
