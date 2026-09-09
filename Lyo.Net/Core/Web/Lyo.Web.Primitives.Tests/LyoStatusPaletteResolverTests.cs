using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.Web.Primitives.Tests;

public sealed class LyoStatusPaletteResolverTests
{
    [Fact]
    public void Resolve_UnknownStatus_FallsBackToNeutralChip()
    {
        var spec = LyoStatusPaletteResolver.Default.Resolve("not-a-real-status");
        Assert.Equal("not-a-real-status", spec.Label);
        Assert.Equal(Color.Default, spec.Color);
    }

    [Fact]
    public void Resolve_SharedVocabulary_ColoursSuccess()
    {
        var spec = LyoStatusPaletteResolver.Default.Resolve("succeeded");
        Assert.Equal(Color.Success, spec.Color);
    }

    [Fact]
    public void Resolve_NamedPaletteWinsOverDefault()
    {
        var resolver = new LyoStatusPaletteResolver([new OverridePalette()]);
        var spec = resolver.Resolve("running", "job");
        Assert.Equal(Color.Warning, spec.Color);
    }

    private sealed class OverridePalette : ILyoStatusPalette
    {
        public string Name => "job";

        public LyoChipSpec? Resolve(string? status)
            => LyoStatusText.Normalize(status) == "running" ? new LyoChipSpec("Running", Color.Warning) : null;
    }
}
