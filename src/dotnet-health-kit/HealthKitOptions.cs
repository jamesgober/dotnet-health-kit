using Microsoft.Extensions.DependencyInjection;

namespace JG.HealthKit;

/// <summary>
/// Configuration options for the HealthKit health check system.
/// </summary>
/// <remarks>
/// Use this class within <c>AddHealthKit</c> to register health checks, configure
/// caching, timeouts, and endpoint paths. Once sealed during service registration,
/// the options become immutable.
/// </remarks>
public sealed class HealthKitOptions
{
    private volatile bool _sealed;
    private volatile IReadOnlyList<HealthCheckRegistration>? _liveRegistrations;
    private volatile IReadOnlyList<HealthCheckRegistration>? _readyRegistrations;

    private TimeSpan _cacheDuration = TimeSpan.Zero;
    private TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    private string _livenessPath = "/health/live";
    private string _readinessPath = "/health/ready";

    /// <summary>
    /// Gets or sets the duration for which health check results are cached.
    /// Set to <see cref="TimeSpan.Zero"/> (the default) to disable caching.
    /// </summary>
    public TimeSpan CacheDuration
    {
        get => _cacheDuration;
        set { ThrowIfSealed(); _cacheDuration = value; }
    }

    /// <summary>
    /// Gets or sets the default timeout applied to individual health checks
    /// that do not specify their own timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan DefaultTimeout
    {
        get => _defaultTimeout;
        set { ThrowIfSealed(); _defaultTimeout = value; }
    }

    /// <summary>
    /// Gets or sets the URL path for the liveness probe endpoint. Defaults to <c>/health/live</c>.
    /// </summary>
    public string LivenessPath
    {
        get => _livenessPath;
        set { ThrowIfSealed(); _livenessPath = value; }
    }

    /// <summary>
    /// Gets or sets the URL path for the readiness probe endpoint. Defaults to <c>/health/ready</c>.
    /// </summary>
    public string ReadinessPath
    {
        get => _readinessPath;
        set { ThrowIfSealed(); _readinessPath = value; }
    }

    /// <summary>
    /// Gets the list of registered health check registrations.
    /// </summary>
    internal List<HealthCheckRegistration> Registrations { get; } = new();

    /// <summary>
    /// Gets the list of check types that need to be registered in DI.
    /// </summary>
    internal List<Type> CheckTypes { get; } = new();

    /// <summary>
    /// Gets or sets whether a startup signal is required.
    /// </summary>
    internal bool RequiresStartupSignal { get; set; }

    /// <summary>
    /// Registers a health check instance with the specified name.
    /// </summary>
    /// <param name="name">A unique name for the health check.</param>
    /// <param name="instance">The health check instance.</param>
    /// <param name="failureStatus">The status to report on unhandled exceptions.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c> if not specified.</param>
    /// <param name="timeout">An optional per-check timeout override.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Options have been sealed after service registration.</exception>
    public HealthKitOptions AddCheck(
        string name,
        IHealthCheck instance,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return AddCheck(name, _ => instance, failureStatus, tags, timeout);
    }

    /// <summary>
    /// Registers a health check using a factory delegate resolved from the service provider.
    /// </summary>
    /// <param name="name">A unique name for the health check.</param>
    /// <param name="factory">A factory that receives <see cref="IServiceProvider"/> and returns an <see cref="IHealthCheck"/>.</param>
    /// <param name="failureStatus">The status to report on unhandled exceptions.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c> if not specified.</param>
    /// <param name="timeout">An optional per-check timeout override.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Options have been sealed after service registration.</exception>
    public HealthKitOptions AddCheck(
        string name,
        Func<IServiceProvider, IHealthCheck> factory,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ThrowIfSealed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);

        var tagSet = CreateTagSet(tags);
        Registrations.Add(new HealthCheckRegistration(name, factory, failureStatus, tagSet, timeout));
        return this;
    }

    /// <summary>
    /// Registers a health check type that is resolved from the service provider.
    /// The type is automatically registered in DI as transient if not already registered.
    /// </summary>
    /// <typeparam name="T">The health check implementation type.</typeparam>
    /// <param name="name">A unique name for the health check.</param>
    /// <param name="failureStatus">The status to report on unhandled exceptions.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c> if not specified.</param>
    /// <param name="timeout">An optional per-check timeout override.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Options have been sealed after service registration.</exception>
    public HealthKitOptions AddCheck<T>(
        string name,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null) where T : class, IHealthCheck
    {
        ThrowIfSealed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        CheckTypes.Add(typeof(T));
        var tagSet = CreateTagSet(tags);
        Registrations.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredService<T>(),
            failureStatus,
            tagSet,
            timeout));
        return this;
    }

    /// <summary>
    /// Registers an inline health check using a delegate.
    /// </summary>
    /// <param name="name">A unique name for the health check.</param>
    /// <param name="check">The delegate to execute as the health check.</param>
    /// <param name="failureStatus">The status to report on unhandled exceptions.</param>
    /// <param name="tags">Tags for endpoint filtering. Defaults to <c>"ready"</c> if not specified.</param>
    /// <param name="timeout">An optional per-check timeout override.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Options have been sealed after service registration.</exception>
    public HealthKitOptions AddCheck(
        string name,
        Func<CancellationToken, ValueTask<HealthCheckResult>> check,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(check);
        var wrapper = new Checks.DelegateHealthCheck(check);
        return AddCheck(name, _ => wrapper, failureStatus, tags, timeout);
    }

    /// <summary>
    /// Gets the pre-filtered list of registrations for the liveness endpoint.
    /// </summary>
    internal IReadOnlyList<HealthCheckRegistration> GetLiveRegistrations()
    {
        if (_liveRegistrations is not null) return _liveRegistrations;
        _liveRegistrations = FilterByTag(HealthCheckTags.Live);
        return _liveRegistrations;
    }

    /// <summary>
    /// Gets the pre-filtered list of registrations for the readiness endpoint.
    /// </summary>
    internal IReadOnlyList<HealthCheckRegistration> GetReadyRegistrations()
    {
        if (_readyRegistrations is not null) return _readyRegistrations;
        _readyRegistrations = FilterByTag(HealthCheckTags.Ready);
        return _readyRegistrations;
    }

    /// <summary>
    /// Seals the options to prevent further modifications.
    /// Called during service registration.
    /// </summary>
    internal void Seal() => _sealed = true;

    private IReadOnlyList<HealthCheckRegistration> FilterByTag(string tag)
    {
        var list = new List<HealthCheckRegistration>();
        for (int i = 0; i < Registrations.Count; i++)
        {
            if (Registrations[i].Tags.Contains(tag))
            {
                list.Add(Registrations[i]);
            }
        }
        return list;
    }

    private static HashSet<string> CreateTagSet(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return new HashSet<string>(1, StringComparer.Ordinal) { HealthCheckTags.Ready };
        }

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                set.Add(tag);
            }
        }

        if (set.Count == 0)
        {
            set.Add(HealthCheckTags.Ready);
        }

        return set;
    }

    private void ThrowIfSealed()
    {
        if (_sealed)
        {
            throw new InvalidOperationException(
                "HealthKitOptions cannot be modified after service registration is complete.");
        }
    }
}
