using System.Collections;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Formatter;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests.Services.Projection;

public sealed class SiblingCollectionMergeTests
{
    private sealed class Charge
    {
        public string? Description { get; set; }
        public string? Code { get; set; }
        public string? Number { get; set; }
    }

    private sealed class Docket
    {
        public Guid Id { get; set; }
        public List<Charge> DocketCharges { get; set; } = [];
    }

    /// <summary>Nested scope: collection → navigation → leaves zip when only one Select path exists but computed adds a parallel column.</summary>
    [Fact]
    public void MergeSiblingCollectionProjectionRows_Zips_OneSelectPlusParallelComputed_NestedPrefix()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.Address.CreatedTimestamp"]);
        Assert.Empty(pathErrors);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["ContactAddresses.Address.CreatedTimestamp"] = new[] { "2026-01-01", "2026-01-02" },
            ["ContactAddresses.Address.a"] = new[] { "Broad St", "Main Ave" }
        };
        var items = new List<object?> { row };
        service.MergeSiblingCollectionProjectionRows(items, typeof(NestedPerson), specs, zipSiblingCollectionSelections: true);

        var outRow = Assert.IsType<Dictionary<string, object?>>(items[0]);
        Assert.False(outRow.ContainsKey("ContactAddresses.Address.CreatedTimestamp"));
        Assert.True(outRow.TryGetValue("ContactAddresses.Address", out var mergedObj));
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(mergedObj);
        Assert.Equal(2, list!.Count);
        Assert.Equal("2026-01-01", list[0]["CreatedTimestamp"]?.ToString());
        Assert.Equal("Broad St", list[0]["a"]?.ToString());
        Assert.Equal("2026-01-02", list[1]["CreatedTimestamp"]?.ToString());
        Assert.Equal("Main Ave", list[1]["a"]?.ToString());
    }

    private sealed class NestedAddr
    {
        public string? CreatedTimestamp { get; set; }
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

    /// <summary><c>contactaddresses.*</c> + computed parallel <c>contactaddresses.address.n</c> → nested <c>Address.n</c> per row.</summary>
    [Fact]
    public void MergeSiblingCollectionProjectionRows_Zips_ScopeWildcardWithNestedParallelComputedColumn()
    {
        var formatter = new FormatterService();
        var service = new ProjectionService(formatter);
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.*"]);
        Assert.Empty(pathErrors);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["ContactAddresses.*"] = new object?[] {
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                    ["Id"] = "1",
                    ["Type"] = "Home",
                    ["Address"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["StreetType"] = "St" }
                },
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                    ["Id"] = "2",
                    ["Type"] = "Work",
                    ["Address"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["StreetType"] = "Ave" }
                }
            }
        };
        var items = new List<object?> { row };
        var computedFields = new List<ComputedField> { new("n", "{contactaddresses.address.streettype}") };
        var afterComputed = service.ApplyComputedFields(items, computedFields, specs);
        service.MergeSiblingCollectionProjectionRows(afterComputed, typeof(NestedPerson), specs, zipSiblingCollectionSelections: true);

        var outRow = Assert.IsType<Dictionary<string, object?>>(afterComputed[0]);
        Assert.False(outRow.ContainsKey("ContactAddresses.*"));
        Assert.False(outRow.ContainsKey("ContactAddresses.Address"));
        Assert.True(outRow.TryGetValue("ContactAddresses", out var mergedObj));
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(mergedObj);
        Assert.Equal(2, list!.Count);
        Assert.Equal("Home", list[0]["Type"]?.ToString());
        var addr0 = Assert.IsAssignableFrom<Dictionary<string, object?>>(list[0]["Address"]);
        Assert.Equal("St", addr0["n"]?.ToString());
        var addr1 = Assert.IsAssignableFrom<Dictionary<string, object?>>(list[1]["Address"]);
        Assert.Equal("Ave", addr1["n"]?.ToString());
    }

    /// <summary>
    /// Mixed depths under the same collection (id on row + address.streetname) → one <c>ContactAddresses</c> array, not parallel <c>contactaddresses.id</c> + <c>ContactAddresses.Address</c>.
    /// </summary>
    [Fact]
    public void MergeSiblingCollectionProjectionRows_UnifiedRoot_ZipsIdWithNestedAddressAndComputed()
    {
        var formatter = new FormatterService();
        var service = new ProjectionService(formatter);
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>([
            "ContactAddresses.Id",
            "ContactAddresses.Address.StreetName",
            "ContactAddresses.Address.StreetType"
        ]);
        Assert.Empty(pathErrors);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["ContactAddresses.Id"] = new[] { "ca-1", "ca-2" },
            ["ContactAddresses.Address.StreetName"] = new[] { "Hollywood", "Oak" },
            ["ContactAddresses.Address.StreetType"] = new[] { "Blvd", "Dr" }
        };
        var items = new List<object?> { row };
        var computedFields = new List<ComputedField> { new("n", "{contactaddresses.address.streettype}") };
        var afterComputed = service.ApplyComputedFields(items, computedFields, specs);
        service.MergeSiblingCollectionProjectionRows(afterComputed, typeof(NestedPerson), specs, zipSiblingCollectionSelections: true);

        var outRow = Assert.IsType<Dictionary<string, object?>>(afterComputed[0]);
        Assert.False(outRow.ContainsKey("ContactAddresses.Id"));
        Assert.False(outRow.ContainsKey("ContactAddresses.Address"));
        Assert.False(outRow.ContainsKey("contactaddresses.id"));
        Assert.True(outRow.TryGetValue("ContactAddresses", out var mergedObj));
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(mergedObj);
        Assert.Equal(2, list!.Count);
        Assert.Equal("ca-1", list[0]["Id"]?.ToString());
        var addr0 = Assert.IsAssignableFrom<Dictionary<string, object?>>(list[0]["Address"]);
        Assert.Equal("Hollywood", addr0["StreetName"]?.ToString());
        Assert.Equal("Blvd", addr0["n"]?.ToString());
        Assert.Equal("ca-2", list[1]["Id"]?.ToString());
        var addr1 = Assert.IsAssignableFrom<Dictionary<string, object?>>(list[1]["Address"]);
        Assert.Equal("Oak", addr1["StreetName"]?.ToString());
        Assert.Equal("Dr", addr1["n"]?.ToString());
    }

    /// <summary>
    /// With <c>contactaddresses.*</c> plus explicit nested leaves, do not create a separate merge at <c>contactaddresses.address</c>
    /// (empty <c>ContactAddresses.Address</c> at root); zip stays under <c>ContactAddresses</c> with nested <c>address.n</c>.
    /// </summary>
    [Fact]
    public void MergeSiblingCollectionProjectionRows_ScopeWildcardPlusNestedAddressLeaves_NoDuplicateContactAddressesAddressMerge()
    {
        var formatter = new FormatterService();
        var service = new ProjectionService(formatter);
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.*", "ContactAddresses.Address.StreetName"]);
        Assert.Empty(pathErrors);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["ContactAddresses.*"] = new object?[] {
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                    ["Id"] = "1",
                    ["Type"] = "Home",
                    ["Address"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                        ["StreetName"] = "Hollywood",
                        ["StreetType"] = "St"
                    }
                }
            },
            ["ContactAddresses.Address.StreetName"] = new[] { "Hollywood" },
            ["ContactAddresses.Address.StreetType"] = new[] { "St" }
        };
        var items = new List<object?> { row };
        var computedFields = new List<ComputedField> { new("n", "{contactaddresses.address.streettype}") };
        var afterComputed = service.ApplyComputedFields(items, computedFields, specs);
        service.MergeSiblingCollectionProjectionRows(afterComputed, typeof(NestedPerson), specs, zipSiblingCollectionSelections: true);

        var outRow = Assert.IsType<Dictionary<string, object?>>(afterComputed[0]);
        Assert.False(outRow.ContainsKey("ContactAddresses.Address"));
        Assert.DoesNotContain(outRow, kvp =>
            string.Equals(kvp.Key, "ContactAddresses.Address", StringComparison.OrdinalIgnoreCase)
            && kvp.Value is IList l && l.Count == 0);
        Assert.True(outRow.TryGetValue("ContactAddresses", out var mergedObj));
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(mergedObj);
        Assert.Single(list!);
        var addr = Assert.IsAssignableFrom<Dictionary<string, object?>>(list![0]["Address"]);
        Assert.Equal("Hollywood", addr["StreetName"]?.ToString());
        Assert.Equal("St", addr["n"]?.ToString());
    }

    [Fact]
    public void MergeSiblingCollectionProjectionRows_Zips_WildcardScopePlusParallelComputed()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<NestedPerson>(["ContactAddresses.Address.*"]);
        Assert.Empty(pathErrors);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["ContactAddresses.Address.*"] = new object?[] {
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["StreetType"] = "St", ["StreetName"] = "Main" },
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["StreetType"] = "Ave", ["StreetName"] = "Oak" }
            },
            ["ContactAddresses.Address.a"] = new[] { "Main St", "Oak Ave" }
        };
        var items = new List<object?> { row };
        service.MergeSiblingCollectionProjectionRows(items, typeof(NestedPerson), specs, zipSiblingCollectionSelections: true);

        var outRow = Assert.IsType<Dictionary<string, object?>>(items[0]);
        Assert.False(outRow.ContainsKey("ContactAddresses.Address.*"));
        Assert.False(outRow.ContainsKey("ContactAddresses.Address.a"));
        Assert.True(outRow.TryGetValue("ContactAddresses.Address", out var mergedObj));
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(mergedObj);
        Assert.Equal(2, list!.Count);
        Assert.Equal("St", list[0]["StreetType"]?.ToString());
        Assert.Equal("Main St", list[0]["a"]?.ToString());
        Assert.Equal("Ave", list[1]["StreetType"]?.ToString());
        Assert.Equal("Oak Ave", list[1]["a"]?.ToString());
    }

    [Fact]
    public void MergeSiblingCollectionProjectionRows_ZipsParallelCollectionColumns()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<Docket>(["DocketCharges.Code", "DocketCharges.Number"]);
        Assert.Empty(pathErrors);
        var entity = new Docket {
            DocketCharges = [
                new Charge { Code = "A", Number = "1" },
                new Charge { Code = "B", Number = "2" }
            ]
        };

        var items = service.ProjectEntities([entity], specs, QueryIncludeFilterMode.Full, new([], default));
        service.MergeSiblingCollectionProjectionRows(items, typeof(Docket), specs, zipSiblingCollectionSelections: true);

        var row = Assert.IsType<Dictionary<string, object?>>(items[0]);
        Assert.False(row.ContainsKey("DocketCharges.Code"));
        Assert.False(row.ContainsKey("DocketCharges.Number"));
        Assert.True(row.TryGetValue("DocketCharges", out var merged));
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(merged);
        Assert.Equal(2, list!.Count);
        Assert.Equal("A", list[0]["Code"]);
        Assert.Equal("1", list[0]["Number"]);
        Assert.Equal("B", list[1]["Code"]);
        Assert.Equal("2", list[1]["Number"]);
    }

    [Fact]
    public void MergeSiblingCollectionProjectionRows_DoesNotMerge_WhenOnlyOneSiblingField()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<Docket>(["DocketCharges.Code"]);
        Assert.Empty(pathErrors);
        // Single-path ProjectEntities returns the raw value (not a dictionary row); use an explicit row to test merge behavior.
        var items = new List<object?> {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["DocketCharges.Code"] = new[] { "A" } }
        };
        service.MergeSiblingCollectionProjectionRows(items, typeof(Docket), specs, zipSiblingCollectionSelections: true);

        var row = Assert.IsType<Dictionary<string, object?>>(items[0]);
        Assert.True(row.ContainsKey("DocketCharges.Code"));
        Assert.False(row.ContainsKey("DocketCharges"));
    }

    [Fact]
    public void MergeSiblingCollectionProjectionRows_SkipsZip_WhenOptionFalse()
    {
        var service = new ProjectionService();
        var (specs, pathErrors) = service.ResolveProjectedFields<Docket>(["DocketCharges.Code", "DocketCharges.Number"]);
        Assert.Empty(pathErrors);
        var entity = new Docket {
            DocketCharges = [
                new Charge { Code = "A", Number = "1" }
            ]
        };

        var items = service.ProjectEntities([entity], specs, QueryIncludeFilterMode.Full, new([], default));
        service.MergeSiblingCollectionProjectionRows(items, typeof(Docket), specs, zipSiblingCollectionSelections: false);

        var row = Assert.IsType<Dictionary<string, object?>>(items[0]);
        Assert.True(row.ContainsKey("DocketCharges.Code"));
        Assert.True(row.ContainsKey("DocketCharges.Number"));
        Assert.False(row.ContainsKey("DocketCharges"));
    }

    [Fact]
    public void MergeSiblingCollectionProjectionRows_ZipsCollectionParallelComputedIntoEachElement()
    {
        var formatter = new FormatterService();
        var service = new ProjectionService(formatter);
        var (specs, pathErrors) = service.ResolveProjectedFields<Docket>(["DocketCharges.Description", "DocketCharges.Number"]);
        Assert.Empty(pathErrors);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["DocketCharges.Description"] = new[] { "A", "B" },
            ["DocketCharges.Number"] = new[] { 1, 2 }
        };
        var items = new List<object?> { row };
        var computedFields = new List<ComputedField> { new("a", "{docketcharges.description} {docketcharges.number}") };
        var afterComputed = service.ApplyComputedFields(items, computedFields, specs);
        service.MergeSiblingCollectionProjectionRows(afterComputed, typeof(Docket), specs, zipSiblingCollectionSelections: true);

        var outRow = Assert.IsType<Dictionary<string, object?>>(afterComputed[0]);
        Assert.False(outRow.ContainsKey("a"));
        Assert.True(outRow.TryGetValue("DocketCharges", out var mergedObj));
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(mergedObj);
        Assert.Equal(2, list!.Count);
        Assert.Equal("A 1", list[0]["a"]?.ToString());
        Assert.Equal("B 2", list[1]["a"]?.ToString());
    }

    [Fact]
    public void StripAutoDerivedDependencyLeavesFromMergedCollections_OmitsTemplateOnlyColumnsFromZippedCharges()
    {
        var formatter = new FormatterService();
        var service = new ProjectionService(formatter);
        var (specs, pathErrors) = service.ResolveProjectedFields<Docket>(["DocketCharges.Description", "DocketCharges.Number"]);
        Assert.Empty(pathErrors);
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["DocketCharges.Description"] = new[] { "A", "B" },
            ["DocketCharges.Number"] = new[] { 1, 2 }
        };
        var items = new List<object?> { row };
        var computedFields = new List<ComputedField> { new("a", "{docketcharges.description} {docketcharges.number}") };
        var afterComputed = service.ApplyComputedFields(items, computedFields, specs);
        service.MergeSiblingCollectionProjectionRows(afterComputed, typeof(Docket), specs, zipSiblingCollectionSelections: true);

        service.StripAutoDerivedDependencyLeavesFromMergedCollections(
            afterComputed,
            specs,
            ["DocketCharges.Description", "DocketCharges.Number"]);

        var outRow = Assert.IsType<Dictionary<string, object?>>(afterComputed[0]);
        var list = Assert.IsAssignableFrom<IList<Dictionary<string, object?>>>(outRow["DocketCharges"]);
        Assert.Equal(2, list!.Count);
        Assert.False(list[0].ContainsKey("Description"));
        Assert.False(list[0].ContainsKey("Number"));
        Assert.True(list[0].ContainsKey("a"));
        Assert.Equal("A 1", list[0]["a"]?.ToString());
    }
}
