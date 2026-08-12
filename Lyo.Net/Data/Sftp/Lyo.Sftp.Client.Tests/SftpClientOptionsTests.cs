using Lyo.Exceptions.Models;

namespace Lyo.Sftp.Client.Tests;

public class SftpClientOptionsTests
{
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
    public void Validate_MissingAuth_Throws()
    {
        var o = ValidBase();
        o.Password = null;
        Assert.Throws<InvalidFormatException>(() => o.Validate());
    }

    [Fact]
    public void Validate_AllowListWithoutFingerprints_Throws()
    {
        var o = ValidBase();
        o.AllowedHostKeyFingerprints.Clear();
        Assert.Throws<InvalidFormatException>(() => o.Validate());
    }

    [Fact]
    public void Validate_AcceptAny_DoesNotRequireFingerprints()
    {
        var o = ValidBase();
        o.HostKeyPolicy = SftpHostKeyPolicy.AcceptAny;
        o.AllowedHostKeyFingerprints.Clear();
        o.Validate();
    }

    [Fact]
    public void ResolvePath_Relative_JoinsRoot()
    {
        using var client = new SftpClient(AcceptAnyOptions());
        Assert.Equal("/data/lyo/a/b", client.ResolvePath("a/b"));
    }

    [Fact]
    public void ResolvePath_Escape_Throws()
    {
        using var client = new SftpClient(AcceptAnyOptions());
        Assert.Throws<InvalidFormatException>(() => client.ResolvePath("../secret"));
    }

    private static SftpClientOptions ValidBase() => new() {
        Host = "localhost",
        Username = "foo",
        Password = "pass",
        RootRemoteDirectory = "/data/lyo",
        HostKeyPolicy = SftpHostKeyPolicy.FingerprintAllowList,
        AllowedHostKeyFingerprints = ["SHA256:abc"]
    };

    private static SftpClientOptions AcceptAnyOptions() => new() {
        Host = "localhost",
        Username = "foo",
        Password = "pass",
        RootRemoteDirectory = "/data/lyo",
        HostKeyPolicy = SftpHostKeyPolicy.AcceptAny
    };
}
