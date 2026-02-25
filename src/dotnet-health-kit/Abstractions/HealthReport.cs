namespace JG.HealthKit;

/// <summary>
/// An aggregated report containing the results of all executed health checks.
/// </summary>
/// <remarks>
/// The overall <see cref="Status"/> is the worst (highest ordinal) status across all entries.
/// This type is immutable and thread-safe once constructed.
/// </remarks>
public sealed class HealthReport
{
    /// <summary>Gets the aggregate health status across all check entries.</summary>
    public HealthStatus Status { get; }

    /// <summary>Gets the total wall-clock time taken to execute all checks.</summary>
    public TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets the individual check results keyed by registration name.
    /// </summary>
    public IReadOnlyDictionary<string, HealthCheckResult> Entries { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HealthReport"/>.
    /// </summary>
    /// <param name="entries">The individual check results keyed by name.</param>
    /// <param name="status">The aggregate status.</param>
    /// <param name="totalDuration">The total execution duration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/>.</exception>
    public HealthReport(
        IReadOnlyDictionary<string, HealthCheckResult> entries,
        HealthStatus status,
        TimeSpan totalDuration)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries;
        Status = status;
        TotalDuration = totalDuration;
    }

    /// <summary>
    /// Creates an empty healthy report with zero duration.
    /// </summary>
    internal static HealthReport Empty { get; } = new(
        new Dictionary<string, HealthCheckResult>(0),
        HealthStatus.Healthy,
        TimeSpan.Zero);
}
