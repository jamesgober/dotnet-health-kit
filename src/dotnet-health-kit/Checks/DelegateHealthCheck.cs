namespace JG.HealthKit.Checks;

/// <summary>
/// Wraps a delegate as an <see cref="IHealthCheck"/> implementation.
/// </summary>
internal sealed class DelegateHealthCheck : IHealthCheck
{
    private readonly Func<CancellationToken, ValueTask<HealthCheckResult>> _check;

    /// <summary>
    /// Initializes a new instance of <see cref="DelegateHealthCheck"/>.
    /// </summary>
    /// <param name="check">The delegate to invoke when the check executes.</param>
    public DelegateHealthCheck(Func<CancellationToken, ValueTask<HealthCheckResult>> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _check = check;
    }

    /// <inheritdoc />
    public ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
        => _check(cancellationToken);
}
