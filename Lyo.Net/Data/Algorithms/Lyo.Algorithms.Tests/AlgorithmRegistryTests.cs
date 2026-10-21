using Lyo.Algorithms.Models;
using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Tests;

public sealed class MemoryAlgorithmCatalogTests : AlgorithmCatalogContractTests
{
    private readonly ServiceProvider _services = new ServiceCollection().AddAlgorithms().AddMemoryAlgorithmStore().BuildServiceProvider();

    protected override IServiceProvider Services => _services;
}

public sealed class ExpressionCompilerTests
{
    private readonly ExpressionCompiler _compiler = new();

    [Fact]
    public void Compile_TypedExpression_SecondCallReturnsSameInstance()
    {
        var first = _compiler.Compile<CombatStats, decimal>("Atk * 2m - Def");
        var second = _compiler.Compile<CombatStats, decimal>("Atk * 2m - Def");
        Assert.True(first.IsSuccess);
        Assert.Same(first.Data, second.Data);
        Assert.Equal(17m, first.Data!.Execute(new(10m, 3m)));
    }

    [Fact]
    public void Compile_BagExpression_UsesDictionaryValues()
    {
        var bag = new AlgorithmContextBag();
        bag.Set("Atk", 10d);
        bag.Set("Def", 3d);
        var compiled = _compiler.Compile<AlgorithmContextBag, double>("Atk * 2 - Def");
        Assert.True(compiled.IsSuccess);
        Assert.Equal(17d, compiled.Data!.Execute(bag));
    }

    [Fact]
    public void Compile_UnknownMethod_IsDenied()
    {
        var compiled = _compiler.Compile<CombatStats, decimal>("Atk.ToString()");
        Assert.False(compiled.IsSuccess);
        Assert.Equal("algorithm.expression.denied", compiled.Errors![0].Code);
    }

    [Fact]
    public void Validate_FileDelete_DoesNotSucceed()
    {
        var validated = _compiler.Validate("File.Delete(1)");
        Assert.False(validated.IsSuccess);
    }

    [Fact]
    public void Compile_MathPi_IsAllowed()
    {
        var compiled = _compiler.Compile<AlgorithmContextBag, double>("Math.PI");
        Assert.True(compiled.IsSuccess);
        Assert.Equal(Math.PI, compiled.Data!.Execute(new()), 5);
    }

    [Fact]
    public void Compile_ChoiceKey_BranchesOnTheKeyAndFallsBackToTheNumber()
    {
        var compiled = _compiler.Compile<AlgorithmContextBag, double>("fish_type_key == \"bass\" ? 15 : fish_type");
        Assert.True(compiled.IsSuccess);
        var bass = new AlgorithmContextBag();
        bass.Set("fish_type", 10d);
        bass.Set("fish_type_key", "bass");
        Assert.Equal(15d, compiled.Data!.Execute(bass));
        var catfish = new AlgorithmContextBag();
        catfish.Set("fish_type", 10d);
        catfish.Set("fish_type_key", "catfish");
        Assert.Equal(10d, compiled.Data.Execute(catfish));
    }

    [Fact]
    public void Compile_StringLength_IsDenied()
    {
        var compiled = _compiler.Compile<NamedText, int>("Name.Length");
        Assert.False(compiled.IsSuccess);
        Assert.Equal("algorithm.expression.denied", compiled.Errors![0].Code);
    }

    private sealed record CombatStats(decimal Atk, decimal Def);

    private sealed record NamedText(string Name);
}

