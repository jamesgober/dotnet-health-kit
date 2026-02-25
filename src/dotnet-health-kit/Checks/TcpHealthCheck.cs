using System.Net.Sockets;

namespace JG.HealthKit.Checks;

/// <summary>
/// Health check that verifies TCP connectivity to a remote host and port.
/// </summary>
/// <remarks>
/// Opens a TCP connection to the specified endpoint and reports healthy on success.
/// The connection is immediately closed after verification. Thread-safe.
/// </remarks>
internal sealed class TcpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;

    /// <summary>
    /// Initializes a new instance of <see cref="TcpHealthCheck"/>.
    /// </summary>
    /// <param name="host">The hostname or IP address to connect to.</param>
    /// <param name="port">The TCP port number.</param>
    public TcpHealthCheck(string host, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");
        }

        _host = host;
        _port = port;
    }

    /// <inheritdoc />
    public async ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy($"TCP connection to {_host}:{_port} succeeded");
    }
}
