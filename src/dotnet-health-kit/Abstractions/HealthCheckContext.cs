namespace JG.HealthKit;

/// <summary>
/// Provides context about the currently executing health check.
/// </summary>
/// <remarks>
/// Passed to <see cref="IHealthCheck.CheckAsync"/> so implementations can
/// inspect the registration metadata (name, tags, failure status) when producing a result.
/// </remarks>
public sealed class HealthCheckContext
{
    /// <summary>
    /// Gets the registration associated with the executing health check.
    /// </summary>
    public HealthCheckRegistration Registration { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HealthCheckContext"/>.
    /// </summary>
    /// <param name="registration">The registration metadata for this check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registration"/> is <see langword="null"/>.</exception>
    public HealthCheckContext(HealthCheckRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        Registration = registration;
    }
}
