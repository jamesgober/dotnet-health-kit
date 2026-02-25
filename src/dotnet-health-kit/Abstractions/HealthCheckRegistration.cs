namespace JG.HealthKit;

/// <summary>
/// Describes a health check registration including its name, factory, tags, timeout, and failure status.
/// </summary>
/// <remarks>
/// Registrations are immutable after <see cref="HealthKitOptions"/> is sealed during service configuration.
/// Tags control which endpoints include the check (e.g., liveness vs. readiness).
/// </remarks>
public sealed class HealthCheckRegistration
{
    /// <summary>Gets the unique name of the health check.</summary>
    public string Name { get; }

    /// <summary>
    /// Gets the factory that creates the <see cref="IHealthCheck"/> instance.
    /// </summary>
    public Func<IServiceProvider, IHealthCheck> Factory { get; }

    /// <summary>
    /// Gets or sets the status to report when the check throws an unhandled exception.
    /// Defaults to <see cref="HealthStatus.Unhealthy"/>.
    /// </summary>
    public HealthStatus FailureStatus { get; set; }

    /// <summary>
    /// Gets the tags associated with this check. Tags determine endpoint inclusion
    /// (e.g., <c>"live"</c> for liveness, <c>"ready"</c> for readiness).
    /// </summary>
    public ISet<string> Tags { get; }

    /// <summary>
    /// Gets or sets an optional timeout for this individual check.
    /// When <see langword="null"/>, the default timeout from <see cref="HealthKitOptions"/> is used.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="HealthCheckRegistration"/>.
    /// </summary>
    /// <param name="name">The unique name of the health check.</param>
    /// <param name="factory">A factory that produces the <see cref="IHealthCheck"/> instance.</param>
    /// <param name="failureStatus">The status to report on unhandled exceptions.</param>
    /// <param name="tags">The tags for endpoint filtering.</param>
    /// <param name="timeout">An optional per-check timeout.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> or <paramref name="tags"/> is <see langword="null"/>.</exception>
    public HealthCheckRegistration(
        string name,
        Func<IServiceProvider, IHealthCheck> factory,
        HealthStatus failureStatus,
        ISet<string> tags,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(tags);

        Name = name;
        Factory = factory;
        FailureStatus = failureStatus;
        Tags = tags;
        Timeout = timeout;
    }
}
