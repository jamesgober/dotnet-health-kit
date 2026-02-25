using JG.HealthKit.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace JG.HealthKit.Tests.Internal;

public sealed class HealthCheckRunnerTests
{
    private readonly HealthKitOptions _options = new();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private HealthCheckRunner CreateRunner()
    {
        return new HealthCheckRunner(
            _serviceProvider,
            _options,
            NullLogger<HealthCheckRunner>.Instance);
    }

    [Fact]
    public async Task RunAsync_NoRegistrations_ReturnsEmptyHealthyReport()
    {
        var runner = CreateRunner();
        var registrations = Array.Empty<HealthCheckRegistration>();

        var report = await runner.RunAsync(registrations, CancellationToken.None);

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_SingleHealthyCheck_ReturnsHealthy()
    {
        var runner = CreateRunner();
        var check = Substitute.For<IHealthCheck>();
        check.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy("ok")));

        var registrations = new[]
        {
            new HealthCheckRegistration(
                "test",
                _ => check,
                HealthStatus.Unhealthy,
                new HashSet<string>(StringComparer.Ordinal) { "ready" })
        };

        var report = await runner.RunAsync(registrations, CancellationToken.None);

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().HaveCount(1);
        report.Entries["test"].Description.Should().Be("ok");
    }

    [Fact]
    public async Task RunAsync_MixedResults_ReturnsWorstStatus()
    {
        var runner = CreateRunner();

        var healthyCheck = Substitute.For<IHealthCheck>();
        healthyCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy()));

        var degradedCheck = Substitute.For<IHealthCheck>();
        degradedCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Degraded("warn")));

        var tags = new HashSet<string>(StringComparer.Ordinal) { "ready" };

        var registrations = new[]
        {
            new HealthCheckRegistration("healthy", _ => healthyCheck, HealthStatus.Unhealthy, new HashSet<string>(tags)),
            new HealthCheckRegistration("degraded", _ => degradedCheck, HealthStatus.Unhealthy, new HashSet<string>(tags))
        };

        var report = await runner.RunAsync(registrations, CancellationToken.None);

        report.Status.Should().Be(HealthStatus.Degraded);
        report.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task RunAsync_CheckThrows_ReportsUnhealthy()
    {
        var runner = CreateRunner();

        var failingCheck = Substitute.For<IHealthCheck>();
        failingCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<HealthCheckResult>>(_ => throw new InvalidOperationException("boom"));

        var registrations = new[]
        {
            new HealthCheckRegistration(
                "failing",
                _ => failingCheck,
                HealthStatus.Unhealthy,
                new HashSet<string>(StringComparer.Ordinal) { "ready" })
        };

        var report = await runner.RunAsync(registrations, CancellationToken.None);

        report.Status.Should().Be(HealthStatus.Unhealthy);
        report.Entries["failing"].Status.Should().Be(HealthStatus.Unhealthy);
        report.Entries["failing"].Description.Should().Contain("boom");
        report.Entries["failing"].Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task RunAsync_CheckThrowsWithDegradedFailureStatus_ReportsDegraded()
    {
        var runner = CreateRunner();

        var failingCheck = Substitute.For<IHealthCheck>();
        failingCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<HealthCheckResult>>(_ => throw new TimeoutException("slow"));

        var registrations = new[]
        {
            new HealthCheckRegistration(
                "degraded-on-fail",
                _ => failingCheck,
                HealthStatus.Degraded,
                new HashSet<string>(StringComparer.Ordinal) { "ready" })
        };

        var report = await runner.RunAsync(registrations, CancellationToken.None);

        report.Status.Should().Be(HealthStatus.Degraded);
        report.Entries["degraded-on-fail"].Status.Should().Be(HealthStatus.Degraded);
        report.Entries["degraded-on-fail"].Description.Should().Contain("slow");
    }

    [Fact]
    public async Task RunAsync_CheckTimesOut_ReportsUnhealthy()
    {
        var runner = CreateRunner();

        var slowCheck = Substitute.For<IHealthCheck>();
        slowCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                return new ValueTask<HealthCheckResult>(SlowCheckAsync(ct));
            });

        var registrations = new[]
        {
            new HealthCheckRegistration(
                "slow",
                _ => slowCheck,
                HealthStatus.Unhealthy,
                new HashSet<string>(StringComparer.Ordinal) { "ready" },
                TimeSpan.FromMilliseconds(50))
        };

        var report = await runner.RunAsync(registrations, CancellationToken.None);

        report.Status.Should().Be(HealthStatus.Unhealthy);
        report.Entries["slow"].Description.Should().Contain("timed out");
    }

    [Fact]
    public async Task RunAsync_SetsDurationOnResults()
    {
        var runner = CreateRunner();
        var check = Substitute.For<IHealthCheck>();
        check.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy()));

        var registrations = new[]
        {
            new HealthCheckRegistration(
                "test",
                _ => check,
                HealthStatus.Unhealthy,
                new HashSet<string>(StringComparer.Ordinal) { "ready" })
        };

        var report = await runner.RunAsync(registrations, CancellationToken.None);

        report.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        report.Entries["test"].Duration.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_MultipleChecks_RunsConcurrently()
    {
        var runner = CreateRunner();
        int concurrentCount = 0;
        int maxConcurrent = 0;
        var lockObj = new object();

        var slowCheck = Substitute.For<IHealthCheck>();
        slowCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                return new ValueTask<HealthCheckResult>(ConcurrentCheckAsync());

                async Task<HealthCheckResult> ConcurrentCheckAsync()
                {
                    lock (lockObj)
                    {
                        concurrentCount++;
                        if (concurrentCount > maxConcurrent)
                            maxConcurrent = concurrentCount;
                    }
                    await Task.Delay(50);
                    lock (lockObj) { concurrentCount--; }
                    return HealthCheckResult.Healthy();
                }
            });

        var tags = new HashSet<string>(StringComparer.Ordinal) { "ready" };
        var registrations = new[]
        {
            new HealthCheckRegistration("a", _ => slowCheck, HealthStatus.Unhealthy, new HashSet<string>(tags)),
            new HealthCheckRegistration("b", _ => slowCheck, HealthStatus.Unhealthy, new HashSet<string>(tags)),
            new HealthCheckRegistration("c", _ => slowCheck, HealthStatus.Unhealthy, new HashSet<string>(tags))
        };

        await runner.RunAsync(registrations, CancellationToken.None);

        maxConcurrent.Should().BeGreaterThan(1, "checks should run concurrently");
    }

    private static async Task<HealthCheckResult> SlowCheckAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
        return HealthCheckResult.Healthy();
    }
}
