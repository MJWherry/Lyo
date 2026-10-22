using System.Text.Json;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Response;
using Lyo.Algorithms.Web.Components;

namespace Lyo.Algorithms.Web.Components.Tests;

public sealed class AlgorithmDraftTests
{
    [Fact]
    public void ToPayload_Expression_WritesTypeMap()
    {
        var draft = new AlgorithmDraft {
            Kind = AlgorithmKind.Expression,
            Result = "decimal",
            Parameters = [new() { Name = "Rate", Alias = "decimal" }, new() { Name = "Periods", Alias = "int" }]
        };

        using var doc = JsonDocument.Parse(draft.ToPayload()!);
        Assert.Equal("decimal", doc.RootElement.GetProperty("result").GetString());
        Assert.Equal("int", doc.RootElement.GetProperty("parameters").GetProperty("Periods").GetString());
    }

    [Fact]
    public void ToPayload_DiscreteMap_WritesCamelCaseNumbers()
    {
        var draft = new AlgorithmDraft {
            Kind = AlgorithmKind.DiscreteMap, Input = "FishType", Entries = [new() { Key = "bass", Value = "10" }, new() { Key = "trout", Value = "4" }]
        };

        using var doc = JsonDocument.Parse(draft.ToPayload()!);
        Assert.Equal("FishType", doc.RootElement.GetProperty("input").GetString());
        Assert.Equal(10, doc.RootElement.GetProperty("entries")[0].GetProperty("points").GetDouble());
        Assert.False(doc.RootElement.TryGetProperty("result", out _));
    }

    [Fact]
    public void ToPayload_StringMapZero_IncludesMissingValue()
    {
        var draft = new AlgorithmDraft {
            Kind = AlgorithmKind.DiscreteMap,
            Result = "string",
            Input = "Code",
            Missing = MissingKeyPolicy.Zero,
            MissingValue = "none",
            Entries = [new() { Key = "a", Value = "yes" }]
        };

        using var doc = JsonDocument.Parse(draft.ToPayload()!);
        Assert.Equal("none", doc.RootElement.GetProperty("missingValue").GetString());
        Assert.Equal("yes", doc.RootElement.GetProperty("entries")[0].GetProperty("points").GetString());
        Assert.Empty(draft.Issues());
    }

    [Fact]
    public void Issues_StringMapWithoutMissingValue_ReportsIt()
    {
        var draft = new AlgorithmDraft {
            Kind = AlgorithmKind.DiscreteMap, Result = "string", Input = "Code", Missing = MissingKeyPolicy.Zero, Entries = [new() { Key = "a", Value = "yes" }]
        };

        Assert.Contains(draft.Issues(), static issue => issue.Contains("missingValue", StringComparison.Ordinal));
    }

    [Fact]
    public void Issues_ChildRefAndInline_ReportsXor()
    {
        var draft = new AlgorithmDraft { Kind = AlgorithmKind.Composite, Children = [new() { Ref = "board.type", Kind = AlgorithmKind.DiscreteMap }] };

        Assert.Contains(draft.Issues(), static issue => issue.Contains("not both", StringComparison.Ordinal));
    }

    [Fact]
    public void IsIdentifier_DottedName_IsFalse()
    {
        Assert.False(AlgorithmDraftRules.IsIdentifier("fish.type"));
        Assert.True(AlgorithmDraftRules.IsIdentifier("fish_type"));
    }

    [Fact]
    public void EditorKind_DatetimeArray_IsList()
    {
        Assert.Equal(AlgorithmValueKind.List, AlgorithmDraftRules.EditorKind("datetime[]"));
        Assert.Equal(AlgorithmValueKind.Number, AlgorithmDraftRules.EditorKind("decimal"));
        Assert.Equal(AlgorithmValueKind.Date, AlgorithmDraftRules.EditorKind("datetime"));
        Assert.Equal(AlgorithmValueKind.Switch, AlgorithmDraftRules.EditorKind("bool"));
        Assert.Equal(AlgorithmValueKind.Duration, AlgorithmDraftRules.EditorKind("timespan"));
        Assert.Equal(AlgorithmValueKind.Text, AlgorithmDraftRules.EditorKind("string"));
    }

    [Fact]
    public void ToContextJson_IntAndArray_UsesJsonTypes()
    {
        var json = AlgorithmContextRows.ToJson([
            new() { Name = "Periods", Alias = "int", Text = "4" }, new() { Name = "Dates", Alias = "datetime[]", Text = """["2020-01-01T00:00:00"]""" }
        ]);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(4, doc.RootElement.GetProperty("Periods").GetInt32());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("Dates").ValueKind);
    }

    [Fact]
    public void FromRevision_DiscreteMap_ReadsEntries()
    {
        var draft = AlgorithmDraft.FromRevision(
            new AlgorithmRevisionRes { Kind = AlgorithmKind.DiscreteMap, Payload = """{"input":"FishType","entries":[{"key":"bass","points":10}]}""" });

        Assert.Equal("FishType", draft.Input);
        Assert.Equal("bass", draft.Entries[0].Key);
        Assert.Equal("10", draft.Entries[0].Value);
    }

    [Fact]
    public void Insert_AppendsAtCaret() => Assert.Equal("Atk + 1", AlgorithmDraftRules.Insert("Atk", 3, 3, " + 1"));
}
