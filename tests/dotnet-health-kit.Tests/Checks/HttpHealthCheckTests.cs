namespace JG.HealthKit.Tests.Checks;

public sealed class HttpHealthCheckTests
{
    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        var act = () => new JG.HealthKit.Checks.HttpHealthCheck(
            null!, "test", new Uri("https://example.com"));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullClientName_ThrowsArgumentException()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var act = () => new JG.HealthKit.Checks.HttpHealthCheck(
            factory, null!, new Uri("https://example.com"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullUri_ThrowsArgumentNullException()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var act = () => new JG.HealthKit.Checks.HttpHealthCheck(
            factory, "test", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckAsync_SuccessStatusCode_ReturnsHealthy()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("test").Returns(client);

        var check = new JG.HealthKit.Checks.HttpHealthCheck(
            factory, "test", new Uri("https://example.com/health"));
        var context = CreateContext("http-dep");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("200");
    }

    [Fact]
    public async Task CheckAsync_ServerError_ReturnsUnhealthy()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("test").Returns(client);

        var check = new JG.HealthKit.Checks.HttpHealthCheck(
            factory, "test", new Uri("https://example.com/health"));
        var context = CreateContext("http-dep");

        var result = await check.CheckAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("500");
    }

    [Fact]
    public async Task CheckAsync_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("test").Returns(client);

        var check = new JG.HealthKit.Checks.HttpHealthCheck(
            factory, "test", new Uri("https://example.com/health"));
        var context = CreateContext("http-dep");

        var act = () => check.CheckAsync(context, cts.Token).AsTask();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static HealthCheckContext CreateContext(string name)
    {
        var reg = new HealthCheckRegistration(
            name,
            _ => Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            new HashSet<string>(StringComparer.Ordinal));
        return new HealthCheckContext(reg);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_response);
        }
    }
}
