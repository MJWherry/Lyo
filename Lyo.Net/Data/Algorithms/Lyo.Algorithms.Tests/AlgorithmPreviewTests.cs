using System.Text.Json;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Tests;

public sealed class AlgorithmPreviewTests
{
    private const string FishingFormula = """
                                          fish_type + (weight >= 5 && weight < 10 ? 3 : (weight >= 0 && weight < 5 ? 1 : 0)) + (length < 10 ? 0 : (length > 20 ? 50 : (length - 10) / 10 * 50))
                                          """;

    [Fact]
    public async Task PreviewAsync_Expression_UsesBag()
    {
        var (preview, _, store, ct) = await HostAsync();
        var result = await preview.PreviewAsync(
            new() { Kind = AlgorithmKind.Expression, Source = "Atk * 2 - Def" }, Context("""{"Atk":3,"Def":1}"""), "attack.physical", ct);

        Assert.Equal(5d, result.Value);
        Assert.Equal("attack.physical", result.Steps[0].Key);
        Assert.Equal(5d, result.Steps[0].Points);
        Assert.Empty(await store.ListAsync(ct));
    }

    [Fact]
    public async Task PreviewAsync_DeclaredInt_ConvertsJsonNumber()
    {
        var (preview, _, _, ct) = await HostAsync();
        var result = await preview.PreviewAsync(
            new() { Kind = AlgorithmKind.Expression, Source = "Amount + 1", Payload = """{"result":"int","parameters":{"Amount":"int"}}""" }, Context("""{"Amount":2}"""),
            "amount.plus", ct);

        Assert.Equal(3, result.Value);
        Assert.Equal("int", result.Result);
    }

    [Fact]
    public async Task PreviewAsync_DecimalParameters_UsesEachDeclaredType()
    {
        var (preview, _, _, ct) = await HostAsync();
        var payload = """
                      {"result":"decimal","parameters":{"Rate":"decimal","Periods":"int","Issued":"datetime","Maturity":"datetime","Dates":"datetime[]"}}
                      """;
        var source = "Rate * Convert.ToDecimal(Periods) + (Maturity > Issued && Dates[0] == Issued ? 1m : 0m)";
        var context = Context("""{"Rate":2.5,"Periods":4,"Issued":"2020-01-01T00:00:00","Maturity":"2020-06-01T00:00:00","Dates":["2020-01-01T00:00:00"]}""");
        var result = await preview.PreviewAsync(new() { Kind = AlgorithmKind.Expression, Source = source, Payload = payload }, context, "mix.preview", ct);

        Assert.Equal(11m, result.Value);
    }

    [Fact]
    public async Task PreviewAsync_DiscreteMap_DateTime_ReturnsValue()
    {
        var (preview, _, _, ct) = await HostAsync();
        var payload = """{"result":"datetime","missing":"Reject","input":"Code","parameters":{"Code":"string"},"entries":[{"key":"start","points":"2020-01-15T00:00:00"}]}""";
        var result = await preview.PreviewAsync(
            new() { Kind = AlgorithmKind.DiscreteMap, Payload = payload }, Context("""{"Code":"start"}"""), "when.preview", ct);

        Assert.Equal(new DateTime(2020, 1, 15), result.Value);
        Assert.Equal(0d, result.Steps[0].Points);
        Assert.Equal(new DateTime(2020, 1, 15), result.Steps[0].Value);
    }

    [Fact]
    public async Task PreviewAsync_RangeBand_ReturnsString()
    {
        var (preview, _, _, ct) = await HostAsync();
        var payload = """{"result":"string","outOfRange":"Reject","input":"Weight","bands":[{"min":0,"max":10,"points":"light"}]}""";
        var result = await preview.PreviewAsync(
            new() { Kind = AlgorithmKind.RangeBand, Payload = payload }, Context("""{"Weight":5}"""), "band.preview", ct);

        Assert.Equal("light", result.Value);
    }

    [Fact]
    public async Task PreviewAsync_CompositeFormula_InlineChild()
    {
        var (preview, _, _, ct) = await HostAsync();
        var payload = """
                      {"parameters":{"Atk":"double","Matchup":"string"},"combiner":"Formula","formula":"Atk * score(\"chart\")","children":[{"key":"chart","kind":"DiscreteMap","payload":{"input":"Matchup","entries":[{"key":"water-fire","points":2}]}}]}
                      """;
        var result = await preview.PreviewAsync(
            new() { Kind = AlgorithmKind.Composite, Payload = payload }, Context("""{"Atk":3,"Matchup":"water-fire"}"""), "combo.preview", ct);

        Assert.Equal(6d, result.Value);
    }

    [Fact]
    public async Task PreviewAsync_CatalogRef_UsesSavedRevision()
    {
        var (preview, catalog, _, ct) = await HostAsync();
        var key = "board.type." + LyoGuid.CreateV7().ToString("N");
        Assert.True((await catalog.SaveRevisionAsync(key, new() { Kind = AlgorithmKind.Expression, Source = "10" }, ct)).IsSuccess);
        var before = await catalog.GetRevisionsAsync(key, ct);
        var result = await preview.PreviewAsync(
            new() { Kind = AlgorithmKind.Expression, Source = "Algo(\"" + key + "\")", Payload = "{\"calls\":{\"" + key + "\":\"double\"}}" }, Context("{}"), "draft.call", ct);

        Assert.Equal(10d, result.Value);
        Assert.Equal(before.Count, (await catalog.GetRevisionsAsync(key, ct)).Count);
    }

    [Fact]
    public async Task PreviewAsync_RejectsDottedContextKey()
    {
        var (preview, _, _, ct) = await HostAsync();
        var error = await Assert.ThrowsAsync<AlgorithmException>(() => preview.PreviewAsync(
            new() { Kind = AlgorithmKind.Expression, Source = "1" }, Context("""{"fish.type":1}"""), "dotted.preview", ct));

        Assert.Equal("algorithm.context.invalid", error.Code);
    }

    [Fact]
    public void Identifiers_FishingFormula_ListsBareNames()
    {
        var names = AlgorithmExpressionHelp.Identifiers(FishingFormula);
        Assert.Equal(["fish_type", "weight", "length"], names);
    }

    private static JsonElement Context(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static async Task<(IAlgorithmPreview Preview, IAlgorithmCatalog Catalog, IAlgorithmDefinitionStore Store, CancellationToken Ct)> HostAsync()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms().AddMemoryAlgorithmStore();
        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<IAlgorithmPreview>(), provider.GetRequiredService<IAlgorithmCatalog>(), provider.GetRequiredService<IAlgorithmDefinitionStore>(),
            TestCt);
    }
}
