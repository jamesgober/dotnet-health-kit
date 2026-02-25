using System.Collections.Concurrent;

namespace JG.HealthKit.Internal;

/// <summary>
/// Thread-safe TTL cache for health check reports, preventing check storms under load.
/// </summary>
/// <remarks>
/// Uses a double-check locking pattern with <see cref="SemaphoreSlim"/> to ensure
/// only one execution occurs per cache key when the cache expires. The fast path
/// (cache hit) is lock-free. This type is thread-safe and designed for singleton use.
/// </remarks>
internal sealed class HealthCheckCache : IDisposable
{
    private readonly TimeSpan _cacheDuration;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="HealthCheckCache"/>.
    /// </summary>
    /// <param name="cacheDuration">
    /// The duration to cache results. Use <see cref="TimeSpan.Zero"/> to disable caching.
    /// </param>
    public HealthCheckCache(TimeSpan cacheDuration)
    {
        _cacheDuration = cacheDuration;
    }

    /// <summary>
    /// Gets a cached report or executes the factory to produce a fresh one.
    /// </summary>
    /// <param name="key">The cache key (e.g., "live" or "ready").</param>
    /// <param name="factory">The factory to produce a report on cache miss.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The cached or freshly produced <see cref="HealthReport"/>.</returns>
    public async ValueTask<HealthReport> GetOrRunAsync(
        string key,
        Func<CancellationToken, ValueTask<HealthReport>> factory,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Caching disabled — always run fresh
        if (_cacheDuration <= TimeSpan.Zero)
        {
            return await factory(cancellationToken).ConfigureAwait(false);
        }

        // Fast path: cache hit (lock-free)
        if (_cache.TryGetValue(key, out var entry) && Environment.TickCount64 < entry.ExpiresAtTicks)
        {
            return entry.Report;
        }

        // Slow path: acquire semaphore to prevent thundering herd
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring the lock
            if (_cache.TryGetValue(key, out entry) && Environment.TickCount64 < entry.ExpiresAtTicks)
            {
                return entry.Report;
            }

            var report = await factory(cancellationToken).ConfigureAwait(false);
            long expiresAt = Environment.TickCount64 + (long)_cacheDuration.TotalMilliseconds;
            _cache[key] = new CacheEntry(report, expiresAt);
            return report;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }

    private readonly struct CacheEntry
    {
        public readonly HealthReport Report;
        public readonly long ExpiresAtTicks;

        public CacheEntry(HealthReport report, long expiresAtTicks)
        {
            Report = report;
            ExpiresAtTicks = expiresAtTicks;
        }
    }
}