public sealed class AlgorithmRegistryTests
{
    [Fact]
    public async Task ExecuteAsync_NamedAlgorithm_ReturnsBody()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms();
        services.AddNamedAlgorithm<DoubleAtk, CombatStats, decimal>("double.atk");
        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAlgorithmRegistry>();
        var value = await registry.ExecuteAsync<CombatStats, decimal>("double.atk", new(4m, 0m), TestCt);
        Assert.Equal(8m, value);
    }

    [Fact]
    public void AddNamedAlgorithm_DuplicateKey_FailsWhenRegistryIsCreated()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms();
        services.AddNamedAlgorithm<DoubleAtk, CombatStats, decimal>("double.atk");
        services.AddNamedAlgorithm<TripleAtk, CombatStats, decimal>("double.atk");
        using var provider = services.BuildServiceProvider();
        Assert.Throws<AlgorithmException>(() => provider.GetRequiredService<IAlgorithmRegistry>());
    }

    [Fact]
    public async Task ExecuteAsync_MissingKey_Throws()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms();
        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAlgorithmRegistry>();
        await Assert.ThrowsAsync<AlgorithmException>(() => registry.ExecuteAsync<CombatStats, decimal>("missing.key", new(1m, 1m), TestCt));
    }

    [Fact]
    public void AddAlgorithms_BadStartupExpression_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<AlgorithmException>(() => services.AddAlgorithms(options => options.Expressions.Add(new() { Key = "bad.expr", Expression = "Atk *" })));
    }

    [Fact]
    public async Task SaveRevision_StartupKey_IsRejected()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms();
        services.AddMemoryAlgorithmStore();
        services.AddNamedAlgorithm<DoubleAtk, CombatStats, decimal>("double.atk");
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var saved = await catalog.SaveRevisionAsync("double.atk", new() { Kind = AlgorithmKind.Expression, Source = "Atk * 2" }, TestCt);
        Assert.False(saved.IsSuccess);
        Assert.Equal("algorithm.key.shadows", saved.Errors![0].Code);
    }

    [Fact]
    public async Task ExecuteAsync_AllowRuntimeOverride_UsesCatalogBody()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms(options => options.AllowRuntimeOverride = true);
        services.AddExpressionAlgorithm<AlgorithmContextBag, double>("attack.live", "Atk * 2");
        services.AddMemoryAlgorithmStore();
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var registry = provider.GetRequiredService<IAlgorithmRegistry>();
        var ct = TestCt;
        Assert.True((await catalog.SaveRevisionAsync("attack.live", new() { Kind = AlgorithmKind.Expression, Source = "Atk * 3" }, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Atk", 10d);
        Assert.Equal(30d, await registry.ExecuteAsync<AlgorithmContextBag, double>("attack.live", bag, ct));
    }

    [Fact]
    public async Task Execute_AfterCompile_SkipsStoreAndMatchesAsync()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms();
        services.AddMemoryAlgorithmStore();
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var registry = provider.GetRequiredService<IAlgorithmRegistry>();
        var store = provider.GetRequiredService<MemoryAlgorithmDefinitionStore>();
        var key = "attack." + LyoGuid.CreateV7().ToString("N");
        var ct = TestCt;
        Assert.True((await catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.Expression, Source = "Atk * 2" }, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Atk", 10d);
        var asyncValue = await registry.ExecuteAsync<AlgorithmContextBag, double>(key, bag, ct);
        var reads = store.ReadCount;
        var syncValue = registry.Execute<AlgorithmContextBag, double>(key, bag);
        Assert.Equal(20d, asyncValue);
        Assert.Equal(asyncValue, syncValue);
        Assert.Equal(reads, store.ReadCount);
        var algorithm = await provider.GetRequiredService<IAlgorithmRuntimeStore>().ResolveAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(key), ct);
        Assert.Equal(key, algorithm!.Key);
    }

    [Fact]
    public async Task SetLabel_MissingAndMutability_UseDistinctCodes()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms().AddMemoryAlgorithmStore();
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var ct = TestCt;
        var missing = await catalog.SetLabelAsync("missing.key", "live", new() { Revision = 1, IsMutable = true }, ct);
        Assert.Equal("algorithm.definition.missing", missing.Errors![0].Code);
        var key = "attack." + LyoGuid.CreateV7().ToString("N");
        Assert.True((await catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.Expression, Source = "Atk * 2" }, ct)).IsSuccess);
        Assert.True((await catalog.SetLabelAsync(key, "live", new() { Revision = 1, IsMutable = true }, ct)).IsSuccess);
        var frozen = await catalog.SetLabelAsync(key, "live", new() { Revision = 1, IsMutable = false }, ct);
        Assert.Equal("algorithm.label.mutability", frozen.Errors![0].Code);
        Assert.True((await catalog.SetLabelAsync(key, "game.1.0", new() { Revision = 1, IsMutable = false }, ct)).IsSuccess);
        Assert.True((await catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.Expression, Source = "Atk * 3" }, ct)).IsSuccess);
        var moved = await catalog.SetLabelAsync(key, "game.1.0", new() { Revision = 2, IsMutable = false }, ct);
        Assert.Equal("algorithm.label.immutable", moved.Errors![0].Code);
    }

    [Fact]
    public async Task SaveRevision_ParallelCalls_AllocateDistinctRevisions()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms().AddMemoryAlgorithmStore();
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var key = "attack." + LyoGuid.CreateV7().ToString("N");
        var ct = TestCt;
        var first = catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.Expression, Source = "Atk * 2" }, ct);
        var second = catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.Expression, Source = "Atk * 3" }, ct);
        var saved = await Task.WhenAll(first, second);
        Assert.True(saved[0].IsSuccess);
        Assert.True(saved[1].IsSuccess);
        Assert.Equal(2, saved.Max(s => s.Data!.CurrentRevision));
        var revisions = await catalog.GetRevisionsAsync(key, ct);
        Assert.Equal(2, revisions.Count);
    }

    [Fact]
    public async Task ExecuteAsync_FishingBoard_SumsMapAndBand()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var typeKey = "fish.type." + suffix;
        var weightKey = "fish.weight." + suffix;
        var scoreKey = "fish.score." + suffix;
        Assert.True(
            (await catalog.SaveRevisionAsync(
                typeKey,
                new() { Kind = AlgorithmKind.DiscreteMap, Source = "", Payload = """{"input":"FishType","entries":[{"key":"bass","points":10},{"key":"trout","points":4}]}""" },
                ct)).IsSuccess);

        Assert.True(
            (await catalog.SaveRevisionAsync(
                weightKey,
                new() { Kind = AlgorithmKind.RangeBand, Source = "", Payload = """{"input":"Weight","bands":[{"min":0,"max":10,"points":1},{"min":10,"max":50,"points":5}]}""" },
                ct)).IsSuccess);

        var composite = "{\"combiner\":\"Sum\",\"children\":[{\"ref\":\"" + typeKey + "\"},{\"ref\":\"" + weightKey + "\"}]}";
        Assert.True((await catalog.SaveRevisionAsync(scoreKey, new() { Kind = AlgorithmKind.Composite, Source = "", Payload = composite }, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("FishType", "bass");
        bag.Set("Weight", 12d);
        Assert.Equal(15d, await registry.ExecuteAsync<AlgorithmContextBag, double>(scoreKey, bag, ct));
        var breakdown = await registry.ExecuteWithBreakdownAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(scoreKey), bag, ct);
        Assert.Equal(15d, breakdown.Value);
        Assert.Equal(15d, breakdown.Steps[0].Points);
        Assert.Equal(AlgorithmKind.Composite, breakdown.Steps[0].Kind);
        Assert.Equal(2, breakdown.Steps[0].Children.Count);
        Assert.Equal(10d, breakdown.Steps.SelectMany(step => step.Children).Single(step => step.Kind == AlgorithmKind.DiscreteMap).Points);
        Assert.Equal(5d, breakdown.Steps.SelectMany(step => step.Children).Single(step => step.Kind == AlgorithmKind.RangeBand).Points);
        Assert.Equal(15d, registry.Execute<AlgorithmContextBag, double>(scoreKey, bag));
    }

    [Fact]
    public async Task ExecuteAsync_RangeAndMapPolicies_ZeroNearestAndReject()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var nearestKey = "fish.nearest." + suffix;
        var rejectKey = "fish.reject." + suffix;
        var zeroKey = "fish.zero." + suffix;
        var mapKey = "fish.missing." + suffix;
        Assert.True(
            (await catalog.SaveRevisionAsync(
                nearestKey,
                new() {
                    Kind = AlgorithmKind.RangeBand,
                    Source = "",
                    Payload = """{"input":"Weight","outOfRange":"Nearest","bands":[{"min":0,"max":10,"points":1},{"min":10,"max":50,"points":5}]}"""
                }, ct)).IsSuccess);

        Assert.True(
            (await catalog.SaveRevisionAsync(
                rejectKey, new() { Kind = AlgorithmKind.RangeBand, Source = "", Payload = """{"input":"Weight","outOfRange":"Reject","bands":[{"min":0,"max":10,"points":1}]}""" },
                ct)).IsSuccess);

        Assert.True(
            (await catalog.SaveRevisionAsync(
                zeroKey,
                new() { Kind = AlgorithmKind.Linear, Source = "", Payload = """{"input":"Weight","inputMin":0,"inputMax":10,"pointsMin":0,"pointsMax":100,"clamp":true}""" }, ct))
            .IsSuccess);

        Assert.True(
            (await catalog.SaveRevisionAsync(
                mapKey, new() { Kind = AlgorithmKind.DiscreteMap, Source = "", Payload = """{"input":"FishType","missing":"Reject","entries":[{"key":"bass","points":10}]}""" },
                ct)).IsSuccess);

        var bag = new AlgorithmContextBag();
        bag.Set("Weight", 60d);
        Assert.Equal(5d, await registry.ExecuteAsync<AlgorithmContextBag, double>(nearestKey, bag, ct));
        bag.Set("Weight", 20d);
        Assert.Equal(100d, await registry.ExecuteAsync<AlgorithmContextBag, double>(zeroKey, bag, ct));
        bag.Set("FishType", "carp");
        await Assert.ThrowsAsync<AlgorithmException>(() => registry.ExecuteAsync<AlgorithmContextBag, double>(mapKey, bag, ct));
        bag.Set("Weight", 20d);
        await Assert.ThrowsAsync<AlgorithmException>(() => registry.ExecuteAsync<AlgorithmContextBag, double>(rejectKey, bag, ct));
        var missing = await catalog.SaveRevisionAsync("fish.bad." + suffix, new() { Kind = AlgorithmKind.DiscreteMap, Source = "" }, ct);
        Assert.Equal("algorithm.payload.missing", missing.Errors![0].Code);
        var invalid = await catalog.SaveRevisionAsync("fish.bad2." + suffix, new() { Kind = AlgorithmKind.DiscreteMap, Source = "", Payload = "{}" }, ct);
        Assert.Equal("algorithm.payload.invalid", invalid.Errors![0].Code);
    }

    [Fact]
    public async Task ExecuteAsync_ChildCall_NestsBreakdownAndRejectsCycles()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var child = "bonus.flat." + suffix;
        var parent = "attack.total." + suffix;
        var cycle = "cycle.self." + suffix;
        Assert.True((await catalog.SaveRevisionAsync(child, new() { Kind = AlgorithmKind.Expression, Source = "2" }, ct)).IsSuccess);
        Assert.True((await catalog.SaveRevisionAsync(parent, new() { Kind = AlgorithmKind.Expression, Source = "Atk + Algo(\"" + child + "\")" }, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Atk", 10d);
        var breakdown = await registry.ExecuteWithBreakdownAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(parent), bag, ct);
        Assert.Equal(12d, breakdown.Value);
        Assert.Equal(AlgorithmKind.Expression, breakdown.Steps[0].Kind);
        Assert.Equal(12d, breakdown.Steps[0].Points);
        Assert.Equal(2d, Assert.Single(breakdown.Steps[0].Children).Points);
        Assert.True((await catalog.SaveRevisionAsync(cycle, new() { Kind = AlgorithmKind.Expression, Source = "Algo(\"" + cycle + "\")" }, ct)).IsSuccess);
        var cycleError = await Assert.ThrowsAsync<AlgorithmException>(() => registry.ExecuteAsync<AlgorithmContextBag, double>(cycle, bag, ct));
        Assert.Contains("calls itself", cycleError.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_ChainLongerThanMaxDepth_Throws()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms(options => options.MaxChainDepth = 1);
        services.AddMemoryAlgorithmStore();
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var registry = provider.GetRequiredService<IAlgorithmRegistry>();
        var ct = TestCt;
        var suffix = LyoGuid.CreateV7().ToString("N");
        var child = "chain.child." + suffix;
        var parent = "chain.parent." + suffix;
        Assert.True((await catalog.SaveRevisionAsync(child, new() { Kind = AlgorithmKind.Expression, Source = "1" }, ct)).IsSuccess);
        Assert.True((await catalog.SaveRevisionAsync(parent, new() { Kind = AlgorithmKind.Expression, Source = "Algo(\"" + child + "\")" }, ct)).IsSuccess);
        var error = await Assert.ThrowsAsync<AlgorithmException>(() => registry.ExecuteAsync<AlgorithmContextBag, double>(parent, new(), ct));
        Assert.Contains("maximum depth", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetLabel_ImmutableMap_StaysPinnedWhileLiveMoves()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "fish.type." + LyoGuid.CreateV7().ToString("N");
        var first = """{"input":"FishType","entries":[{"key":"bass","points":10}]}""";
        var second = """{"input":"FishType","entries":[{"key":"bass","points":30}]}""";
        Assert.True((await catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.DiscreteMap, Source = "", Payload = first }, ct)).IsSuccess);
        Assert.True((await catalog.SetLabelAsync(key, "game.1.0", new() { Revision = 1, IsMutable = false }, ct)).IsSuccess);
        Assert.True((await catalog.SetLabelAsync(key, "live", new() { Revision = 1, IsMutable = true }, ct)).IsSuccess);
        Assert.True((await catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.DiscreteMap, Source = "", Payload = second }, ct)).IsSuccess);
        Assert.True((await catalog.SetLabelAsync(key, "live", new() { Revision = 2, IsMutable = true }, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("FishType", "bass");
        Assert.Equal(30d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(key), bag, ct));
        Assert.Equal(30d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Label(key, "live"), bag, ct));
        Assert.Equal(10d, await registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Label(key, "game.1.0"), bag, ct));
    }

    [Fact]
    public async Task ExecuteAsync_CompositeFormula_AddsInlineChildren()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "fish.formula." + LyoGuid.CreateV7().ToString("N");
        var payload = """
                      {"combiner":"Formula","formula":"score(\"type\") + score(\"weight\")","children":[
                        {"key":"type","kind":"DiscreteMap","payload":{"input":"FishType","entries":[{"key":"bass","points":10}]}},
                        {"key":"weight","kind":"Linear","payload":{"input":"Weight","inputMin":0,"inputMax":10,"pointsMin":0,"pointsMax":10}}
                      ]}
                      """;

        Assert.True((await catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.Composite, Source = "", Payload = payload }, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("FishType", "bass");
        bag.Set("Weight", 5d);
        Assert.Equal(15d, await registry.ExecuteAsync<AlgorithmContextBag, double>(key, bag, ct));
    }

    private static async Task<(IAlgorithmCatalog Catalog, IAlgorithmRegistry Registry, CancellationToken Ct)> CatalogAsync()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms().AddMemoryAlgorithmStore();
        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IAlgorithmCatalog>(), provider.GetRequiredService<IAlgorithmRegistry>(), TestCt);
    }

    private sealed record CombatStats(decimal Atk, decimal Def);

    private sealed class DoubleAtk : INamedAlgorithm<CombatStats, decimal>
    {
        public string Key => "double.atk";

        public decimal Execute(CombatStats context) => context.Atk * 2m;
    }

    private sealed class TripleAtk : INamedAlgorithm<CombatStats, decimal>
    {
        public string Key => "triple.atk";

        public decimal Execute(CombatStats context) => context.Atk * 3m;
    }
}