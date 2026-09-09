using Lyo.Http.Client;
using Lyo.Http.Client.Session;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpHeaderBagTests
{
    [Fact]
    public void Set_ReplacesSameName_KeepsOrder()
    {
        var bag = new LyoHttpHeaderBag();
        bag.Set("Accept", "text/html");
        bag.Set("X-Test", "1");
        bag.Set("Accept", "application/json");
        Assert.Equal(2, bag.Items.Count);
        Assert.True(bag.TryGet("Accept", out var accept));
        Assert.Equal("application/json", accept);
    }

    [Fact]
    public void Set_DoesNotOverwriteCorrelation()
    {
        var bag = new LyoHttpHeaderBag();
        bag.Set("X-Correlation-Id", "abc");
        bag.Set("X-Correlation-Id", "xyz");
        Assert.True(bag.TryGet("X-Correlation-Id", out var value));
        Assert.Equal("abc", value);
    }
}

