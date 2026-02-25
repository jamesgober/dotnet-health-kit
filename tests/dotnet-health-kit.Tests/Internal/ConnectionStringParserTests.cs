using JG.HealthKit.Internal;

namespace JG.HealthKit.Tests.Internal;

public sealed class ConnectionStringParserTests
{
    [Theory]
    [InlineData("Server=localhost;Database=mydb;", "localhost", 1433)]
    [InlineData("Server=myserver,1434;Database=mydb;", "myserver", 1434)]
    [InlineData("Server=tcp:myserver.database.windows.net,1433;Database=mydb;", "myserver.database.windows.net", 1433)]
    [InlineData("Data Source=myserver;Initial Catalog=mydb;", "myserver", 1433)]
    [InlineData("Server=myserver\\SQLEXPRESS;Database=mydb;", "myserver", 1433)]
    [InlineData("server=localhost;database=test;", "localhost", 1433)]
    [InlineData("Addr=10.0.0.1;Database=mydb;", "10.0.0.1", 1433)]
    [InlineData("Address=10.0.0.1,1444;Database=mydb;", "10.0.0.1", 1444)]
    [InlineData("Network Address=10.0.0.1;Database=mydb;", "10.0.0.1", 1433)]
    [InlineData("Server=tcp:host;Database=db;", "host", 1433)]
    public void Parse_ValidConnectionString_ExtractsHostAndPort(
        string connectionString, string expectedHost, int expectedPort)
    {
        var (host, port) = ConnectionStringParser.Parse(connectionString);

        host.Should().Be(expectedHost);
        port.Should().Be(expectedPort);
    }

    [Fact]
    public void Parse_NullConnectionString_ThrowsArgumentException()
    {
        var act = () => ConnectionStringParser.Parse(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_EmptyConnectionString_ThrowsArgumentException()
    {
        var act = () => ConnectionStringParser.Parse("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_MissingServerKey_ThrowsArgumentException()
    {
        var act = () => ConnectionStringParser.Parse("Database=mydb;User=admin;");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Server*Data Source*");
    }

    [Fact]
    public void Parse_WhitespaceAroundValues_TrimsCorrectly()
    {
        var (host, port) = ConnectionStringParser.Parse("  Server = localhost , 1434 ;Database=mydb;");

        host.Should().Be("localhost");
        port.Should().Be(1434);
    }

    [Fact]
    public void Parse_InvalidPort_DefaultsTo1433()
    {
        var (host, port) = ConnectionStringParser.Parse("Server=localhost,abc;Database=mydb;");

        host.Should().Be("localhost");
        port.Should().Be(1433);
    }
}
