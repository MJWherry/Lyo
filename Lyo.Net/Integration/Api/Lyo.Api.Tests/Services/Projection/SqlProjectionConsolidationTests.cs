using System.Collections;
using System.Runtime.CompilerServices;
using Lyo.Api.Services.Crud.Read.Project;

// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace Lyo.Api.Tests.Services.Projection;

/// <summary>Verifies sibling collection fields share one LINQ Select (single join) in SQL projection.</summary>
public sealed class SqlProjectionConsolidationTests
{
    [Fact]
    public void TryBuildSqlProjectionExpression_MergesSiblingCollectionPathsIntoOneSlot()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<Docket>(["DocketCharges.Code", "DocketCharges.Number"]);
        Assert.Empty(pathErrors);
        var build = service.TryBuildSqlProjectionExpression<Docket>(specs);
        Assert.NotNull(build.Projection);
        Assert.NotNull(build.ConversionPlan);
        Assert.Single(build.ConversionPlan!.Slots);
        var merged = Assert.IsType<SqlProjectionMergedCollectionSlot>(build.ConversionPlan.Slots[0]);
        Assert.Equal(2, merged.SpecIndicesInOrder.Count);
        Assert.Equal(0, merged.SpecIndicesInOrder[0]);
        Assert.Equal(1, merged.SpecIndicesInOrder[1]);
    }

    [Fact]
    public void TryBuildSqlProjectionExpression_IndependentRootFieldsStaySeparateSlots()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<Docket>(["Id", "DocketCharges.Code"]);
        Assert.Empty(pathErrors);
        var build = service.TryBuildSqlProjectionExpression<Docket>(specs);
        Assert.NotNull(build.Projection);
        Assert.NotNull(build.ConversionPlan);
        Assert.Equal(2, build.ConversionPlan!.Slots.Count);
        Assert.IsType<SqlProjectionSingleSlot>(build.ConversionPlan.Slots[0]);
        Assert.IsType<SqlProjectionSingleSlot>(build.ConversionPlan.Slots[1]);
    }

    [Fact]
    public void TryBuildSqlProjectionExpression_CollectionScopeWildcard_DoesNotBuildSqlLayer()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.*"]);
        Assert.Empty(pathErrors);
        var build = service.TryBuildSqlProjectionExpression<NestedPerson>(specs);
        Assert.Null(build.Projection);
        Assert.Null(build.ConversionPlan);
    }

    [Fact]
    public void TryBuildSqlProjectionExpression_NestedAddressSiblings_StillMergesOneSlot()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.Address.StreetName", "ContactAddresses.Address.StreetType"]);
        Assert.Empty(pathErrors);
        var build = service.TryBuildSqlProjectionExpression<NestedPerson>(specs);
        Assert.NotNull(build.Projection);
        Assert.NotNull(build.ConversionPlan);
        Assert.Single(build.ConversionPlan!.Slots);
        Assert.IsType<SqlProjectionMergedCollectionSlot>(build.ConversionPlan.Slots[0]);
    }

    /// <summary>Mixed depths under one collection root (id + nested address fields) must still use one merged Select, not three slots.</summary>
    [Fact]
    public void TryBuildSqlProjectionExpression_UnifiedRootCollection_MergesOneSlot()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) =
            service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.Id", "ContactAddresses.Address.StreetType", "ContactAddresses.Address.StreetName"]);

        Assert.Empty(pathErrors);
        var build = service.TryBuildSqlProjectionExpression<NestedPerson>(specs);
        Assert.NotNull(build.Projection);
        Assert.NotNull(build.ConversionPlan);
        Assert.Single(build.ConversionPlan!.Slots);
        var merged = Assert.IsType<SqlProjectionMergedCollectionSlot>(build.ConversionPlan.Slots[0]);
        Assert.Equal(3, merged.SpecIndicesInOrder.Count);
    }

    [Fact]
    public void CollectProjectionFieldIssues_AllowsTerminalWildcardWhenEnabled()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.*"]);
        Assert.Empty(pathErrors);
        var issues = service.CollectProjectionFieldIssues<NestedPerson>(specs);
        Assert.Empty(issues);
    }

    [Fact]
    public void ResolveProjectedFields_RejectsWildcardsInPathWhenDisabled()
    {
        var service = new ProjectionService();
        var (_, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.*"], false);
        Assert.Single(pathErrors);
        Assert.Contains("wildcard", pathErrors[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryBuildSqlProjectionExpression_NineIndependentFields_BuildsAndConvertsInSlotOrder()
        => AssertWideIndependentFieldsConvert(9);

    [Fact]
    public void TryBuildSqlProjectionExpression_ThirtyIndependentFields_BuildsAndConvertsInSlotOrder()
        => AssertWideIndependentFieldsConvert(30);

    [Fact]
    public void TryBuildSqlProjectionExpression_NineSiblingCollectionLeaves_MergesOneSlotAndConverts()
    {
        var service = new ProjectionService();
        var paths = Enumerable.Range(0, 9).Select(i => $"Items.C{i}").ToArray();
        var (specs, pathErrors) = service.ResolveProjectedFields<SiblingRoot>(paths);
        Assert.Empty(pathErrors);
        var build = service.TryBuildSqlProjectionExpression<SiblingRoot>(specs);
        Assert.NotNull(build.Projection);
        Assert.NotNull(build.ConversionPlan);
        Assert.Single(build.ConversionPlan!.Slots);
        Assert.IsType<SqlProjectionMergedCollectionSlot>(build.ConversionPlan.Slots[0]);

        var entity = new SiblingRoot {
            Items = [
                CreateSiblingItem("a"),
                CreateSiblingItem("b")
            ]
        };
        var raw = build.Projection!.Compile()(entity);
        var converted = service.ConvertSqlProjectedResults([raw], specs, build.ConversionPlan);
        var dict = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(converted[0]);
        for (var i = 0; i < 9; i++) {
            var list = Assert.IsAssignableFrom<IList>(dict[$"Items.C{i}"]);
            Assert.Equal(2, list.Count);
            Assert.Equal($"a{i}", list[0]?.ToString());
            Assert.Equal($"b{i}", list[1]?.ToString());
        }
    }

    private static void AssertWideIndependentFieldsConvert(int fieldCount)
    {
        var service = new ProjectionService();
        var paths = WidePaths(fieldCount);
        var (specs, pathErrors) = service.ResolveProjectedFields<WideRoot>(paths);
        Assert.Empty(pathErrors);
        var build = service.TryBuildSqlProjectionExpression<WideRoot>(specs);
        Assert.NotNull(build.Projection);
        Assert.NotNull(build.ConversionPlan);
        Assert.Equal(fieldCount, build.ConversionPlan!.Slots.Count);

        var entity = new WideRoot();
        for (var i = 0; i < fieldCount; i++)
            typeof(WideRoot).GetProperty(WidePath(i))!.SetValue(entity, $"v{i}");

        var raw = build.Projection!.Compile()(entity);
        Assert.False(raw is ITuple);
        var converted = service.ConvertSqlProjectedResults([raw], specs, build.ConversionPlan);
        var dict = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(converted[0]);
        for (var i = 0; i < fieldCount; i++)
            Assert.Equal($"v{i}", dict[paths[i]]?.ToString());
    }

    private static string[] WidePaths(int count) => Enumerable.Range(0, count).Select(WidePath).ToArray();

    private static string WidePath(int i) => $"F{i:D2}";

    private static SiblingItem CreateSiblingItem(string prefix)
        => new() {
            C0 = $"{prefix}0",
            C1 = $"{prefix}1",
            C2 = $"{prefix}2",
            C3 = $"{prefix}3",
            C4 = $"{prefix}4",
            C5 = $"{prefix}5",
            C6 = $"{prefix}6",
            C7 = $"{prefix}7",
            C8 = $"{prefix}8"
        };

    private sealed class Charge
    {
        public string? Code { get; set; }

        public string? Number { get; set; }
    }

    private sealed class Docket
    {
        public Guid Id { get; set; }

        public List<Charge> DocketCharges { get; set; } = [];
    }

    private sealed class NestedAddr
    {
        public string? StreetName { get; set; }

        public string? StreetType { get; set; }
    }

    private sealed class NestedContactAddr
    {
        public string? Id { get; set; }

        public NestedAddr? Address { get; set; }
    }

    private sealed class NestedPerson
    {
        public List<NestedContactAddr> ContactAddresses { get; set; } = [];
    }

    private sealed class SiblingItem
    {
        public string? C0 { get; set; }

        public string? C1 { get; set; }

        public string? C2 { get; set; }

        public string? C3 { get; set; }

        public string? C4 { get; set; }

        public string? C5 { get; set; }

        public string? C6 { get; set; }

        public string? C7 { get; set; }

        public string? C8 { get; set; }
    }

    private sealed class SiblingRoot
    {
        public List<SiblingItem> Items { get; set; } = [];
    }

    private sealed class WideRoot
    {
        public string? F00 { get; set; }

        public string? F01 { get; set; }

        public string? F02 { get; set; }

        public string? F03 { get; set; }

        public string? F04 { get; set; }

        public string? F05 { get; set; }

        public string? F06 { get; set; }

        public string? F07 { get; set; }

        public string? F08 { get; set; }

        public string? F09 { get; set; }

        public string? F10 { get; set; }

        public string? F11 { get; set; }

        public string? F12 { get; set; }

        public string? F13 { get; set; }

        public string? F14 { get; set; }

        public string? F15 { get; set; }

        public string? F16 { get; set; }

        public string? F17 { get; set; }

        public string? F18 { get; set; }

        public string? F19 { get; set; }

        public string? F20 { get; set; }

        public string? F21 { get; set; }

        public string? F22 { get; set; }

        public string? F23 { get; set; }

        public string? F24 { get; set; }

        public string? F25 { get; set; }

        public string? F26 { get; set; }

        public string? F27 { get; set; }

        public string? F28 { get; set; }

        public string? F29 { get; set; }
    }
}