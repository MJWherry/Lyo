using Lyo.Common.Enums;
using Lyo.TextEncoding.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.TextEncoding.Tests;

public sealed class EncodingDiRegistrationTests
{
    [Fact]
    public void AddLyoTextEncoding_ResolvesBothServices()
    {
        var services = new ServiceCollection();
        services.AddLyoTextEncoding();
        using var sp = services.BuildServiceProvider();
        Assert.Same(BinaryEncodingService.Shared, sp.GetRequiredService<IBinaryEncodingService>());
        Assert.Same(CharsetEncodingService.Shared, sp.GetRequiredService<ICharsetEncodingService>());
    }

    [Fact]
    public void AddLyoBinaryEncoding_WithOptions_UsesConfiguredHexCase()
    {
        var services = new ServiceCollection();
        services.AddLyoBinaryEncoding(o => o.DefaultHexLetterCase = TextLetterCase.Lower);
        using var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IBinaryEncodingService>();
        Assert.Equal("ab", svc.Encode(BinaryEncodingKind.Hex, [0xAB]));
    }

    [Fact]
    public void AddLyoCharsetEncoding_ResolvesService()
    {
        var services = new ServiceCollection();
        services.AddLyoCharsetEncoding();
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<ICharsetEncodingService>());
    }
}