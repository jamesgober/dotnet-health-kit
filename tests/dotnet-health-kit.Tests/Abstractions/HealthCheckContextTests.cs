namespace JG.HealthKit.Tests.Abstractions;

public sealed class HealthCheckContextTests
{
    [Fact]
    public void Constructor_NullRegistration_ThrowsArgumentNullException()
    {
        var act = () => new HealthCheckContext(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("registration");
    }

    [Fact]
    public void Constructor_SetsRegistration()
    {
        var reg = new HealthCheckRegistration(
            "test",
            _ => Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            new HashSet<string>(StringComparer.Ordinal));

        var context = new HealthCheckContext(reg);

        context.Registration.Should().BeSameAs(reg);
    }
}
