namespace JG.HealthKit;

/// <summary>
/// Represents the health status of a component or the overall system.
/// </summary>
/// <remarks>
/// Values are ordered by severity. <see cref="Healthy"/> is the best state,
/// <see cref="Unhealthy"/> is the worst. Aggregate status is determined by
/// taking the maximum value across all individual check results.
/// </remarks>
public enum HealthStatus
{
    /// <summary>The component is working normally.</summary>
    Healthy = 0,

    /// <summary>The component is functioning but with reduced capability or elevated resource usage.</summary>
    Degraded = 1,

    /// <summary>The component is not functioning and the service cannot handle requests.</summary>
    Unhealthy = 2
}
