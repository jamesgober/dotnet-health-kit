using JG.HealthKit.Internal;
using Microsoft.AspNetCore.Http;

namespace JG.HealthKit.Endpoints;

/// <summary>
/// Handles HTTP requests for health check endpoints, coordinating the runner,
/// cache, and formatter to produce JSON responses.
/// </summary>
/// <remarks>
/// Designed as a singleton. The liveness and readiness endpoints run different
/// subsets of health checks based on their tags. Thread-safe.
/// </remarks>
internal sealed class HealthEndpointHandler
{
    private readonly HealthCheckRunner _runner;
    private readonly HealthCheckCache _cache;
    private readonly HealthKitOptions _options;
    private readonly IHealthReportFormatter _formatter;

    public HealthEndpointHandler(
        HealthCheckRunner runner,
        HealthCheckCache cache,
        HealthKitOptions options,
        IHealthReportFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(formatter);

        _runner = runner;
        _cache = cache;
        _options = options;
        _formatter = formatter;
    }

    /// <summary>
    /// Handles the liveness probe endpoint. Runs checks tagged with <c>"live"</c>.
    /// Returns 200 when healthy or degraded, 503 when unhealthy.
    /// </summary>
    public async Task HandleLivenessAsync(HttpContext context)
    {
        var registrations = _options.GetLiveRegistrations();
        var report = await _cache.GetOrRunAsync(
            HealthCheckTags.Live,
            ct => _runner.RunAsync(registrations, ct),
            context.RequestAborted).ConfigureAwait(false);

        await WriteResponseAsync(context, report).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the readiness probe endpoint. Runs checks tagged with <c>"ready"</c>.
    /// Returns 200 when healthy or degraded, 503 when unhealthy.
    /// </summary>
    public async Task HandleReadinessAsync(HttpContext context)
    {
        var registrations = _options.GetReadyRegistrations();
        var report = await _cache.GetOrRunAsync(
            HealthCheckTags.Ready,
            ct => _runner.RunAsync(registrations, ct),
            context.RequestAborted).ConfigureAwait(false);

        await WriteResponseAsync(context, report).ConfigureAwait(false);
    }

    private async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.StatusCode = report.Status == HealthStatus.Unhealthy ? 503 : 200;
        context.Response.ContentType = _formatter.ContentType;

        // Prevent caching of health responses by proxies
        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.Headers.Pragma = "no-cache";

        await _formatter.WriteAsync(
            context.Response.Body,
            report,
            context.RequestAborted).ConfigureAwait(false);
    }
}
