namespace JG.HealthKit.Checks;

/// <summary>
/// Health check that verifies an HTTP dependency is reachable and returning success status codes.
/// </summary>
/// <remarks>
/// Uses <see cref="IHttpClientFactory"/> for proper <see cref="HttpClient"/> lifecycle management.
/// Only reads response headers (does not buffer the body) to minimize overhead. Thread-safe.
/// </remarks>
internal sealed class HttpHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName;
    private readonly Uri _uri;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpHealthCheck"/>.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
    /// <param name="clientName">The named HTTP client identifier.</param>
    /// <param name="uri">The URI to send a GET request to.</param>
    public HttpHealthCheck(IHttpClientFactory httpClientFactory, string clientName, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentNullException.ThrowIfNull(uri);

        _httpClientFactory = httpClientFactory;
        _clientName = clientName;
        _uri = uri;
    }

    /// <inheritdoc />
    public async ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient(_clientName);
        using var response = await client.GetAsync(
            _uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            return HealthCheckResult.Healthy(
                $"HTTP {statusCode} from {_uri.Host}");
        }

        return HealthCheckResult.Unhealthy(
            $"HTTP {statusCode} from {_uri.Host}");
    }
}
