using Lyo.Algorithms;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Microsoft.Extensions.DependencyInjection;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Algorithms.Tests;

/// <summary>Catalog behavior shared by memory, Postgres, and SQLite. Each test uses its own key so a shared database stays isolated.</summary>
public abstract class AlgorithmCatalogContractTests
{
    /// <summary>Root provider with <c>AddAlgorithms</c> and one <see cref="IAlgorithmDefinitionStore" />.</summary>
    protected abstract IServiceProvider Services { get; }

    [Fact]
    public async Task SaveRevision_LabelsAndRevert_KeepPinsAndLastGood()
    {
        var catalog = Services.GetRequiredService<IAlgorithmCatalog>();
        var registry = Services.GetRequiredService<IAlgorithmRegistry>();
        var key = "attack." + LyoGuid.CreateV7().ToString("N");
        var bag = new AlgorithmContextBag();
        bag.Set("Atk", 10d);
        var ct = TestCt;

        var first = await catalog.SaveRevisionAsync(key, new AlgorithmRevisionReq { Kind = AlgorithmKind.Expression, Source = "Atk * 2" }, ct);
        Assert.True(first.IsSuccess);
        Assert.True((await catalog.SetLabelAsync(key, "game.1.0", new AlgorithmLabelReq { Revision = 1, IsMutable = false }, ct)).IsSuccess);
        Assert.True((await catalog.SetLabelAsync(key, "live", new AlgorithmLabelReq { Revision = 1, IsMutable = true }, ct)).IsSuccess);

        Assert.True((await catalog.SaveRevisionAsync(key, new AlgorithmRevisionReq { Kind = AlgorithmKind.Expression, Source = "Atk * 3" }, ct)).IsSuccess);
        Assert.True((await catalog.SetLabelAsync(key, "live", new AlgorithmLabelReq { Revision = 2, IsMutable = true }, ct)).IsSuccess);

        Assert.Equal(30d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(key), bag, ct));
        Assert.Equal(30d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Label(key, "live"), bag, ct));
        Assert.Equal(20d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Revision(key, 1), bag, ct));
        Assert.Equal(20d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Label(key, "game.1.0"), bag, ct));

        var broken = await catalog.SaveRevisionAsync(key, new AlgorithmRevisionReq { Kind = AlgorithmKind.Expression, Source = "Atk *" }, ct);
        Assert.False(broken.IsSuccess);
        Assert.Equal(30d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(key), bag, ct));

        var reverted = await catalog.RevertToRevisionAsync(key, new AlgorithmRevertReq { Revision = 1 }, ct);
        Assert.True(reverted.IsSuccess);
        Assert.Equal(3, reverted.Data!.CurrentRevision);
        Assert.Equal(20d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(key), bag, ct));
        Assert.Equal(20d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Revision(key, 1), bag, ct));
        Assert.Equal(30d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Revision(key, 2), bag, ct));

        var moved = await catalog.SetLabelAsync(key, "game.1.0", new AlgorithmLabelReq { Revision = 2, IsMutable = false }, ct);
        Assert.False(moved.IsSuccess);
        Assert.Equal(20d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Label(key, "game.1.0"), bag, ct));
    }

    [Fact]
    public async Task SaveRevision_DiscreteMapPayload_RoundTrips()
    {
        var catalog = Services.GetRequiredService<IAlgorithmCatalog>();
        var registry = Services.GetRequiredService<IAlgorithmRegistry>();
        var key = "fish.type." + LyoGuid.CreateV7().ToString("N");
        var payload = """{"input":"FishType","entries":[{"key":"bass","points":10},{"key":"trout","points":4}]}""";
        var ct = TestCt;
        var saved = await catalog.SaveRevisionAsync(key, new AlgorithmRevisionReq { Kind = AlgorithmKind.DiscreteMap, Source = "", Payload = payload }, ct);
        Assert.True(saved.IsSuccess);
        var revision = Assert.Single(await catalog.GetRevisionsAsync(key, ct));
        Assert.Contains("bass", revision.Payload, StringComparison.Ordinal);
        var bag = new AlgorithmContextBag();
        bag.Set("FishType", "bass");
        Assert.Equal(10d, await registry.ExecuteAsync<AlgorithmContextBag, double>(key, bag, ct));
    }
}
