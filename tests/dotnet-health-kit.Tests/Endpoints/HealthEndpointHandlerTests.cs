using System.Text;
using System.Text.Json;
using JG.HealthKit.Endpoints;
using JG.HealthKit.Internal;
using JG.HealthKit.Reporting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace JG.HealthKit.Tests.Endpoints;

public sealed class HealthEndpointHandlerTests : IDisposable
{
    private readonly HealthKitOptions _options;
    private readonly HealthCheckRunner _runner;
    private readonly HealthCheckCache _cache;
    private readonly JsonHealthReportFormatter _formatter;
    private readonly HealthEndpointHandler _handler;
    private readonly IServiceProvider _serviceProvider;

    public HealthEndpointHandlerTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _options = new HealthKitOptions();
        _runner = new HealthCheckRunner(
            _serviceProvider, _options, NullLogger<HealthCheckRunner>.Instance);
        _cache = new HealthCheckCache(_options.CacheDuration);
        _formatter = new JsonHealthReportFormatter();
        _handler = new HealthEndpointHandler(_runner, _cache, _options, _formatter);
    }

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task HandleLivenessAsync_NoChecks_Returns200Healthy()
    {
        var context = CreateHttpContext();

        await _handler.HandleLivenessAsync(context);

        context.Response.StatusCode.Should().Be(200);
        context.Response.ContentType.Should().Be("application/json; charset=utf-8");

        var json = await ReadResponseBodyAsync(context);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task HandleReadinessAsync_HealthyCheck_Returns200()
    {
        var check = Substitute.For<IHealthCheck>();
        check.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy("ok")));

        _options.AddCheck("test", check, tags: new[] { HealthCheckTags.Ready });

        var handler = CreateHandler();
        var context = CreateHttpContext();

        await handler.HandleReadinessAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task HandleReadinessAsync_UnhealthyCheck_Returns503()
    {
        var check = Substitute.For<IHealthCheck>();
        check.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Unhealthy("down")));

        _options.AddCheck("test", check, tags: new[] { HealthCheckTags.Ready });

        var handler = CreateHandler();
        var context = CreateHttpContext();

        await handler.HandleReadinessAsync(context);

        context.Response.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task HandleReadinessAsync_DegradedCheck_Returns200()
    {
        var check = Substitute.For<IHealthCheck>();
        check.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Degraded("slow")));

        _options.AddCheck("test", check, tags: new[] { HealthCheckTags.Ready });

        var handler = CreateHandler();
        var context = CreateHttpContext();

        await handler.HandleReadinessAsync(context);

        context.Response.StatusCode.Should().Be(200);

        var json = await ReadResponseBodyAsync(context);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Degraded");
    }

    [Fact]
    public async Task HandleLivenessAsync_SetsNoCacheHeaders()
    {
        var context = CreateHttpContext();

        await _handler.HandleLivenessAsync(context);

        context.Response.Headers.CacheControl.ToString().Should().Contain("no-store");
        context.Response.Headers.Pragma.ToString().Should().Contain("no-cache");
    }

    [Fact]
    public async Task HandleLivenessAsync_OnlyRunsLiveTaggedChecks()
    {
        var liveCheck = Substitute.For<IHealthCheck>();
        liveCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy("live")));

        var readyCheck = Substitute.For<IHealthCheck>();
        readyCheck.CheckAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<HealthCheckResult>(HealthCheckResult.Unhealthy("fail")));

        _options.AddCheck("live-check", liveCheck, tags: new[] { HealthCheckTags.Live });
        _options.AddCheck("ready-check", readyCheck, tags: new[] { HealthCheckTags.Ready });

        var handler = CreateHandler();
        var context = CreateHttpContext();

        await handler.HandleLivenessAsync(context);

        // Should be healthy because only the live check ran
        context.Response.StatusCode.Should().Be(200);

        var json = await ReadResponseBodyAsync(context);
        var doc = JsonDocument.Parse(json);
        var checks = doc.RootElement.GetProperty("checks");
        checks.TryGetProperty("live-check", out _).Should().BeTrue();
        checks.TryGetProperty("ready-check", out _).Should().BeFalse();
    }

    private HealthEndpointHandler CreateHandler()
    {
        return new HealthEndpointHandler(
            new HealthCheckRunner(_serviceProvider, _options, NullLogger<HealthCheckRunner>.Instance),
            new HealthCheckCache(_options.CacheDuration),
            _options,
            _formatter);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
