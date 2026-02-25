namespace JG.HealthKit;

/// <summary>
/// Formats a <see cref="HealthReport"/> for HTTP response output.
/// </summary>
public interface IHealthReportFormatter
{
    /// <summary>Gets the content type produced by this formatter (e.g., <c>application/json</c>).</summary>
    string ContentType { get; }

    /// <summary>
    /// Writes the health report to the specified stream.
    /// </summary>
    /// <param name="stream">The output stream to write to.</param>
    /// <param name="report">The health report to format.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    ValueTask WriteAsync(Stream stream, HealthReport report, CancellationToken cancellationToken = default);
}
