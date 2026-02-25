using JG.HealthKit.Endpoints;
using JG.HealthKit.Internal;
using JG.HealthKit.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace JG.HealthKit.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHealthKit_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddHealthKit(_ => { });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHealthKit_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddHealthKit(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHealthKit_RegistersOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthKit(options =>
        {
            options.CacheDuration = TimeSpan.FromSeconds(5);
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<HealthKitOptions>();

        options.CacheDuration.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddHealthKit_RegistersCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthKit(_ => { });

        var provider = services.BuildServiceProvider();

        provider.GetService<HealthCheckRunner>().Should().NotBeNull();
        provider.GetService<HealthCheckCache>().Should().NotBeNull();
        provider.GetService<IHealthReportFormatter>().Should().NotBeNull();
        provider.GetService<HealthEndpointHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddHealthKit_SealsOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        HealthKitOptions? capturedOptions = null;
        services.AddHealthKit(options =>
        {
            capturedOptions = options;
        });

        var act = () => capturedOptions!.AddCheck("test", Substitute.For<IHealthCheck>());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddHealthKit_WithStartupCheck_RegistersSignal()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthKit(options =>
        {
            options.AddStartupCheck();
        });

        var provider = services.BuildServiceProvider();
        var signal = provider.GetService<IStartupHealthSignal>();

        signal.Should().NotBeNull();
        signal.Should().BeOfType<StartupHealthSignal>();
    }

    [Fact]
    public void AddHealthKit_WithoutStartupCheck_DoesNotRegisterSignal()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthKit(_ => { });

        var provider = services.BuildServiceProvider();
        var signal = provider.GetService<IStartupHealthSignal>();

        signal.Should().BeNull();
    }

    [Fact]
    public void AddHealthKit_RegistersHttpClientFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthKit(_ => { });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IHttpClientFactory>();

        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddHealthKit_OptionsWithChecks_RegistersCheckTypes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthKit(options =>
        {
            options.AddCheck<FakeCheck>("custom");
        });

        var provider = services.BuildServiceProvider();
        var check = provider.GetService<FakeCheck>();

        check.Should().NotBeNull();
    }

    public sealed class FakeCheck : IHealthCheck
    {
        public ValueTask<HealthCheckResult> CheckAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
            => new(HealthCheckResult.Healthy());
    }
}
