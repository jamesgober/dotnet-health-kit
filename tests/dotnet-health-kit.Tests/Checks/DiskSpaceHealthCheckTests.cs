namespace JG.HealthKit.Tests.Checks;

public sealed class DiskSpaceHealthCheckTests
{
    [Fact]
    public void Constructor_NegativeMinimum_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new JG.HealthKit.Checks.DiskSpaceHealthCheck(GetDriveName(), -1, 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("minimumFreeBytes");
    }

    [Fact]
    public void Constructor_DegradedLessThanMinimum_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new JG.HealthKit.Checks.DiskSpaceHealthCheck(GetDriveName(), 1000, 500);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("degradedThresholdBytes");
    }

    [Fact]
    public void Constructor_NullDriveName_ThrowsArgumentException()
    {
        var act = () => new JG.HealthKit.Checks.DiskSpaceHealthCheck(null!, 1000, 1000);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task CheckAsync_HealthyDisk_ReturnsHealthy()
    {
        // Use a threshold of 0 to guarantee healthy on any machine
        var check = new JG.HealthKit.Checks.DiskSpaceHealthCheck(GetDriveName(), 0, 0);
        var context = CreateContext("disk");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNullOrWhiteSpace();
        result.Data.Should().ContainKey("availableBytes");
        result.Data.Should().ContainKey("totalBytes");
    }

    [Fact]
    public async Task CheckAsync_BelowMinimum_ReturnsUnhealthy()
    {
        // Use impossibly high threshold
        var check = new JG.HealthKit.Checks.DiskSpaceHealthCheck(
            GetDriveName(), long.MaxValue, long.MaxValue);
        var context = CreateContext("disk");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_BelowDegradedAboveMinimum_ReturnsDegraded()
    {
        var drive = new DriveInfo(GetDriveName());
        if (!drive.IsReady) return;

        long available = drive.AvailableFreeSpace;
        // Set minimum below available, degraded above available
        long minimum = 0;
        long degraded = available + 1;

        var check = new JG.HealthKit.Checks.DiskSpaceHealthCheck(
            GetDriveName(), minimum, degraded);
        var context = CreateContext("disk");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    private static string GetDriveName()
    {
        return OperatingSystem.IsWindows() ? @"C:\" : "/";
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
