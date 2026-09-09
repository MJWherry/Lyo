using Lyo.Web.Primitives;

namespace Lyo.Web.Primitives.Tests;

public sealed class LyoStatusTextTests
{
    [Theory]
    [InlineData("Partially_Delivered", "partially delivered")]
    [InlineData("partially-delivered", "partially delivered")]
    [InlineData("Partially Delivered", "partially delivered")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void Normalize_MixedSeparators_CollapsesToOneKey(string? status, string expected)
        => Assert.Equal(expected, LyoStatusText.Normalize(status));

    [Fact]
    public void Humanize_NormalizedKey_TitleCases()
        => Assert.Equal("Partially Delivered", LyoStatusText.Humanize("partially_delivered"));
}
