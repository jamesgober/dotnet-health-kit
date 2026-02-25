namespace JG.HealthKit;

/// <summary>
/// The result of a single health check execution.
/// </summary>
/// <remarks>
/// This is a value type to minimize allocations on the hot path.
/// Use the static factory methods <see cref="Healthy"/>, <see cref="Degraded"/>,
/// and <see cref="Unhealthy"/> to create instances.
/// This type is immutable and thread-safe.
/// </remarks>
public readonly struct HealthCheckResult : IEquatable<HealthCheckResult>
{
    /// <summary>Gets the aggregate status of the health check.</summary>
    public HealthStatus Status { get; }

    /// <summary>Gets a human-readable description of the check result.</summary>
    public string? Description { get; }

    /// <summary>Gets the wall-clock duration of the check execution.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the exception that caused the check to fail, if any.</summary>
    public Exception? Exception { get; }

    /// <summary>Gets optional key-value data associated with the check result.</summary>
    public IReadOnlyDictionary<string, object>? Data { get; }

    private HealthCheckResult(
        HealthStatus status,
        string? description,
        TimeSpan duration,
        Exception? exception,
        IReadOnlyDictionary<string, object>? data)
    {
        Status = status;
        Description = description;
        Duration = duration;
        Exception = exception;
        Data = data;
    }

    /// <summary>
    /// Creates a result representing a healthy component.
    /// </summary>
    /// <param name="description">An optional description of the healthy state.</param>
    /// <param name="data">Optional key-value data to include in the result.</param>
    /// <returns>A <see cref="HealthCheckResult"/> with <see cref="HealthStatus.Healthy"/> status.</returns>
    public static HealthCheckResult Healthy(
        string? description = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(HealthStatus.Healthy, description, TimeSpan.Zero, null, data);

    /// <summary>
    /// Creates a result representing a degraded component.
    /// </summary>
    /// <param name="description">A description of the degraded condition.</param>
    /// <param name="exception">An optional exception associated with the degraded state.</param>
    /// <param name="data">Optional key-value data to include in the result.</param>
    /// <returns>A <see cref="HealthCheckResult"/> with <see cref="HealthStatus.Degraded"/> status.</returns>
    public static HealthCheckResult Degraded(
        string description,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(HealthStatus.Degraded, description, TimeSpan.Zero, exception, data);

    /// <summary>
    /// Creates a result representing an unhealthy component.
    /// </summary>
    /// <param name="description">A description of the failure.</param>
    /// <param name="exception">An optional exception associated with the failure.</param>
    /// <param name="data">Optional key-value data to include in the result.</param>
    /// <returns>A <see cref="HealthCheckResult"/> with <see cref="HealthStatus.Unhealthy"/> status.</returns>
    public static HealthCheckResult Unhealthy(
        string description,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(HealthStatus.Unhealthy, description, TimeSpan.Zero, exception, data);

    /// <summary>
    /// Returns a new <see cref="HealthCheckResult"/> with the specified duration applied.
    /// </summary>
    internal HealthCheckResult WithDuration(TimeSpan duration)
        => new(Status, Description, duration, Exception, Data);

    /// <inheritdoc />
    public bool Equals(HealthCheckResult other)
        => Status == other.Status
           && string.Equals(Description, other.Description, StringComparison.Ordinal)
           && Duration == other.Duration;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is HealthCheckResult other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Status, Description, Duration);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(HealthCheckResult left, HealthCheckResult right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(HealthCheckResult left, HealthCheckResult right) => !left.Equals(right);
}
