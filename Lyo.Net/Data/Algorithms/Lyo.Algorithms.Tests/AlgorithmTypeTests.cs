using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Response;
using Lyo.Common.Core.Identifiers;
using Lyo.Result;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Tests;

public sealed class AlgorithmTypeTests
{
    private readonly ExpressionCompiler _compiler = new();

    [Fact]
    public async Task Compile_MixedParameters_UsesEachDeclaredType()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "mix." + LyoGuid.CreateV7().ToString("N");
        var payload = """
                      {"result":"decimal","parameters":{"Rate":"decimal","Periods":"int","Issued":"datetime","Maturity":"datetime","Dates":"datetime[]"}}
                      """;

        var source = "Rate * Convert.ToDecimal(Periods) + (Maturity > Issued && Dates[0] == Issued ? 1m : 0m)";
        Assert.True((await Save(catalog, key, AlgorithmKind.Expression, source, payload, ct)).IsSuccess);
        var issued = new DateTime(2020, 1, 1);
        var bag = new AlgorithmContextBag();
        bag.Set("Rate", 2.5m);
        bag.Set("Periods", 4);
        bag.Set("Issued", issued);
        bag.Set("Maturity", new DateTime(2020, 6, 1));
        bag.Set("Dates", new[] { issued });
        Assert.Equal(11m, await registry.ExecuteAsync<AlgorithmContextBag, decimal>(key, bag, ct));
    }

    [Fact]
    public async Task ExecuteAsync_Calls_KeepEachChildType()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var fee = "fee." + suffix;
        var maturity = "maturity." + suffix;
        var parent = "parent." + suffix;
        Assert.True((await Save(catalog, fee, AlgorithmKind.Expression, "1.5m", """{"result":"decimal"}""", ct)).IsSuccess);
        Assert.True((await Save(catalog, maturity, AlgorithmKind.Expression, "Issued", """{"result":"datetime","parameters":{"Issued":"datetime"}}""", ct)).IsSuccess);
        var payload = "{\"result\":\"int\",\"calls\":{\"" + fee + "\":\"decimal\",\"" + maturity + "\":\"datetime\"}}";
        var source = "score(\"" + maturity + "\").Day + (Algo(\"" + fee + "\") == 1.5m ? 10 : 0)";
        Assert.True((await Save(catalog, parent, AlgorithmKind.Expression, source, payload, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Issued", new DateTime(2020, 1, 15));
        Assert.Equal(25, await registry.ExecuteAsync<AlgorithmContextBag, int>(parent, bag, ct));
    }

    [Fact]
    public async Task ExecuteAsync_ResultMismatch_Fails()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "mismatch." + LyoGuid.CreateV7().ToString("N");
        Assert.True((await Save(catalog, key, AlgorithmKind.Expression, "1.5m", """{"result":"decimal"}""", ct)).IsSuccess);
        await Assert.ThrowsAsync<AlgorithmException>(() => registry.ExecuteAsync<AlgorithmContextBag, double>(key, new(), ct));
    }

    [Fact]
    public void Compile_SameSourceDifferentMaps_DoNotShareADelegate()
    {
        var ints = AlgorithmTypeBinding.Create(parameters: new Dictionary<string, string> { ["Amount"] = "int" });
        var doubles = AlgorithmTypeBinding.Create(parameters: new Dictionary<string, string> { ["Amount"] = "double" });
        var first = _compiler.Compile<AlgorithmContextBag, double>("Amount + 1", ints);
        var second = _compiler.Compile<AlgorithmContextBag, double>("Amount + 1", doubles);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotSame(first.Data, second.Data);
        var bag = new AlgorithmContextBag();
        bag.Set("Amount", 2);
        Assert.Equal(3d, first.Data!.Execute(bag));
        bag.Set("Amount", 2.5d);
        Assert.Equal(3.5d, second.Data!.Execute(bag));
    }

    [Fact]
    public async Task ExecuteAsync_DiscreteMap_ReturnsDateTime()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "when." + LyoGuid.CreateV7().ToString("N");
        var payload = """{"result":"datetime","missing":"Reject","input":"Code","entries":[{"key":"start","points":"2020-01-15T00:00:00"}]}""";
        Assert.True((await Save(catalog, key, AlgorithmKind.DiscreteMap, "", payload, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Code", "start");
        var breakdown = await registry.ExecuteWithBreakdownAsync<AlgorithmContextBag, DateTime>(AlgorithmRef.Latest(key), bag, ct);
        Assert.Equal(new DateTime(2020, 1, 15), breakdown.Value);
        Assert.Equal(0d, breakdown.Steps[0].Points);
        Assert.Equal(new DateTime(2020, 1, 15), breakdown.Steps[0].Value);
    }

    [Fact]
    public async Task ExecuteAsync_RangeBand_SelectsString()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "band." + LyoGuid.CreateV7().ToString("N");
        var payload = """{"result":"string","outOfRange":"Reject","input":"Weight","bands":[{"min":0,"max":10,"points":"light"}]}""";
        Assert.True((await Save(catalog, key, AlgorithmKind.RangeBand, "", payload, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Weight", 5d);
        Assert.Equal("light", await registry.ExecuteAsync<AlgorithmContextBag, string>(key, bag, ct));
    }

    [Fact]
    public async Task ExecuteAsync_Linear_InterpolatesDecimal()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var key = "line." + LyoGuid.CreateV7().ToString("N");
        var payload = """{"result":"decimal","inputType":"decimal","input":"Amount","inputMin":0,"inputMax":1,"pointsMin":0,"pointsMax":0.1}""";
        Assert.True((await Save(catalog, key, AlgorithmKind.Linear, "", payload, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("Amount", 1m);
        Assert.Equal(0.1m, await registry.ExecuteAsync<AlgorithmContextBag, decimal>(key, bag, ct));
    }

    [Fact]
    public async Task ExecuteAsync_WeightedSum_ScalesDecimalChildren()
    {
        var (catalog, registry, ct) = await CatalogAsync();
        var suffix = LyoGuid.CreateV7().ToString("N");
        var child = "weight.child." + suffix;
        var parent = "weight.sum." + suffix;
        var map = """{"result":"decimal","input":"FishType","entries":[{"key":"bass","points":1}]}""";
        Assert.True((await Save(catalog, child, AlgorithmKind.DiscreteMap, "", map, ct)).IsSuccess);
        var composite = "{\"result\":\"decimal\",\"combiner\":\"WeightedSum\",\"children\":[{\"ref\":\"" + child + "\",\"weight\":0.1}]}";
        Assert.True((await Save(catalog, parent, AlgorithmKind.Composite, "", composite, ct)).IsSuccess);
        var bag = new AlgorithmContextBag();
        bag.Set("FishType", "bass");
        Assert.Equal(0.1m, await registry.ExecuteAsync<AlgorithmContextBag, decimal>(parent, bag, ct));
    }

    [Fact]
    public async Task ExecuteAsync_NamedAlgorithm_ReturnsDateTime()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms().AddMemoryAlgorithmStore();
        services.AddNamedAlgorithm<FirstDate, AlgorithmContextBag, DateTime>("dates.first");
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var registry = provider.GetRequiredService<IAlgorithmRegistry>();
        var key = "dates.catalog." + LyoGuid.CreateV7().ToString("N");
        Assert.True((await Save(catalog, key, AlgorithmKind.Named, "dates.first", """{"result":"datetime"}""", TestCt)).IsSuccess);
        var issued = new DateTime(2020, 1, 15);
        var bag = new AlgorithmContextBag();
        bag.Set("Rate", 1.5m);
        bag.Set("Issued", issued);
        bag.Set("Dates", new[] { issued });
        Assert.Equal(issued, await registry.ExecuteAsync<AlgorithmContextBag, DateTime>(key, bag, TestCt));
    }

    [Fact]
    public void Compile_DateTimeToString_IsDenied()
    {
        var compiled = _compiler.Compile<When, string>("Issued.ToString()");
        Assert.False(compiled.IsSuccess);
        Assert.Equal("algorithm.expression.denied", compiled.Errors![0].Code);
    }

    [Fact]
    public void Compile_StringElementLength_IsDenied()
    {
        var compiled = _compiler.Compile<Names, int>("Values[0].Length");
        Assert.False(compiled.IsSuccess);
        Assert.Equal("algorithm.expression.denied", compiled.Errors![0].Code);
    }

    [Fact]
    public void Create_UnregisteredEnum_FailsUntilRegistered()
    {
        Assert.Throws<AlgorithmException>(() => AlgorithmTypeBinding.Create(parameters: new Dictionary<string, string> { ["Tone"] = nameof(SampleTone) }));
        AlgorithmTypeBinding.RegisterEnum(typeof(SampleTone));
        var binding = AlgorithmTypeBinding.Create(parameters: new Dictionary<string, string> { ["Tone"] = nameof(SampleTone) });
        Assert.Equal(typeof(SampleTone), binding.ParameterType("Tone"));
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

    private sealed record When(DateTime Issued);

    private sealed record Names(string[] Values);

    private enum SampleTone { Low, High }

    private sealed class FirstDate : INamedAlgorithm<AlgorithmContextBag, DateTime>
    {
        public string Key => "dates.first";

        public DateTime Execute(AlgorithmContextBag context)
        {
            if (context.TryGetValue("Rate", out var rate) && rate is decimal money && money > 0m && context.TryGetValue("Issued", out var issued) && issued is DateTime when)
                return when;

            if (context.TryGetValue("Dates", out var dates) && dates is DateTime[] list && list.Length > 0)
                return list[0];

            throw new AlgorithmException("Dates are missing.");
        }
    }
}
