using System.Data.Common;
using JG.HealthKit.Checks;
using JG.HealthKit.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace JG.HealthKit;

/// <summary>
/// Extension methods for registering built-in health checks on <see cref="HealthKitOptions"/>.
/// </summary>
public static class HealthKitOptionsExtensions
{
    /// <summary>
    /// Adds a SQL Server TCP connectivity check by parsing the connection string
    /// to extract the server host and port.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="connectionString">A SQL Server connection string.</param>
    /// <param name="name">
    /// An optional name for the check. Defaults to <c>sqlserver:host:port</c>.
    /// </param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c>.</param>
    /// <param name="timeout">An optional per-check timeout.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString"/> is null, empty, or missing a server key.
    /// </exception>
    /// <remarks>
    /// This performs a TCP-level connectivity check to the SQL Server endpoint.
    /// For a full query-level check, use <see cref="AddDatabaseCheck"/> with a
    /// <see cref="DbConnection"/> factory instead.
    /// </remarks>
    public static HealthKitOptions AddSqlServer(
        this HealthKitOptions options,
        string connectionString,
        string? name = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var (host, port) = ConnectionStringParser.Parse(connectionString);
        name ??= $"sqlserver:{host}:{port}";

        return options.AddCheck(name, _ => new TcpHealthCheck(host, port), tags: tags, timeout: timeout);
    }

    /// <summary>
    /// Adds an HTTP dependency health check that sends a GET request to the specified URL.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="name">A unique name identifying the HTTP dependency.</param>
    /// <param name="url">The URL to send a health check request to.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c>.</param>
    /// <param name="timeout">An optional per-check timeout.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is null or whitespace, or <paramref name="url"/> is not a valid absolute URI.
    /// </exception>
    public static HealthKitOptions AddHttpDependency(
        this HealthKitOptions options,
        string name,
        string url,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"'{url}' is not a valid absolute URI.", nameof(url));
        }

        var clientName = $"HealthKit_{name}";
        return options.AddCheck(
            name,
            sp => new HttpHealthCheck(
                sp.GetRequiredService<IHttpClientFactory>(),
                clientName,
                uri),
            tags: tags,
            timeout: timeout);
    }

    /// <summary>
    /// Adds a disk space health check that monitors available free space on the specified drive.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="threshold">The minimum free bytes below which the check reports unhealthy.</param>
    /// <param name="degradedThreshold">
    /// Optional threshold below which the check reports degraded.
    /// Must be greater than or equal to <paramref name="threshold"/>.
    /// </param>
    /// <param name="driveName">
    /// The drive or mount point to check. Defaults to the OS root drive
    /// (<c>C:\</c> on Windows, <c>/</c> on Linux).
    /// </param>
    /// <param name="name">An optional name for the check. Defaults to <c>disk-space</c>.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c>.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="threshold"/> is negative, or <paramref name="degradedThreshold"/> is less than <paramref name="threshold"/>.
    /// </exception>
    public static HealthKitOptions AddDiskSpace(
        this HealthKitOptions options,
        long threshold,
        long? degradedThreshold = null,
        string? driveName = null,
        string? name = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        driveName ??= OperatingSystem.IsWindows() ? @"C:\" : "/";
        name ??= "disk-space";
        long degraded = degradedThreshold ?? threshold;

        var check = new DiskSpaceHealthCheck(driveName, threshold, degraded);
        return options.AddCheck(name, _ => check, tags: tags);
    }

    /// <summary>
    /// Adds a memory health check that monitors managed heap allocation.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="unhealthyThreshold">
    /// The allocated byte count above which the check reports unhealthy.
    /// Defaults to 1 GB.
    /// </param>
    /// <param name="degradedThreshold">
    /// The allocated byte count above which the check reports degraded.
    /// Must be less than or equal to <paramref name="unhealthyThreshold"/>.
    /// Defaults to <paramref name="unhealthyThreshold"/> (degraded state disabled).
    /// </param>
    /// <param name="name">An optional name for the check. Defaults to <c>memory</c>.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c>.</param>
    /// <returns>The options instance for chaining.</returns>
    public static HealthKitOptions AddMemoryCheck(
        this HealthKitOptions options,
        long unhealthyThreshold = 1_073_741_824,
        long? degradedThreshold = null,
        string? name = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        name ??= "memory";
        long degraded = degradedThreshold ?? unhealthyThreshold;

        var check = new MemoryHealthCheck(unhealthyThreshold, degraded);
        return options.AddCheck(name, _ => check, tags: tags);
    }

    /// <summary>
    /// Adds a TCP connectivity health check to the specified host and port.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="name">A unique name for the check.</param>
    /// <param name="host">The hostname or IP address.</param>
    /// <param name="port">The TCP port number (1–65535).</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c>.</param>
    /// <param name="timeout">An optional per-check timeout.</param>
    /// <returns>The options instance for chaining.</returns>
    public static HealthKitOptions AddTcp(
        this HealthKitOptions options,
        string name,
        string host,
        int port,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var check = new TcpHealthCheck(host, port);
        return options.AddCheck(name, _ => check, tags: tags, timeout: timeout);
    }

    /// <summary>
    /// Adds a database health check using a caller-provided <see cref="DbConnection"/> factory.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="name">A unique name for the check.</param>
    /// <param name="connectionFactory">
    /// A factory that creates a new <see cref="DbConnection"/>. The connection is opened
    /// and disposed on each health check invocation.
    /// </param>
    /// <param name="testQuery">
    /// An optional SQL query to execute after opening the connection (e.g., <c>"SELECT 1"</c>).
    /// </param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c>.</param>
    /// <param name="timeout">An optional per-check timeout.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connectionFactory"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// options.AddDatabaseCheck("mydb",
    ///     () => new SqlConnection(connectionString),
    ///     testQuery: "SELECT 1");
    /// </code>
    /// </example>
    public static HealthKitOptions AddDatabaseCheck(
        this HealthKitOptions options,
        string name,
        Func<DbConnection> connectionFactory,
        string? testQuery = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        var check = new DatabaseHealthCheck(connectionFactory, testQuery);
        return options.AddCheck(name, _ => check, tags: tags, timeout: timeout);
    }

    /// <summary>
    /// Adds a startup health check that gates readiness until
    /// <see cref="IStartupHealthSignal.MarkReady"/> is called.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c>.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <remarks>
    /// After calling this method, inject <see cref="IStartupHealthSignal"/> into your
    /// startup logic and call <see cref="IStartupHealthSignal.MarkReady"/> when initialization
    /// is complete. The readiness endpoint will return unhealthy until then.
    /// </remarks>
    public static HealthKitOptions AddStartupCheck(
        this HealthKitOptions options,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.RequiresStartupSignal = true;

        return options.AddCheck(
            "startup",
            sp => new Checks.StartupHealthCheck(
                sp.GetRequiredService<IStartupHealthSignal>()),
            tags: tags);
    }
}
