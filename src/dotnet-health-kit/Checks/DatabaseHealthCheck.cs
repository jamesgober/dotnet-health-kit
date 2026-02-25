using System.Data.Common;

namespace JG.HealthKit.Checks;

/// <summary>
/// Health check that verifies database connectivity by opening a connection
/// and optionally executing a test query.
/// </summary>
/// <remarks>
/// Uses the caller-provided <see cref="DbConnection"/> factory so there is no
/// dependency on a specific database provider. Each invocation creates, opens,
/// and disposes its own connection. Thread-safe.
/// </remarks>
internal sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly Func<DbConnection> _connectionFactory;
    private readonly string? _testQuery;

    /// <summary>
    /// Initializes a new instance of <see cref="DatabaseHealthCheck"/>.
    /// </summary>
    /// <param name="connectionFactory">A factory that creates a new <see cref="DbConnection"/>.</param>
    /// <param name="testQuery">
    /// An optional SQL query to execute after opening the connection (e.g., <c>"SELECT 1"</c>).
    /// When <see langword="null"/>, only connection open is verified.
    /// </param>
    public DatabaseHealthCheck(Func<DbConnection> connectionFactory, string? testQuery = null)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
        _testQuery = testQuery;
    }

    /// <inheritdoc />
    public async ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        if (connection is null)
        {
            return HealthCheckResult.Unhealthy("Connection factory returned null");
        }

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (_testQuery is not null)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = _testQuery;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return HealthCheckResult.Healthy("Database connection successful");
    }
}
