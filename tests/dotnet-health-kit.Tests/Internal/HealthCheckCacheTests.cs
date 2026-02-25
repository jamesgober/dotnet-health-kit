using JG.HealthKit.Internal;

namespace JG.HealthKit.Tests.Internal;

public sealed class HealthCheckCacheTests : IDisposable
{
    private readonly HealthCheckCache _cache;
    private int _factoryCallCount;

    public HealthCheckCacheTests()
    {
        _cache = new HealthCheckCache(TimeSpan.FromSeconds(10));
    }

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task GetOrRunAsync_FirstCall_InvokesFactory()
    {
        var report = await _cache.GetOrRunAsync("key", Factory, CancellationToken.None);

        _factoryCallCount.Should().Be(1);
        report.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GetOrRunAsync_CachedResult_DoesNotInvokeFactory()
    {
        await _cache.GetOrRunAsync("key", Factory, CancellationToken.None);
        await _cache.GetOrRunAsync("key", Factory, CancellationToken.None);

        _factoryCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrRunAsync_DifferentKeys_InvokesFactoryForEach()
    {
        await _cache.GetOrRunAsync("a", Factory, CancellationToken.None);
        await _cache.GetOrRunAsync("b", Factory, CancellationToken.None);

        _factoryCallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOrRunAsync_ExpiredCache_InvokesFactoryAgain()
    {
        using var shortCache = new HealthCheckCache(TimeSpan.FromMilliseconds(10));
        int calls = 0;

        await shortCache.GetOrRunAsync("key", _ =>
        {
            Interlocked.Increment(ref calls);
            return new ValueTask<HealthReport>(HealthReport.Empty);
        }, CancellationToken.None);

        // Wait for expiration
        await Task.Delay(50);

        await shortCache.GetOrRunAsync("key", _ =>
        {
            Interlocked.Increment(ref calls);
            return new ValueTask<HealthReport>(HealthReport.Empty);
        }, CancellationToken.None);

        calls.Should().Be(2);
    }

    [Fact]
    public async Task GetOrRunAsync_ZeroCacheDuration_AlwaysInvokesFactory()
    {
        using var noCache = new HealthCheckCache(TimeSpan.Zero);
        int calls = 0;

        for (int i = 0; i < 3; i++)
        {
            await noCache.GetOrRunAsync("key", _ =>
            {
                Interlocked.Increment(ref calls);
                return new ValueTask<HealthReport>(HealthReport.Empty);
            }, CancellationToken.None);
        }

        calls.Should().Be(3);
    }

    [Fact]
    public async Task GetOrRunAsync_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // When the semaphore wait is cancelled
        // First fill cache so next call doesn't hit fast path
        using var blockingCache = new HealthCheckCache(TimeSpan.FromSeconds(10));

        var act = () => blockingCache.GetOrRunAsync("key", async ct =>
        {
            ct.ThrowIfCancellationRequested();
            return HealthReport.Empty;
        }, cts.Token).AsTask();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var cache = new HealthCheckCache(TimeSpan.FromSeconds(1));
        cache.Dispose();

        var act = () => cache.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetOrRunAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var cache = new HealthCheckCache(TimeSpan.FromSeconds(1));
        cache.Dispose();

        var act = () => cache.GetOrRunAsync("key", Factory, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    private ValueTask<HealthReport> Factory(CancellationToken ct)
    {
        Interlocked.Increment(ref _factoryCallCount);
        return new ValueTask<HealthReport>(HealthReport.Empty);
    }
}
