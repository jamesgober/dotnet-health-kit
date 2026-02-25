namespace JG.HealthKit.Checks;

/// <summary>
/// Health check that monitors managed memory usage against configured thresholds.
/// </summary>
/// <remarks>
/// Reports GC heap allocated bytes along with generation collection counts.
/// Supports three-state reporting with degraded and unhealthy thresholds.
/// Completes synchronously (no I/O). Thread-safe.
/// </remarks>
internal sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly long _unhealthyThresholdBytes;
    private readonly long _degradedThresholdBytes;

    /// <summary>
    /// Initializes a new instance of <see cref="MemoryHealthCheck"/>.
    /// </summary>
    /// <param name="unhealthyThresholdBytes">
    /// The allocated byte count above which the check reports unhealthy.
    /// </param>
    /// <param name="degradedThresholdBytes">
    /// The allocated byte count above which the check reports degraded.
    /// Must be less than or equal to <paramref name="unhealthyThresholdBytes"/>.
    /// When equal to <paramref name="unhealthyThresholdBytes"/>, the degraded state is effectively disabled.
    /// </param>
    public MemoryHealthCheck(long unhealthyThresholdBytes, long degradedThresholdBytes)
    {
        if (unhealthyThresholdBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unhealthyThresholdBytes), unhealthyThresholdBytes,
                "Unhealthy threshold must be a positive value.");
        }
        if (degradedThresholdBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedThresholdBytes), degradedThresholdBytes,
                "Degraded threshold must be a positive value.");
        }
        if (degradedThresholdBytes > unhealthyThresholdBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedThresholdBytes), degradedThresholdBytes,
                "Degraded threshold must be less than or equal to the unhealthy threshold.");
        }

        _unhealthyThresholdBytes = unhealthyThresholdBytes;
        _degradedThresholdBytes = degradedThresholdBytes;
    }

    /// <inheritdoc />
    public ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        long allocated = GC.GetTotalMemory(forceFullCollection: false);

        var data = new Dictionary<string, object>(4)
        {
            ["allocatedBytes"] = allocated,
            ["gen0Collections"] = GC.CollectionCount(0),
            ["gen1Collections"] = GC.CollectionCount(1),
            ["gen2Collections"] = GC.CollectionCount(2)
        };

        if (allocated >= _unhealthyThresholdBytes)
        {
            return new ValueTask<HealthCheckResult>(
                HealthCheckResult.Unhealthy(
                    $"High memory usage: {FormatBytes(allocated)} allocated",
                    data: data));
        }

        if (allocated >= _degradedThresholdBytes)
        {
            return new ValueTask<HealthCheckResult>(
                HealthCheckResult.Degraded(
                    $"Elevated memory usage: {FormatBytes(allocated)} allocated",
                    data: data));
        }

        return new ValueTask<HealthCheckResult>(
            HealthCheckResult.Healthy(
                $"Memory usage: {FormatBytes(allocated)} allocated",
                data: data));
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824L => $"{bytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576L => $"{bytes / 1_048_576.0:F1} MB",
            >= 1_024L => $"{bytes / 1_024.0:F1} KB",
            _ => $"{bytes} B"
        };
    }
}
