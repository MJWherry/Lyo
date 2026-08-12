using Lyo.Exceptions.Models;

namespace Lyo.Ftp.Client.Tests;

public class FtpClientOptionsTests
{
    [Fact]
    public void Validate_MissingPassword_Throws()
    {
        var o = ValidBase();
        o.Password = null;
        Assert.ThrowsAny<Exception>(() => o.Validate());
    }

    [Fact]
    public void Validate_InvalidPort_Throws()
    {
        var o = ValidBase();
        o.Port = 0;
        Assert.Throws<InvalidFormatException>(() => o.Validate());
    }

    [Fact]
    public void Validate_MaxPooledClientsBelowOne_Throws()
    {
        var o = ValidBase();
        o.MaxPooledClients = 0;
        Assert.Throws<InvalidFormatException>(() => o.Validate());
    }

    [Fact]
    public void ResolvePath_Escape_Throws()
    {
        using var client = new FtpClient(ValidBase());
        Assert.Throws<InvalidFormatException>(() => client.ResolvePath("../secret"));
    }

    [Fact]
    public void ResolvePath_Relative_JoinsRoot()
    {
        var o = ValidBase();
        o.RootRemoteDirectory = "/data/lyo";
        using var client = new FtpClient(o);
        Assert.Equal("/data/lyo/a/b", client.ResolvePath("a/b"));
    }

    private static FtpClientOptions ValidBase() => new() {
        Host = "localhost",
        Username = "foo",
        Password = "pass",
        RootRemoteDirectory = "/data/lyo",
        EncryptionMode = FtpEncryptionMode.None
    };
}
