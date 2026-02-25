namespace JG.HealthKit.Tests.Extensions;

public sealed class HealthKitOptionsExtensionsTests
{
    [Fact]
    public void AddSqlServer_ValidConnectionString_RegistersTcpCheck()
    {
        var options = new HealthKitOptions();

        options.AddSqlServer("Server=localhost;Database=mydb;");

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("sqlserver:localhost:1433");
    }

    [Fact]
    public void AddSqlServer_WithCustomPort_ParsesPort()
    {
        var options = new HealthKitOptions();

        options.AddSqlServer("Server=myserver,1434;Database=mydb;");

        options.Registrations[0].Name.Should().Be("sqlserver:myserver:1434");
    }

    [Fact]
    public void AddSqlServer_WithCustomName_UsesProvidedName()
    {
        var options = new HealthKitOptions();

        options.AddSqlServer("Server=localhost;Database=mydb;", name: "primary-db");

        options.Registrations[0].Name.Should().Be("primary-db");
    }

    [Fact]
    public void AddSqlServer_NullConnectionString_ThrowsArgumentException()
    {
        var options = new HealthKitOptions();

        var act = () => options.AddSqlServer(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddHttpDependency_ValidUrl_RegistersHttpCheck()
    {
        var options = new HealthKitOptions();

        options.AddHttpDependency("api", "https://api.example.com/health");

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("api");
    }

    [Fact]
    public void AddHttpDependency_InvalidUrl_ThrowsArgumentException()
    {
        var options = new HealthKitOptions();

        var act = () => options.AddHttpDependency("api", "not-a-url");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("url");
    }

    [Fact]
    public void AddHttpDependency_NullName_ThrowsArgumentException()
    {
        var options = new HealthKitOptions();

        var act = () => options.AddHttpDependency(null!, "https://example.com");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddDiskSpace_ValidThreshold_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddDiskSpace(threshold: 500_000_000);

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("disk-space");
    }

    [Fact]
    public void AddDiskSpace_CustomName_UsesProvidedName()
    {
        var options = new HealthKitOptions();

        options.AddDiskSpace(threshold: 100, name: "data-volume");

        options.Registrations[0].Name.Should().Be("data-volume");
    }

    [Fact]
    public void AddMemoryCheck_DefaultThreshold_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddMemoryCheck();

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("memory");
    }

    [Fact]
    public void AddMemoryCheck_CustomThreshold_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddMemoryCheck(unhealthyThreshold: 2_147_483_648);

        options.Registrations.Should().HaveCount(1);
    }

    [Fact]
    public void AddTcp_ValidHostPort_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddTcp("redis", "localhost", 6379);

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("redis");
    }

    [Fact]
    public void AddDatabaseCheck_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddDatabaseCheck("mydb", () => throw new NotImplementedException(), "SELECT 1");

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("mydb");
    }

    [Fact]
    public void AddDatabaseCheck_NullFactory_ThrowsArgumentNullException()
    {
        var options = new HealthKitOptions();

        var act = () => options.AddDatabaseCheck("mydb", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStartupCheck_RegistersCheck()
    {
        var options = new HealthKitOptions();

        options.AddStartupCheck();

        options.Registrations.Should().HaveCount(1);
        options.Registrations[0].Name.Should().Be("startup");
        options.RequiresStartupSignal.Should().BeTrue();
    }

    [Fact]
    public void AddStartupCheck_DefaultTags_IncludesReady()
    {
        var options = new HealthKitOptions();

        options.AddStartupCheck();

        options.Registrations[0].Tags.Should().Contain(HealthCheckTags.Ready);
    }

    [Fact]
    public void FluentChaining_Works()
    {
        var options = new HealthKitOptions();

        var result = options
            .AddDiskSpace(500_000_000)
            .AddMemoryCheck()
            .AddTcp("redis", "localhost", 6379);

        result.Should().BeSameAs(options);
        options.Registrations.Should().HaveCount(3);
    }
}
