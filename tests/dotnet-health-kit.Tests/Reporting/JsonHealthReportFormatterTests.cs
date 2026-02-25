using System.Text;
using System.Text.Json;
using JG.HealthKit.Reporting;

namespace JG.HealthKit.Tests.Reporting;

public sealed class JsonHealthReportFormatterTests
{
    private readonly JsonHealthReportFormatter _formatter = new();

    [Fact]
    public void ContentType_IsApplicationJson()
    {
        _formatter.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task WriteAsync_EmptyReport_WritesValidJson()
    {
        var report = HealthReport.Empty;

        var json = await FormatAsync(report);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("totalDuration").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("checks").EnumerateObject().Should().BeEmpty();
    }

    [Fact]
    public async Task WriteAsync_SingleEntry_WritesCorrectStructure()
    {
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["db"] = HealthCheckResult.Healthy("Database ok")
                .WithDuration(TimeSpan.FromMilliseconds(5))
        };
        var report = new HealthReport(entries, HealthStatus.Healthy, TimeSpan.FromMilliseconds(10));

        var json = await FormatAsync(report);
        var doc = JsonDocument.Parse(json);

        var checks = doc.RootElement.GetProperty("checks");
        var db = checks.GetProperty("db");
        db.GetProperty("status").GetString().Should().Be("Healthy");
        db.GetProperty("description").GetString().Should().Be("Database ok");
        db.GetProperty("duration").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WriteAsync_UnhealthyReport_WritesUnhealthyStatus()
    {
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["api"] = HealthCheckResult.Unhealthy("Connection refused")
        };
        var report = new HealthReport(entries, HealthStatus.Unhealthy, TimeSpan.FromMilliseconds(100));

        var json = await FormatAsync(report);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        var api = doc.RootElement.GetProperty("checks").GetProperty("api");
        api.GetProperty("status").GetString().Should().Be("Unhealthy");
        api.GetProperty("description").GetString().Should().Be("Connection refused");
    }

    [Fact]
    public async Task WriteAsync_EntryWithData_WritesDataObject()
    {
        var data = new Dictionary<string, object>
        {
            ["availableBytes"] = 1073741824L,
            ["healthy"] = true,
            ["ratio"] = 0.75,
            ["label"] = "disk-c"
        };
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["disk"] = HealthCheckResult.Healthy("ok", data)
        };
        var report = new HealthReport(entries, HealthStatus.Healthy, TimeSpan.Zero);

        var json = await FormatAsync(report);
        var doc = JsonDocument.Parse(json);

        var diskData = doc.RootElement
            .GetProperty("checks")
            .GetProperty("disk")
            .GetProperty("data");

        diskData.GetProperty("availableBytes").GetInt64().Should().Be(1073741824L);
        diskData.GetProperty("healthy").GetBoolean().Should().BeTrue();
        diskData.GetProperty("ratio").GetDouble().Should().Be(0.75);
        diskData.GetProperty("label").GetString().Should().Be("disk-c");
    }

    [Fact]
    public async Task WriteAsync_NoDescription_OmitsDescriptionField()
    {
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["test"] = HealthCheckResult.Healthy()
        };
        var report = new HealthReport(entries, HealthStatus.Healthy, TimeSpan.Zero);

        var json = await FormatAsync(report);
        var doc = JsonDocument.Parse(json);

        var test = doc.RootElement.GetProperty("checks").GetProperty("test");
        test.TryGetProperty("description", out _).Should().BeFalse();
    }

    [Fact]
    public async Task WriteAsync_NoData_OmitsDataField()
    {
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["test"] = HealthCheckResult.Healthy("ok")
        };
        var report = new HealthReport(entries, HealthStatus.Healthy, TimeSpan.Zero);

        var json = await FormatAsync(report);
        var doc = JsonDocument.Parse(json);

        var test = doc.RootElement.GetProperty("checks").GetProperty("test");
        test.TryGetProperty("data", out _).Should().BeFalse();
    }

    [Fact]
    public async Task WriteAsync_ExceptionNotIncludedInOutput()
    {
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["test"] = HealthCheckResult.Unhealthy("failed", new InvalidOperationException("secret"))
        };
        var report = new HealthReport(entries, HealthStatus.Unhealthy, TimeSpan.Zero);

        var json = await FormatAsync(report);

        // Exception details must not leak
        json.Should().NotContain("secret");
        json.Should().NotContain("InvalidOperationException");
    }

    [Fact]
    public async Task WriteAsync_MultipleEntries_WritesAll()
    {
        var entries = new Dictionary<string, HealthCheckResult>
        {
            ["a"] = HealthCheckResult.Healthy("first"),
            ["b"] = HealthCheckResult.Degraded("second"),
            ["c"] = HealthCheckResult.Unhealthy("third")
        };
        var report = new HealthReport(entries, HealthStatus.Unhealthy, TimeSpan.Zero);

        var json = await FormatAsync(report);
        var doc = JsonDocument.Parse(json);

        var checks = doc.RootElement.GetProperty("checks");
        checks.GetProperty("a").GetProperty("status").GetString().Should().Be("Healthy");
        checks.GetProperty("b").GetProperty("status").GetString().Should().Be("Degraded");
        checks.GetProperty("c").GetProperty("status").GetString().Should().Be("Unhealthy");
    }

    private async Task<string> FormatAsync(HealthReport report)
    {
        using var stream = new MemoryStream();
        await _formatter.WriteAsync(stream, report);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
