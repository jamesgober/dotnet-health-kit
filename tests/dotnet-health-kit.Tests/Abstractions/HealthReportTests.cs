namespace JG.HealthKit.Tests.Abstractions;

public sealed class HealthReportTests
{
    [Fact]
    public void Constructor_NullEntries_ThrowsArgumentNullException()
    {
        var act = () => new HealthReport(null!, HealthStatus.Healthy, TimeSpan.Zero);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entries");
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["db"] = HealthCheckResult.Healthy("ok")
        };

        var report = new HealthReport(entries, HealthStatus.Healthy, TimeSpan.FromSeconds(1));

        report.Status.Should().Be(HealthStatus.Healthy);
        report.TotalDuration.Should().Be(TimeSpan.FromSeconds(1));
        report.Entries.Should().HaveCount(1);
        report.Entries.Should().ContainKey("db");
    }

    [Fact]
    public void Empty_IsHealthyWithNoEntries()
    {
        var empty = HealthReport.Empty;

        empty.Status.Should().Be(HealthStatus.Healthy);
        empty.TotalDuration.Should().Be(TimeSpan.Zero);
        empty.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Empty_ReturnsSameInstance()
    {
        HealthReport.Empty.Should().BeSameAs(HealthReport.Empty);
    }
}
