using Lyo.Common.Enums;
using Lyo.Common.Records;

namespace Lyo.Common.Tests;

public class PortInfoTests
{
    [Fact]
    public void StaticRegistry_ContainsExpectedMetadata()
    {
        Assert.Equal(22, PortInfo.Ssh.Port);
        Assert.Equal("SSH", PortInfo.Ssh.Name);
        Assert.Equal(PortCategory.RemoteAccess, PortInfo.Ssh.Category);
        Assert.Contains("sftp", PortInfo.Ssh.Aliases);
        Assert.Equal(587, PortInfo.SmtpSubmission.Port);
        Assert.Equal(PortCategory.Mail, PortInfo.SmtpSubmission.Category);
        Assert.Equal(5672, PortInfo.Amqp.Port);
        Assert.Equal(PortCategory.Messaging, PortInfo.Amqp.Category);
        Assert.Equal(5432, PortInfo.Postgres.Port);
        Assert.Equal(PortCategory.Database, PortInfo.Postgres.Category);
        Assert.Equal(3310, PortInfo.ClamAv.Port);
        Assert.Equal(PortCategory.Security, PortInfo.ClamAv.Category);
    }

    [Fact]
    public void ByCategory_ReturnsMatchingPorts()
    {
        var mail = PortInfo.ByCategory(PortCategory.Mail).ToList();
        Assert.Contains(PortInfo.Smtp, mail);
        Assert.Contains(PortInfo.SmtpSubmission, mail);
        Assert.DoesNotContain(PortInfo.Ssh, mail);
    }

    [Theory]
    [InlineData(22, "SSH")]
    [InlineData(443, "HTTPS")]
    [InlineData(5432, "PostgreSQL")]
    [InlineData(15672, "RabbitMQ Management")]
    public void FromPort_ResolvesKnown(int port, string expectedName)
    {
        var info = PortInfo.FromPort(port);
        Assert.Equal(expectedName, info.Name);
        Assert.Equal(port, info.Port);
    }

    [Fact]
    public void FromPort_Unknown_ReturnsUnknown() => Assert.Equal(PortInfo.Unknown, PortInfo.FromPort(1));

    [Fact]
    public void TryFromPort_Known_ReturnsTrue()
    {
        Assert.True(PortInfo.TryFromPort(6379, out var info));
        Assert.Equal(PortInfo.Redis, info);
    }

    [Fact]
    public void TryFromPort_Unknown_ReturnsFalse() => Assert.False(PortInfo.TryFromPort(9999, out _));

    [Theory]
    [InlineData("ssh")]
    [InlineData("SFTP")]
    [InlineData("SSH")]
    public void FromName_ResolvesSshAliases(string name) => Assert.Equal(PortInfo.Ssh, PortInfo.FromName(name));

    [Theory]
    [InlineData("postgres")]
    [InlineData("postgresql")]
    [InlineData("pgsql")]
    public void FromName_ResolvesPostgresAliases(string name) => Assert.Equal(PortInfo.Postgres, PortInfo.FromName(name));

    [Fact]
    public void FromName_Blank_ReturnsUnknown() => Assert.Equal(PortInfo.Unknown, PortInfo.FromName("  "));

    [Fact]
    public void All_ExcludesUnknown()
    {
        Assert.DoesNotContain(PortInfo.Unknown, PortInfo.All);
        Assert.Contains(PortInfo.Ssh, PortInfo.All);
        Assert.Contains(PortInfo.Amqp, PortInfo.All);
    }

    [Fact]
    public void ImplicitInt_ReturnsPortNumber()
    {
        int port = PortInfo.Https;
        Assert.Equal(443, port);
    }

    [Fact]
    public void ToString_IncludesPortWhenKnown() => Assert.Equal("HTTPS (443)", PortInfo.Https.ToString());
}
