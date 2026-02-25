using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace JG.HealthKit.Internal;

/// <summary>
/// Executes health check registrations concurrently and aggregates results into a <see cref="HealthReport"/>.
/// </summary>
/// <remarks>
/// Each check runs with its own timeout (or the default from <see cref="HealthKitOptions"/>).
/// Exceptions are caught and converted to <see cref="HealthStatus.Unhealthy"/> results.
/// This type is thread-safe and designed for use as a singleton.
/// </remarks>
internal sealed class HealthCheckRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HealthKitOptions _options;
    private readonly ILogger<HealthCheckRunner> _logger;

    public HealthCheckRunner(
        IServiceProvider serviceProvider,
        HealthKitOptions options,
        ILogger<HealthCheckRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Runs all specified health check registrations and returns an aggregated report.
    /// </summary>
    /// <param name="registrations">The checks to execute.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A <see cref="HealthReport"/> containing all check results.</returns>
    public async ValueTask<HealthReport> RunAsync(
        IReadOnlyList<HealthCheckRegistration> registrations,
        CancellationToken cancellationToken)
    {
        if (registrations.Count == 0)
        {
            return HealthReport.Empty;
        }

        var totalSw = Stopwatch.StartNew();

        var tasks = new Task<(string Name, HealthCheckResult Result)>[registrations.Count];
        for (int i = 0; i < registrations.Count; i++)
        {
            tasks[i] = ExecuteCheckAsync(registrations[i], cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        var entries = new Dictionary<string, HealthCheckResult>(
            registrations.Count,
            StringComparer.Ordinal);

        var worstStatus = HealthStatus.Healthy;

        for (int i = 0; i < tasks.Length; i++)
        {
            var (name, result) = tasks[i].Result;
            entries[name] = result;

            if (result.Status > worstStatus)
            {
                worstStatus = result.Status;
            }
        }

        totalSw.Stop();
        return new HealthReport(entries, worstStatus, totalSw.Elapsed);
    }

    private async Task<(string Name, HealthCheckResult Result)> ExecuteCheckAsync(
        HealthCheckRegistration registration,
        CancellationToken cancellationToken)
    {
        var checkSw = Stopwatch.StartNew();

        try
        {
            IHealthCheck check = registration.Factory(_serviceProvider);
            var context = new HealthCheckContext(registration);
            var timeout = registration.Timeout ?? _options.DefaultTimeout;

            HealthCheckResult result;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            result = await check.CheckAsync(context, cts.Token).ConfigureAwait(false);

            checkSw.Stop();
            result = result.WithDuration(checkSw.Elapsed);

            LogResult(registration.Name, result);
            return (registration.Name, result);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            checkSw.Stop();
            var timeout = registration.Timeout ?? _options.DefaultTimeout;
            var result = HealthCheckResult.Unhealthy(
                    $"Health check '{registration.Name}' timed out after {timeout.TotalSeconds:F1}s")
                .WithDuration(checkSw.Elapsed);

            _logger.LogWarning(
                "Health check '{CheckName}' timed out after {Timeout}",
                registration.Name, timeout);

            return (registration.Name, result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            checkSw.Stop();
            var failureResult = registration.FailureStatus switch
            {
                HealthStatus.Degraded => HealthCheckResult.Degraded(
                    $"Health check '{registration.Name}' failed: {ex.Message}", ex),
                _ => HealthCheckResult.Unhealthy(
                    $"Health check '{registration.Name}' failed: {ex.Message}", ex)
            };
            var result = failureResult.WithDuration(checkSw.Elapsed);

            _logger.LogError(
                ex,
                "Health check '{CheckName}' threw an exception",
                registration.Name);

            return (registration.Name, result);
        }
    }

    private void LogResult(string name, HealthCheckResult result)
    {
        switch (result.Status)
        {
            case HealthStatus.Healthy:
                _logger.LogDebug(
                    "Health check '{CheckName}' completed: {Status} in {Duration}ms",
                    name, result.Status, result.Duration.TotalMilliseconds);
                break;

            case HealthStatus.Degraded:
                _logger.LogWarning(
                    "Health check '{CheckName}' completed: {Status} — {Description}",
                    name, result.Status, result.Description);
                break;

            case HealthStatus.Unhealthy:
                _logger.LogError(
                    "Health check '{CheckName}' completed: {Status} — {Description}",
                    name, result.Status, result.Description);
                break;
        }
    }
}
