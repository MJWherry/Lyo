using Lyo.Common.Enums;
using Lyo.Hashing.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Hashing.Tests;

public sealed class HashingDiRegistrationTests
{
    [Fact]
    public void AddLyoHashing_no_configure_resolves_Shared_instance()
    {
        var services = new ServiceCollection();
        services.AddLyoHashing();
        using var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IHashingService>();
        Assert.Same(HashingService.Shared, svc);
        var digest = svc.Hash(ContentDigestAlgorithm.Sha256, "di"u8.ToArray());
        Assert.Equal(HexEncoding.ToHexString(digest), svc.ToHex(digest));
    }

    [Fact]
    public void AddLyoHashing_with_configure_uses_custom_options()
    {
        var services = new ServiceCollection();
        services.AddLyoHashing(o => o.DefaultHexLetterCase = TextLetterCase.Lower);
        using var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IHashingService>();
        Assert.NotSame(HashingService.Shared, svc);
        var digest = svc.Hash(ContentDigestAlgorithm.Sha256, "x"u8.ToArray());
        Assert.Equal(HexEncoding.ToHexString(digest, TextLetterCase.Lower), svc.ToHex(digest));
    }

    [Fact]
    public void AddLyoHashing_with_options_instance_registers_both()
    {
        var opts = new HashingOptions { DefaultHexLetterCase = TextLetterCase.Lower };
        var services = new ServiceCollection();
        services.AddLyoHashing(opts);
        using var sp = services.BuildServiceProvider();
        Assert.Same(opts, sp.GetRequiredService<HashingOptions>());
        var hex = sp.GetRequiredService<IHashingService>().ToHex(new byte[] { 1 });
        Assert.Equal("01", hex);
    }
}