namespace JG.HealthKit.Internal;

/// <summary>
/// Thread-safe implementation of <see cref="IStartupHealthSignal"/>.
/// </summary>
internal sealed class StartupHealthSignal : IStartupHealthSignal
{
    private volatile bool _isReady;

    /// <inheritdoc />
    public bool IsReady => _isReady;

    /// <inheritdoc />
    public void MarkReady() => _isReady = true;
}
