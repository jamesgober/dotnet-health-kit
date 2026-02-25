namespace JG.HealthKit.Checks;

/// <summary>
/// Health check that gates readiness on application startup completion.
/// </summary>
/// <remarks>
/// Reports <see cref="HealthStatus.Unhealthy"/> until <see cref="IStartupHealthSignal.MarkReady"/>
/// is called. This prevents Kubernetes readiness probes from routing traffic before
/// initialization is complete. Thread-safe.
/// </remarks>
internal sealed class StartupHealthCheck : IHealthCheck
{
    private readonly IStartupHealthSignal _signal;

    /// <summary>
    /// Initializes a new instance of <see cref="StartupHealthCheck"/>.
    /// </summary>
    /// <param name="signal">The startup signal to observe.</param>
    public StartupHealthCheck(IStartupHealthSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        _signal = signal;
    }

    /// <inheritdoc />
    public ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = _signal.IsReady
            ? HealthCheckResult.Healthy("Application started")
            : HealthCheckResult.Unhealthy("Application is starting");

        return new ValueTask<HealthCheckResult>(result);
    }
}
