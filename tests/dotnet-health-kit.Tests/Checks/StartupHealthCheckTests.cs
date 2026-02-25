namespace JG.HealthKit.Tests.Checks;

public sealed class StartupHealthCheckTests
{
    [Fact]
    public void Constructor_NullSignal_ThrowsArgumentNullException()
    {
        var act = () => new JG.HealthKit.Checks.StartupHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckAsync_NotReady_ReturnsUnhealthy()
    {
        var signal = Substitute.For<IStartupHealthSignal>();
        signal.IsReady.Returns(false);

        var check = new JG.HealthKit.Checks.StartupHealthCheck(signal);
        var context = CreateContext("startup");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("starting");
    }

    [Fact]
    public async Task CheckAsync_Ready_ReturnsHealthy()
    {
        var signal = Substitute.For<IStartupHealthSignal>();
        signal.IsReady.Returns(true);

        var check = new JG.HealthKit.Checks.StartupHealthCheck(signal);
        var context = CreateContext("startup");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("started");
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
