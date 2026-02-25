namespace JG.HealthKit.Tests.Abstractions;

public sealed class HealthCheckResultTests
{
    [Fact]
    public void Healthy_DefaultDescription_ReturnsHealthyStatus()
    {
        var result = HealthCheckResult.Healthy();

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().BeNull();
        result.Exception.Should().BeNull();
        result.Data.Should().BeNull();
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Healthy_WithDescription_SetsDescription()
    {
        var result = HealthCheckResult.Healthy("All good");

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("All good");
    }

    [Fact]
    public void Healthy_WithData_SetsData()
    {
        var data = new Dictionary<string, object> { ["key"] = "value" };
        var result = HealthCheckResult.Healthy("ok", data);

        result.Data.Should().NotBeNull();
        result.Data.Should().ContainKey("key");
        result.Data!["key"].Should().Be("value");
    }

    [Fact]
    public void Degraded_SetsStatusAndDescription()
    {
        var result = HealthCheckResult.Degraded("Slow response");

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Slow response");
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Degraded_WithException_SetsException()
    {
        var ex = new TimeoutException("timed out");
        var result = HealthCheckResult.Degraded("Slow", ex);

        result.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void Unhealthy_SetsStatusAndDescription()
    {
        var result = HealthCheckResult.Unhealthy("Connection refused");

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Connection refused");
    }

    [Fact]
    public void Unhealthy_WithException_SetsException()
    {
        var ex = new InvalidOperationException("bad");
        var result = HealthCheckResult.Unhealthy("Failed", ex);

        result.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void WithDuration_ReturnsNewInstanceWithDuration()
    {
        var original = HealthCheckResult.Healthy("ok");
        var duration = TimeSpan.FromMilliseconds(42);

        var withDuration = original.WithDuration(duration);

        withDuration.Duration.Should().Be(duration);
        withDuration.Status.Should().Be(original.Status);
        withDuration.Description.Should().Be(original.Description);
        original.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = HealthCheckResult.Healthy("ok");
        var b = HealthCheckResult.Healthy("ok");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentStatus_ReturnsFalse()
    {
        var a = HealthCheckResult.Healthy("ok");
        var b = HealthCheckResult.Degraded("ok");

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentDescription_ReturnsFalse()
    {
        var a = HealthCheckResult.Healthy("ok");
        var b = HealthCheckResult.Healthy("fine");

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var a = HealthCheckResult.Healthy("ok");
        var b = HealthCheckResult.Healthy("ok");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_BoxedObject_Works()
    {
        var a = HealthCheckResult.Healthy("ok");
        object b = HealthCheckResult.Healthy("ok");

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_NonMatchingType_ReturnsFalse()
    {
        var a = HealthCheckResult.Healthy("ok");
        a.Equals("not a result").Should().BeFalse();
    }
}
