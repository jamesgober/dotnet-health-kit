namespace JG.HealthKit.Tests;

public sealed class HealthKitOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new HealthKitOptions();

        options.CacheDuration.Should().Be(TimeSpan.Zero);
        options.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.LivenessPath.Should().Be("/health/live");
        options.ReadinessPath.Should().Be("/health/ready");
    }

    [Fact]
    public void AddCheck_Instance_RegistersCheck()
    {
        var options = new HealthKitOptions();
        var check = Substitute.For<IHealthCheck>();

        options.AddCheck("test", check);

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("test");
    }

    [Fact]
    public void AddCheck_NullName_ThrowsArgumentException()
    {
        var options = new HealthKitOptions();
        var check = Substitute.For<IHealthCheck>();

        var act = () => options.AddCheck(null!, check);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddCheck_NullInstance_ThrowsArgumentNullException()
    {
        var options = new HealthKitOptions();

        var act = () => options.AddCheck("test", (IHealthCheck)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCheck_Factory_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddCheck("test", _ => Substitute.For<IHealthCheck>());

        options.Registrations.Should().HaveCount(1);
    }

    [Fact]
    public void AddCheck_Delegate_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddCheck("test",
            _ => new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy()));

        options.Registrations.Should().HaveCount(1);
    }

    [Fact]
    public void AddCheck_DefaultTags_IncludesReady()
    {
        var options = new HealthKitOptions();
        var check = Substitute.For<IHealthCheck>();

        options.AddCheck("test", check);

        options.Registrations[0].Tags.Should().Contain(HealthCheckTags.Ready);
    }

    [Fact]
    public void AddCheck_ExplicitTags_UsesProvidedTags()
    {
        var options = new HealthKitOptions();
        var check = Substitute.For<IHealthCheck>();

        options.AddCheck("test", check, tags: new[] { "live", "ready" });

        options.Registrations[0].Tags.Should().Contain("live");
        options.Registrations[0].Tags.Should().Contain("ready");
    }

    [Fact]
    public void AddCheck_EmptyTags_DefaultsToReady()
    {
        var options = new HealthKitOptions();
        var check = Substitute.For<IHealthCheck>();

        options.AddCheck("test", check, tags: Array.Empty<string>());

        options.Registrations[0].Tags.Should().Contain(HealthCheckTags.Ready);
    }

    [Fact]
    public void AddCheck_WithTimeout_SetsTimeout()
    {
        var options = new HealthKitOptions();
        var check = Substitute.For<IHealthCheck>();
        var timeout = TimeSpan.FromSeconds(5);

        options.AddCheck("test", check, timeout: timeout);

        options.Registrations[0].Timeout.Should().Be(timeout);
    }

    [Fact]
    public void AddCheck_ReturnsSameInstance_ForChaining()
    {
        var options = new HealthKitOptions();
        var check = Substitute.For<IHealthCheck>();

        var result = options.AddCheck("test", check);

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void Seal_PreventsAdditionalRegistrations()
    {
        var options = new HealthKitOptions();
        options.Seal();

        var act = () => options.AddCheck("test", Substitute.For<IHealthCheck>());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetLiveRegistrations_FiltersCorrectly()
    {
        var options = new HealthKitOptions();
        options.AddCheck("live-only", Substitute.For<IHealthCheck>(),
            tags: new[] { HealthCheckTags.Live });
        options.AddCheck("ready-only", Substitute.For<IHealthCheck>(),
            tags: new[] { HealthCheckTags.Ready });
        options.AddCheck("both", Substitute.For<IHealthCheck>(),
            tags: new[] { HealthCheckTags.Live, HealthCheckTags.Ready });

        var live = options.GetLiveRegistrations();

        live.Should().HaveCount(2);
        live.Select(r => r.Name).Should().Contain("live-only");
        live.Select(r => r.Name).Should().Contain("both");
    }

    [Fact]
    public void GetReadyRegistrations_FiltersCorrectly()
    {
        var options = new HealthKitOptions();
        options.AddCheck("live-only", Substitute.For<IHealthCheck>(),
            tags: new[] { HealthCheckTags.Live });
        options.AddCheck("ready-only", Substitute.For<IHealthCheck>(),
            tags: new[] { HealthCheckTags.Ready });

        var ready = options.GetReadyRegistrations();

        ready.Should().HaveCount(1);
        ready[0].Name.Should().Be("ready-only");
    }

    [Fact]
    public void GetLiveRegistrations_CachesResult()
    {
        var options = new HealthKitOptions();
        options.AddCheck("test", Substitute.For<IHealthCheck>(),
            tags: new[] { HealthCheckTags.Live });

        var first = options.GetLiveRegistrations();
        var second = options.GetLiveRegistrations();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddCheckGeneric_TracksType()
    {
        var options = new HealthKitOptions();

        options.AddCheck<FakeHealthCheck>("test");

        options.CheckTypes.Should().Contain(typeof(FakeHealthCheck));
    }

    [Fact]
    public void Seal_PreventsPropertyModification_CacheDuration()
    {
        var options = new HealthKitOptions();
        options.Seal();

        var act = () => options.CacheDuration = TimeSpan.FromSeconds(5);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Seal_PreventsPropertyModification_DefaultTimeout()
    {
        var options = new HealthKitOptions();
        options.Seal();

        var act = () => options.DefaultTimeout = TimeSpan.FromSeconds(60);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Seal_PreventsPropertyModification_LivenessPath()
    {
        var options = new HealthKitOptions();
        options.Seal();

        var act = () => options.LivenessPath = "/alive";

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Seal_PreventsPropertyModification_ReadinessPath()
    {
        var options = new HealthKitOptions();
        options.Seal();

        var act = () => options.ReadinessPath = "/ready";

        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class FakeHealthCheck : IHealthCheck
    {
        public ValueTask<HealthCheckResult> CheckAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
            => new(HealthCheckResult.Healthy());
    }
}
