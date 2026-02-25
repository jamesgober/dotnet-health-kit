using System.Data;
using System.Data.Common;

namespace JG.HealthKit.Tests.Checks;

public sealed class DatabaseHealthCheckTests
{
    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        var act = () => new JG.HealthKit.Checks.DatabaseHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckAsync_ConnectionOpensSuccessfully_ReturnsHealthy()
    {
        var connection = Substitute.For<DbConnection>();
        var check = new JG.HealthKit.Checks.DatabaseHealthCheck(() => connection);
        var context = CreateContext("db");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("successful");
        await connection.Received(1).OpenAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAsync_WithTestQuery_ExecutesCommand()
    {
        var command = Substitute.For<DbCommand>();
        command.ExecuteNonQueryAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var connection = Substitute.For<DbConnection>();
        connection.CreateCommand().Returns(command);

        var check = new JG.HealthKit.Checks.DatabaseHealthCheck(
            () => connection, testQuery: "SELECT 1");
        var context = CreateContext("db");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        command.CommandText.Should().Be("SELECT 1");
        await command.Received(1).ExecuteNonQueryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAsync_FactoryReturnsNull_ReturnsUnhealthy()
    {
        var check = new JG.HealthKit.Checks.DatabaseHealthCheck(() => null!);
        var context = CreateContext("db");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("null");
    }

    [Fact]
    public async Task CheckAsync_OpenThrows_PropagatesException()
    {
        var connection = Substitute.For<DbConnection>();
        connection.When(c => c.OpenAsync(Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException("auth failed"));

        var check = new JG.HealthKit.Checks.DatabaseHealthCheck(() => connection);
        var context = CreateContext("db");

        var act = () => check.CheckAsync(context).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("auth failed");
    }

    [Fact]
    public async Task CheckAsync_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var connection = Substitute.For<DbConnection>();
        connection.When(c => c.OpenAsync(Arg.Any<CancellationToken>()))
            .Throw<OperationCanceledException>();

        var check = new JG.HealthKit.Checks.DatabaseHealthCheck(() => connection);
        var context = CreateContext("db");

        var act = () => check.CheckAsync(context, cts.Token).AsTask();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckAsync_WithoutTestQuery_DoesNotCreateCommand()
    {
        var connection = Substitute.For<DbConnection>();
        var check = new JG.HealthKit.Checks.DatabaseHealthCheck(() => connection);
        var context = CreateContext("db");

        await check.CheckAsync(context);

        connection.DidNotReceive().CreateCommand();
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
