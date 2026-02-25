using JG.HealthKit;
using JG.HealthKit.Endpoints;
using JG.HealthKit.Internal;
using JG.HealthKit.Reporting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering HealthKit services in the dependency injection container.
/// </summary>
public static class HealthKitServiceCollectionExtensions
{
    /// <summary>
    /// Adds HealthKit health check services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">A delegate to configure <see cref="HealthKitOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// builder.Services.AddHealthKit(options =>
    /// {
    ///     options.AddHttpDependency("api", "https://api.example.com/health");
    ///     options.AddDiskSpace(threshold: 500_000_000);
    ///     options.CacheDuration = TimeSpan.FromSeconds(10);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddHealthKit(
        this IServiceCollection services,
        Action<HealthKitOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new HealthKitOptions();
        configure(options);
        options.Seal();

        services.AddSingleton(options);

        // Register check types added via AddCheck<T>()
        for (int i = 0; i < options.CheckTypes.Count; i++)
        {
            services.TryAddTransient(options.CheckTypes[i]);
        }

        // Startup signal
        if (options.RequiresStartupSignal)
        {
            services.TryAddSingleton<IStartupHealthSignal, StartupHealthSignal>();
        }

        // Core infrastructure
        services.TryAddSingleton<HealthCheckRunner>();
        services.TryAddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<HealthKitOptions>();
            return new HealthCheckCache(opts.CacheDuration);
        });
        services.TryAddSingleton<IHealthReportFormatter, JsonHealthReportFormatter>();
        services.TryAddSingleton<HealthEndpointHandler>();

        // HttpClient support for HTTP health checks
        services.AddHttpClient();

        return services;
    }
}
