using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Response;
using Lyo.Common.Core.Identifiers;
using Lyo.Result;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Tests;

public sealed class AlgorithmRegistryComplexTests
{
    [Fact]
    public async Task ExecuteAsync_WeightedSum_ScalesChildrenAndKeepsRawSteps()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var typeKey = "board.type." + suffix;
        var weightKey = "board.weight." + suffix;
        var scoreKey = "board.score." + suffix;
        Assert.True((await Save(catalog, typeKey, AlgorithmKind.DiscreteMap, "", """{"input":"FishType","entries":[{"key":"bass","points":10}]}""", ct)).IsSuccess);
        Assert.True((await Save(catalog, weightKey, AlgorithmKind.RangeBand, "", """{"input":"Weight","bands":[{"min":10,"max":50,"points":5}]}""", ct)).IsSuccess);
        var composite = "{\"combiner\":\"WeightedSum\",\"children\":[{\"ref\":\"" + typeKey + "\",\"weight\":2},{\"ref\":\"" + weightKey + "\",\"weight\":0.5}]}";
        Assert.True((await Save(catalog, scoreKey, AlgorithmKind.Composite, "", composite, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("FishType", "bass");
        bag.Set("Weight", 12d);
        var breakdown = await registry.ExecuteWithBreakdownAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(scoreKey), bag, ct);
        Assert.Equal(22.5d, breakdown.Value);
        Assert.Equal(22.5d, breakdown.Steps[0].Points);
        Assert.Equal(10d, breakdown.Steps.SelectMany(step => step.Children).Single(step => step.Kind == AlgorithmKind.DiscreteMap).Points);
        Assert.Equal(5d, breakdown.Steps.SelectMany(step => step.Children).Single(step => step.Kind == AlgorithmKind.RangeBand).Points);
        Assert.Equal(22.5d, registry.Execute<AlgorithmContextBag, double>(scoreKey, bag));
    }

    [Fact]
    public async Task ExecuteAsync_NestedComposite_AddsWeightedInnerAndBonus()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var typeKey = "nest.type." + suffix;
        var innerKey = "nest.inner." + suffix;
        var bonusKey = "nest.bonus." + suffix;
        var outerKey = "nest.outer." + suffix;
        var chart = """{"input":"FishType","entries":[{"key":"bass","points":10},{"key":"trout","points":4}]}""";
        Assert.True((await Save(catalog, typeKey, AlgorithmKind.DiscreteMap, "", chart, ct)).IsSuccess);
        var inner = "{\"combiner\":\"WeightedSum\",\"children\":[{\"ref\":\"" + typeKey + "\",\"weight\":2}]}";
        Assert.True((await Save(catalog, innerKey, AlgorithmKind.Composite, "", inner, ct)).IsSuccess);
        Assert.True((await Save(catalog, bonusKey, AlgorithmKind.Expression, "3", null, ct)).IsSuccess);
        var outer = "{\"combiner\":\"Sum\",\"children\":[{\"ref\":\"" + innerKey + "\"},{\"ref\":\"" + bonusKey + "\"}]}";
        Assert.True((await Save(catalog, outerKey, AlgorithmKind.Composite, "", outer, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("FishType", "trout");
        var breakdown = await registry.ExecuteWithBreakdownAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(outerKey), bag, ct);
        Assert.Equal(11d, breakdown.Value);
        var innerStep = breakdown.Steps[0].Children.Single(step => step.Kind == AlgorithmKind.Composite);
        Assert.Equal(8d, innerStep.Points);
        Assert.Equal(4d, Assert.Single(innerStep.Children).Points);
        Assert.Equal(3d, breakdown.Steps[0].Children.Single(step => step.Kind == AlgorithmKind.Expression).Points);
    }

    [Fact]
    public async Task ExecuteAsync_LinearWithoutClamp_Extrapolates()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "line.open." + LyoGuid.CreateV7().ToString("N");
        Assert.True((await Save(catalog, key, AlgorithmKind.Linear, "", """{"input":"Weight","inputMin":0,"inputMax":10,"pointsMin":0,"pointsMax":100}""", ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Weight", 15d);
        Assert.Equal(150d, await registry.ExecuteAsync<AlgorithmContextBag, double>(key, bag, ct));
    }

    [Fact]
    public async Task ExecuteAsync_FormulaCombiner_MultipliesContextByInlineMap()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "mods.formula." + LyoGuid.CreateV7().ToString("N");
        var payload = """
                      {"combiner":"Formula","formula":"Atk * score(\"chart\")","children":[
                        {"key":"chart","kind":"DiscreteMap","payload":{"input":"Matchup","entries":[{"key":"water-fire","points":2},{"key":"water-water","points":0.5}]}}
                      ]}
                      """;

        Assert.True((await Save(catalog, key, AlgorithmKind.Composite, "", payload, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Atk", 10d);
        bag.Set("Matchup", "water-fire");
        var breakdown = await registry.ExecuteWithBreakdownAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(key), bag, ct);
        Assert.Equal(20d, breakdown.Value);
        Assert.Equal("water-fire", Assert.Single(breakdown.Steps[0].Children).Input);
        Assert.Equal(2d, Assert.Single(breakdown.Steps[0].Children).Points);
    }

    [Fact]
    public async Task ExecuteAsync_TypedDamageFormula_FloorsBaseThenAppliesChartAndStab()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var typeKey = "dmg.type." + suffix;
        var baseKey = "dmg.base." + suffix;
        var totalKey = "dmg.total." + suffix;
        var chart = """{"input":"Matchup","entries":[{"key":"water-fire","points":2},{"key":"water-water","points":0.5}]}""";
        Assert.True((await Save(catalog, typeKey, AlgorithmKind.DiscreteMap, "", chart, ct)).IsSuccess);
        var baseSource = "Math.Floor(Math.Floor(Math.Floor(2 * Level / 5 + 2) * Power * Atk / Def) / 50) + 2";
        Assert.True((await Save(catalog, baseKey, AlgorithmKind.Expression, baseSource, null, ct)).IsSuccess);
        var totalSource = "Math.Floor(Algo(\"" + baseKey + "\") * Algo(\"" + typeKey + "\") * Stab)";
        Assert.True((await Save(catalog, totalKey, AlgorithmKind.Expression, totalSource, null, ct)).IsSuccess);
        var super = new Battle(50, 40, 120, 90, 1.5, "water-fire");
        var breakdown = await registry.ExecuteWithBreakdownAsync<Battle, double>(AlgorithmRef.Latest(totalKey), super, ct);
        Assert.Equal(75d, breakdown.Value);
        Assert.Equal(25d, breakdown.Steps[0].Children.Single(step => step.Key == baseKey).Points);
        var chartStep = breakdown.Steps[0].Children.Single(step => step.Key == typeKey);
        Assert.Equal(AlgorithmKind.DiscreteMap, chartStep.Kind);
        Assert.Equal("water-fire", chartStep.Input);
        Assert.Equal(2d, chartStep.Points);
        var resisted = new Battle(50, 40, 120, 90, 1, "water-water");
        Assert.Equal(12d, await registry.ExecuteAsync<Battle, double>(totalKey, resisted, ct));
        Assert.Equal(75d, registry.Execute<Battle, double>(totalKey, super));
    }

    private static Task<Result<AlgorithmDefinitionRes>> Save(IAlgorithmCatalog catalog, string key, AlgorithmKind kind, string source, string? payload, CancellationToken ct)
        => catalog.SaveRevisionAsync(key, new() { Kind = kind, Source = source, Payload = payload }, ct);

    private static async Task<(IAlgorithmCatalog Catalog, IAlgorithmRegistry Registry, CancellationToken Ct)> CatalogAsync()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms().AddMemoryAlgorithmStore();
        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IAlgorithmCatalog>(), provider.GetRequiredService<IAlgorithmRegistry>(), TestCt);
    }

    private sealed record Battle(double Level, double Power, double Atk, double Def, double Stab, string Matchup);
}