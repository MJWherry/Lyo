namespace Lyo.FileManager.Tests;

public class AwsSharedConfigTests
{
    [Fact]
    public void Read_MergesTheConfigRegionOntoTheCredentialsProfile()
    {
        const string credentials = """
                                   [default]
                                   aws_access_key_id = AKIADEFAULT
                                   aws_secret_access_key = default-secret

                                   [dev]
                                   aws_access_key_id = AKIADEV
                                   aws_secret_access_key = dev-secret
                                   """;

        const string config = """
                              [default]
                              region = us-east-1

                              [profile dev]
                              region = eu-west-1
                              """;

        var profiles = AwsSharedConfig.Read(credentials, config);
        var def = Assert.Single(profiles, profile => profile.Name == "default");
        Assert.Equal("AKIADEFAULT", def.AccessKeyId);
        Assert.Equal("us-east-1", def.Region);
        var dev = Assert.Single(profiles, profile => profile.Name == "dev");
        Assert.Equal("AKIADEV", dev.AccessKeyId);
        Assert.Equal("dev-secret", dev.SecretAccessKey);
        Assert.Equal("eu-west-1", dev.Region);
    }

    [Fact]
    public void SetAuth_ChangesOnlyTheActiveServer()
    {
        var session = new FileManagerSession();
        var first = session.AddRemote("a", "http://a");
        var second = session.AddRemote("b", "http://b");
        session.Select(first.Id);
        session.SetAuth(FileManagerAuthKind.Bearer, "token-a");
        session.Select(second.Id);
        session.SetAuth(FileManagerAuthKind.ApiKey, "key-b");
        Assert.Equal(FileManagerAuthKind.Bearer, first.AuthKind);
        Assert.Equal("token-a", first.Secret);
        Assert.Equal(FileManagerAuthKind.ApiKey, second.AuthKind);
        Assert.Equal("key-b", second.Secret);
    }

    [Fact]
    public void HasKeyStore_RequiresAPassphraseALocalKeyOrAws()
    {
        Assert.False(new FileManagerServer().HasKeyStore());
        Assert.True(new FileManagerServer { Passphrase = "secret" }.HasKeyStore());
        Assert.True(new FileManagerServer { Keys = [new() { Id = "k", Secret = "s" }] }.HasKeyStore());
        Assert.True(new FileManagerServer { KeyStoreKind = FileManagerKeyStoreKind.Aws }.HasKeyStore());
    }
}