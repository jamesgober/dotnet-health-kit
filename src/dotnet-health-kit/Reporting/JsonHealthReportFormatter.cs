using System.Text.Json;

namespace JG.HealthKit.Reporting;

/// <summary>
/// Formats a <see cref="HealthReport"/> as JSON using <see cref="Utf8JsonWriter"/>
/// for zero-allocation serialization.
/// </summary>
/// <remarks>
/// Produces a compact JSON document with status, total duration, and per-check entries.
/// Exception details are intentionally excluded from the output for security.
/// This type is thread-safe and stateless.
/// </remarks>
internal sealed class JsonHealthReportFormatter : IHealthReportFormatter
{
    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Indented = false,
        SkipValidation = false
    };

    /// <inheritdoc />
    public string ContentType => "application/json; charset=utf-8";

    /// <inheritdoc />
    public async ValueTask WriteAsync(
        Stream stream,
        HealthReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(report);

        var writer = new Utf8JsonWriter(stream, s_writerOptions);
        await using (writer.ConfigureAwait(false))
        {
            writer.WriteStartObject();

            writer.WriteString("status", report.Status.ToString());
            writer.WriteString("totalDuration", FormatDuration(report.TotalDuration));

            writer.WriteStartObject("checks");

            foreach (var kvp in report.Entries)
            {
                writer.WriteStartObject(kvp.Key);
                WriteEntry(writer, kvp.Value);
                writer.WriteEndObject();
            }

            writer.WriteEndObject(); // checks
            writer.WriteEndObject(); // root

            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static void WriteEntry(Utf8JsonWriter writer, HealthCheckResult entry)
    {
        writer.WriteString("status", entry.Status.ToString());
        writer.WriteString("duration", FormatDuration(entry.Duration));

        if (entry.Description is not null)
        {
            writer.WriteString("description", entry.Description);
        }

        if (entry.Data is { Count: > 0 })
        {
            writer.WriteStartObject("data");

            foreach (var kvp in entry.Data)
            {
                WriteDataValue(writer, kvp.Key, kvp.Value);
            }

            writer.WriteEndObject();
        }
    }

    private static void WriteDataValue(Utf8JsonWriter writer, string key, object value)
    {
        switch (value)
        {
            case int i:
                writer.WriteNumber(key, i);
                break;
            case long l:
                writer.WriteNumber(key, l);
                break;
            case double d:
                writer.WriteNumber(key, d);
                break;
            case float f:
                writer.WriteNumber(key, f);
                break;
            case decimal m:
                writer.WriteNumber(key, m);
                break;
            case bool b:
                writer.WriteBoolean(key, b);
                break;
            case string s:
                writer.WriteString(key, s);
                break;
            case null:
                writer.WriteNull(key);
                break;
            default:
                writer.WriteString(key, value.ToString());
                break;
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.ToString("c");
    }
}
