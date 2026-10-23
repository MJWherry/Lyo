using Lyo.Web.Components.Catalog;

namespace Lyo.Web.Components.Tests;

public sealed class CatalogLoadGateTests
{
    [Fact]
    public void Begin_SameKey_IsCurrentRequest()
    {
        using var gate = new CatalogLoadGate();
        var key = CatalogLoadGate.Key("/catalog", "Lyo.Cache");
        Assert.False(gate.IsCurrentRequest(key));
        var (requestId, _) = gate.Begin(key);
        Assert.True(gate.IsCurrentRequest(key));
        Assert.True(gate.IsCurrent(requestId));
        Assert.False(gate.IsCurrentRequest(CatalogLoadGate.Key("/catalog", "Lyo.Email")));
    }

    [Fact]
    public void Begin_NewerKey_CancelsThePreviousTokenAndDropsItsId()
    {
        using var gate = new CatalogLoadGate();
        var first = gate.Begin(CatalogLoadGate.Key("/catalog", "Lyo.Cache"));
        var second = gate.Begin(CatalogLoadGate.Key("/catalog", "Lyo.Email"));
        Assert.True(first.Token.IsCancellationRequested);
        Assert.False(second.Token.IsCancellationRequested);
        Assert.False(gate.IsCurrent(first.RequestId));
        Assert.True(gate.IsCurrent(second.RequestId));
        Assert.True(gate.IsCurrentRequest(CatalogLoadGate.Key("/catalog", "Lyo.Email")));
    }

    [Fact]
    public void Dispose_DropsTheCurrentRequest()
    {
        var gate = new CatalogLoadGate();
        var started = gate.Begin(CatalogLoadGate.Key("/catalog", "Lyo.Cache"));
        gate.Dispose();
        Assert.True(started.Token.IsCancellationRequested);
        Assert.False(gate.IsCurrent(started.RequestId));
        Assert.False(gate.IsCurrentRequest(CatalogLoadGate.Key("/catalog", "Lyo.Cache")));
    }
}