namespace JG.HealthKit.Internal;

/// <summary>
/// Parses SQL Server connection strings to extract host and port for TCP health checks.
/// </summary>
/// <remarks>
/// Handles standard SQL Server connection string formats:
/// <c>Server=host</c>, <c>Server=host,port</c>, <c>Server=tcp:host,port</c>,
/// <c>Data Source=host\instance</c>. Defaults to port 1433 when not specified.
/// </remarks>
internal static class ConnectionStringParser
{
    private const int DefaultSqlServerPort = 1433;

    /// <summary>
    /// Extracts the host and port from a SQL Server connection string.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>A tuple of (host, port).</returns>
    /// <exception cref="ArgumentException">The connection string is null, empty, or missing a server key.</exception>
    public static (string Host, int Port) Parse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        ReadOnlySpan<char> span = connectionString.AsSpan();
        ReadOnlySpan<char> serverValue = ReadOnlySpan<char>.Empty;

        // Manual parsing to avoid string allocations on the hot path
        while (span.Length > 0)
        {
            int semiIdx = span.IndexOf(';');
            ReadOnlySpan<char> pair = semiIdx >= 0 ? span[..semiIdx] : span;
            span = semiIdx >= 0 ? span[(semiIdx + 1)..] : ReadOnlySpan<char>.Empty;

            int eqIdx = pair.IndexOf('=');
            if (eqIdx < 0) continue;

            ReadOnlySpan<char> key = pair[..eqIdx].Trim();
            ReadOnlySpan<char> value = pair[(eqIdx + 1)..].Trim();

            if (key.Equals("Server", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Data Source", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Addr", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Address", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Network Address", StringComparison.OrdinalIgnoreCase))
            {
                serverValue = value;
                break;
            }
        }

        if (serverValue.IsEmpty)
        {
            throw new ArgumentException(
                "Connection string does not contain a Server or Data Source key.",
                nameof(connectionString));
        }

        return ParseServerValue(serverValue);
    }

    private static (string Host, int Port) ParseServerValue(ReadOnlySpan<char> value)
    {
        // Strip "tcp:" prefix
        if (value.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
        {
            value = value[4..];
        }

        // Check for port separator (comma for SQL Server)
        int commaIdx = value.IndexOf(',');
        if (commaIdx >= 0)
        {
            ReadOnlySpan<char> host = value[..commaIdx].Trim();
            ReadOnlySpan<char> portStr = value[(commaIdx + 1)..].Trim();

            // Strip instance name from host if present
            int backslashIdx = host.IndexOf('\\');
            if (backslashIdx >= 0)
            {
                host = host[..backslashIdx];
            }

            if (int.TryParse(portStr, out int port) && port is >= 1 and <= 65535)
            {
                return (host.ToString(), port);
            }

            return (host.ToString(), DefaultSqlServerPort);
        }

        // No port specified; check for instance name
        int instIdx = value.IndexOf('\\');
        if (instIdx >= 0)
        {
            return (value[..instIdx].Trim().ToString(), DefaultSqlServerPort);
        }

        return (value.Trim().ToString(), DefaultSqlServerPort);
    }
}
