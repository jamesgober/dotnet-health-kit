namespace JG.HealthKit.Tests.Checks;

public sealed class DelegateHealthCheckTests
{
    [Fact]
    public void Constructor_NullDelegate_ThrowsArgumentNullException()
    {
        var act = () => new JG.HealthKit.Checks.DelegateHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckAsync_InvokesDelegate()
    {
        bool invoked = false;
        var check = new JG.HealthKit.Checks.DelegateHealthCheck(_ =>
        {
            invoked = true;
            return new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy("ok"));
        });

        var context = CreateContext("test");
        var result = await check.CheckAsync(context);

        invoked.Should().BeTrue();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("ok");
    }

    [Fact]
    public async Task CheckAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var check = new JG.HealthKit.Checks.DelegateHealthCheck(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy());
        });

        var context = CreateContext("test");
        var act = () => check.CheckAsync(context, cts.Token).AsTask();

        await act.Should().ThrowAsync<OperationCanceledException>();
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
