namespace JG.HealthKit.Tests.Abstractions;

public sealed class HealthCheckRegistrationTests
{
    [Fact]
    public void Constructor_NullName_ThrowsArgumentException()
    {
        var act = () => new HealthCheckRegistration(
            null!,
            _ => Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            new HashSet<string>(StringComparer.Ordinal));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        var act = () => new HealthCheckRegistration(
            "  ",
            _ => Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            new HashSet<string>(StringComparer.Ordinal));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        var act = () => new HealthCheckRegistration(
            "test",
            null!,
            HealthStatus.Unhealthy,
            new HashSet<string>(StringComparer.Ordinal));

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Fact]
    public void Constructor_NullTags_ThrowsArgumentNullException()
    {
        var act = () => new HealthCheckRegistration(
            "test",
            _ => Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tags");
    }

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var tags = new HashSet<string>(StringComparer.Ordinal) { "ready" };
        var timeout = TimeSpan.FromSeconds(5);
        IHealthCheck check = Substitute.For<IHealthCheck>();

        var reg = new HealthCheckRegistration(
            "mycheck",
            _ => check,
            HealthStatus.Degraded,
            tags,
            timeout);

        reg.Name.Should().Be("mycheck");
        reg.FailureStatus.Should().Be(HealthStatus.Degraded);
        reg.Tags.Should().Contain("ready");
        reg.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void Timeout_DefaultsToNull()
    {
        var reg = new HealthCheckRegistration(
            "test",
            _ => Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            new HashSet<string>(StringComparer.Ordinal));

        reg.Timeout.Should().BeNull();
    }
}
