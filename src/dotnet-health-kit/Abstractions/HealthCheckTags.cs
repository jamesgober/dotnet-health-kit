namespace JG.HealthKit;

/// <summary>
/// Well-known tag constants for health check endpoint filtering.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// Tag for checks included in the liveness probe endpoint (<c>/health/live</c>).
    /// </summary>
    public const string Live = "live";

    /// <summary>
    /// Tag for checks included in the readiness probe endpoint (<c>/health/ready</c>).
    /// This is the default tag applied when no tags are specified.
    /// </summary>
    public const string Ready = "ready";
}
