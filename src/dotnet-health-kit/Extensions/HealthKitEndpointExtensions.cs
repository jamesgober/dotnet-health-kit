using JG.HealthKit;
using JG.HealthKit.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for mapping HealthKit endpoints on the ASP.NET Core routing pipeline.
/// </summary>
public static class HealthKitEndpointExtensions
{
    /// <summary>
    /// Maps the HealthKit liveness and readiness probe endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// By default, maps <c>GET /health/live</c> and <c>GET /health/ready</c>.
    /// Paths are configurable via <see cref="HealthKitOptions.LivenessPath"/> and
    /// <see cref="HealthKitOptions.ReadinessPath"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapHealthKit();
    /// </code>
    /// </example>
    public static IEndpointRouteBuilder MapHealthKit(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<HealthKitOptions>();

        endpoints.MapGet(options.LivenessPath, static async (HttpContext context) =>
        {
            var handler = context.RequestServices.GetRequiredService<HealthEndpointHandler>();
            await handler.HandleLivenessAsync(context);
        }).ExcludeFromDescription();

        endpoints.MapGet(options.ReadinessPath, static async (HttpContext context) =>
        {
            var handler = context.RequestServices.GetRequiredService<HealthEndpointHandler>();
            await handler.HandleReadinessAsync(context);
        }).ExcludeFromDescription();

        return endpoints;
    }
}
