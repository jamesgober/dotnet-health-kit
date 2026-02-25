namespace JG.HealthKit;

/// <summary>
/// Defines a health check that can report the health of a component or subsystem.
/// </summary>
/// <remarks>
/// Implementations must be thread-safe when registered as singletons.
/// All I/O operations should be async and respect the provided <see cref="CancellationToken"/>.
/// </remarks>
public interface IHealthCheck
{
    /// <summary>
    /// Runs the health check and returns a result indicating the component's status.
    /// </summary>
    /// <param name="context">Context containing the check's registration metadata.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="HealthCheckResult"/> describing the component's health.</returns>
    ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default);
}
