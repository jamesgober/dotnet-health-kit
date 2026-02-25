using System.Net.Sockets;

namespace JG.HealthKit.Tests.Checks;

public sealed class TcpHealthCheckTests
{
    [Fact]
    public void Constructor_NullHost_ThrowsArgumentException()
    {
        var act = () => new JG.HealthKit.Checks.TcpHealthCheck(null!, 80);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyHost_ThrowsArgumentException()
    {
        var act = () => new JG.HealthKit.Checks.TcpHealthCheck("  ", 80);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Constructor_InvalidPort_ThrowsArgumentOutOfRangeException(int port)
    {
        var act = () => new JG.HealthKit.Checks.TcpHealthCheck("localhost", port);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("port");
    }

    [Fact]
    public void Constructor_ValidPortBoundaries_DoesNotThrow()
    {
        var act1 = () => new JG.HealthKit.Checks.TcpHealthCheck("localhost", 1);
        var act2 = () => new JG.HealthKit.Checks.TcpHealthCheck("localhost", 65535);

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    [Fact]
    public async Task CheckAsync_ConnectionRefused_ThrowsSocketException()
    {
        // Port 1 is almost certainly not listening
        var check = new JG.HealthKit.Checks.TcpHealthCheck("127.0.0.1", 1);
        var context = CreateContext("tcp-test");

        var act = () => check.CheckAsync(context).AsTask();

        await act.Should().ThrowAsync<SocketException>();
    }

    [Fact]
    public async Task CheckAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var check = new JG.HealthKit.Checks.TcpHealthCheck("127.0.0.1", 80);
        var context = CreateContext("tcp-test");

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
