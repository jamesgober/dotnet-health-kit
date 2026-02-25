namespace JG.HealthKit;

/// <summary>
/// Signals application startup completion to health check probes.
/// </summary>
/// <remarks>
/// Inject this interface and call <see cref="MarkReady"/> once all initialization
/// is complete. Until then, the startup health check reports <see cref="HealthStatus.Unhealthy"/>,
/// which prevents Kubernetes readiness probes from routing traffic to a partially initialized instance.
/// This type is thread-safe.
/// </remarks>
public interface IStartupHealthSignal
{
    /// <summary>Gets whether the application has signaled startup completion.</summary>
    bool IsReady { get; }

    /// <summary>
    /// Marks the application as ready. This is a one-way operation and cannot be reversed.
    /// </summary>
    void MarkReady();
}
