namespace JG.HealthKit.Checks;

/// <summary>
/// Health check that monitors available disk space against configured thresholds.
/// </summary>
/// <remarks>
/// Supports three-state reporting: healthy when above the warning threshold,
/// degraded when between warning and critical, unhealthy when below critical.
/// Completes synchronously (no I/O), so returns <see cref="ValueTask{T}"/> directly.
/// Thread-safe.
/// </remarks>
internal sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly string _driveName;
    private readonly long _minimumFreeBytes;
    private readonly long _degradedThresholdBytes;

    /// <summary>
    /// Initializes a new instance of <see cref="DiskSpaceHealthCheck"/>.
    /// </summary>
    /// <param name="driveName">
    /// The drive or mount point to check (e.g., <c>"C:\"</c> on Windows, <c>"/"</c> on Linux).
    /// </param>
    /// <param name="minimumFreeBytes">The minimum free bytes before reporting unhealthy.</param>
    /// <param name="degradedThresholdBytes">
    /// The free bytes threshold below which the check reports degraded.
    /// Must be greater than or equal to <paramref name="minimumFreeBytes"/>.
    /// When equal to <paramref name="minimumFreeBytes"/>, the degraded state is effectively disabled.
    /// </param>
    public DiskSpaceHealthCheck(string driveName, long minimumFreeBytes, long degradedThresholdBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveName);
        if (minimumFreeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumFreeBytes), minimumFreeBytes,
                "Minimum free bytes cannot be negative.");
        }
        if (degradedThresholdBytes < minimumFreeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedThresholdBytes), degradedThresholdBytes,
                "Degraded threshold must be greater than or equal to the minimum free bytes threshold.");
        }

        _driveName = driveName;
        _minimumFreeBytes = minimumFreeBytes;
        _degradedThresholdBytes = degradedThresholdBytes;
    }

    /// <inheritdoc />
    public ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var drive = new DriveInfo(_driveName);

        if (!drive.IsReady)
        {
            return new ValueTask<HealthCheckResult>(
                HealthCheckResult.Unhealthy($"Drive {_driveName} is not ready"));
        }

        long available = drive.AvailableFreeSpace;
        var data = new Dictionary<string, object>(2)
        {
            ["availableBytes"] = available,
            ["totalBytes"] = drive.TotalSize
        };

        if (available < _minimumFreeBytes)
        {
            return new ValueTask<HealthCheckResult>(
                HealthCheckResult.Unhealthy(
                    $"Low disk space on {_driveName}: {FormatBytes(available)} available",
                    data: data));
        }

        if (available < _degradedThresholdBytes)
        {
            return new ValueTask<HealthCheckResult>(
                HealthCheckResult.Degraded(
                    $"Disk space warning on {_driveName}: {FormatBytes(available)} available",
                    data: data));
        }

        return new ValueTask<HealthCheckResult>(
            HealthCheckResult.Healthy(
                $"Disk space on {_driveName}: {FormatBytes(available)} available",
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
