using Lyo.Authentication.Options;
using Lyo.Authentication.Services.Jwt;
using Lyo.Keystore;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Lyo.Authentication.Tests;

public class Ed25519KeyBootstrapperTests
{
    private CancellationToken TCT => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StartAsync_WhenNoKey_GeneratesV1()
    {
        var keys = new LocalKeyStore();
        var bs = new Ed25519KeyBootstrapper(keys, MsOptions.Create(new LyoJwtOptions()), NullLogger<Ed25519KeyBootstrapper>.Instance);
        await bs.StartAsync(TCT);
        Assert.True(keys.HasKey("lyo-sig", "v1"));
        Assert.Equal("v1", keys.GetCurrentVersion("lyo-sig"));
        var current = keys.GetCurrentKey("lyo-sig");
        Assert.NotNull(current);
        Assert.Equal(32, current.Length);
    }

    [Fact]
    public async Task StartAsync_WhenAutoGenerateFalse_DoesNothing()
    {
        var keys = new LocalKeyStore();
        var bs = new Ed25519KeyBootstrapper(keys, MsOptions.Create(new LyoJwtOptions { AutoGenerateSigningKey = false }), NullLogger<Ed25519KeyBootstrapper>.Instance);
        await bs.StartAsync(TCT);
        Assert.False(keys.HasKey("lyo-sig"));
    }
}