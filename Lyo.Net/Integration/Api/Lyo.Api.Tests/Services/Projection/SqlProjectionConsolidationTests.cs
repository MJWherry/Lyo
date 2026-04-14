using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Project;

namespace Lyo.Api.Tests.Services.Projection;

/// <summary>Verifies sibling collection fields share one LINQ Select (single join) in SQL projection.</summary>
public sealed class SqlProjectionConsolidationTests
{
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
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>([
            "ContactAddresses.Id",
            "ContactAddresses.Address.StreetType",
            "ContactAddresses.Address.StreetName",
        ]);
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
        var issues = service.CollectProjectionFieldIssues<NestedPerson>(specs, allowSelectWildcards: true);
        Assert.Empty(issues);
    }

    [Fact]
    public void ResolveProjectedFields_RejectsWildcardsInPathWhenDisabled()
    {
        var service = new ProjectionService();
        var (_, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.*"], allowSelectWildcards: false);
        Assert.Single(pathErrors);
        Assert.Contains("wildcard", pathErrors[0].Description, StringComparison.OrdinalIgnoreCase);
    }
}
