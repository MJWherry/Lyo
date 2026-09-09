using System.Linq.Expressions;
using System.Text.Json;
using Lyo.Web.Components.DataGrid;

namespace Lyo.Web.Components.Tests;

public class RelatedProjectionTests
{
    private sealed class AddressLookup
    {
        public Guid Id { get; init; }
        public string? FullAddress { get; init; }
        public string? City { get; init; }
        public List<string>? Tags { get; init; }
        public NestedLookup? Nested { get; init; }
    }

    private sealed class NestedLookup
    {
        public string? Name { get; init; }
    }

    private sealed class PersonRow
    {
        public Guid? PlaceOfBirthAddressId { get; init; }
        public Guid? OtherAddressId { get; init; }
    }

    [Fact]
    public void InferSelect_ExplicitList_UnionsId()
    {
        var select = RelatedProjection.InferSelect(typeof(AddressLookup), ["FullAddress", "City"]);

        Assert.Equal(["FullAddress", "City", "Id"], select);
    }

    [Fact]
    public void InferSelect_ExplicitAlreadyHasId_DoesNotDuplicate()
    {
        var select = RelatedProjection.InferSelect(typeof(AddressLookup), ["Id", "City"]);

        Assert.Equal(["Id", "City"], select);
    }

    [Fact]
    public void InferSelect_FromType_IncludesScalarsAndSkipsCollectionsAndNested()
    {
        var select = RelatedProjection.InferSelect(typeof(AddressLookup));

        Assert.Contains("Id", select);
        Assert.Contains("FullAddress", select);
        Assert.Contains("City", select);
        Assert.DoesNotContain("Tags", select);
        Assert.DoesNotContain("Nested", select);
    }

    [Fact]
    public void CollectIds_DropsEmptyAndDedupesAcrossFields()
    {
        var a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var b = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var rows = new object?[] {
            new PersonRow { PlaceOfBirthAddressId = a, OtherAddressId = a },
            new PersonRow { PlaceOfBirthAddressId = b },
            new PersonRow { PlaceOfBirthAddressId = Guid.Empty },
            new PersonRow()
        };

        var ids = RelatedProjection.CollectIds(rows, ["PlaceOfBirthAddressId", "OtherAddressId"]);

        Assert.Equal(2, ids.Count);
        Assert.Contains(a, ids);
        Assert.Contains(b, ids);
    }

    [Fact]
    public void ChunkKeys_SplitsIntoObjectArrayRows()
    {
        var ids = Enumerable.Range(1, 250).Select(i => (object)i).ToList();

        var chunks = RelatedProjection.ChunkKeys(ids);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(100, chunks[0].Count);
        Assert.Equal(100, chunks[1].Count);
        Assert.Equal(50, chunks[2].Count);
        Assert.Equal([1], chunks[0][0]);
        Assert.Equal([101], chunks[1][0]);
    }

    [Fact]
    public void GroupVisible_SameRouteAndType_UnionsIdsAndSelect()
    {
        var registry = new RelatedColumnRegistry();
        registry.Register("PlaceOfBirthAddressId", "PersonAddress", typeof(AddressLookup), ["FullAddress", "Id"]);
        registry.Register("OtherAddressId", "PersonAddress", typeof(AddressLookup), ["City", "Id"]);
        registry.Register("EmergencyContactPersonId", "Person", typeof(PersonRow), ["Id"]);

        var groups = RelatedProjection.GroupVisible(registry);

        Assert.Equal(2, groups.Count);
        var address = Assert.Single(groups, g => g.Route == "PersonAddress");
        Assert.Equal(typeof(AddressLookup), address.ResType);
        Assert.Contains("FullAddress", address.Select);
        Assert.Contains("City", address.Select);
        Assert.Contains("Id", address.Select);
        Assert.Contains("PlaceOfBirthAddressId", address.Fields);
        Assert.Contains("OtherAddressId", address.Fields);
    }

    [Fact]
    public void GroupVisible_HiddenColumn_IsExcluded()
    {
        var registry = new RelatedColumnRegistry();
        registry.Register("PlaceOfBirthAddressId", "PersonAddress", typeof(AddressLookup), ["Id"], hidden: true);
        registry.Register("OtherAddressId", "PersonAddress", typeof(AddressLookup), ["City", "Id"]);

        var groups = RelatedProjection.GroupVisible(registry);

        var group = Assert.Single(groups);
        Assert.Equal(["OtherAddressId"], group.Fields);
    }

    [Fact]
    public void Deserialize_JsonElement_MapsToType()
    {
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        using var doc = JsonDocument.Parse($$"""{"id":"{{id}}","fullAddress":"1 Main St","city":"Boston"}""");

        var related = RelatedProjection.Deserialize<AddressLookup>(doc.RootElement);

        Assert.NotNull(related);
        Assert.Equal(id, related.Id);
        Assert.Equal("1 Main St", related.FullAddress);
        Assert.Equal("Boston", related.City);
    }

    [Fact]
    public void Deserialize_Dictionary_MapsToType()
    {
        var id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var row = new Dictionary<string, object?> { ["Id"] = id, ["FullAddress"] = "2 Oak Ave", ["City"] = "Austin" };

        var related = RelatedProjection.Deserialize<AddressLookup>(row);

        Assert.NotNull(related);
        Assert.Equal(id, related.Id);
        Assert.Equal("2 Oak Ave", related.FullAddress);
    }

    [Fact]
    public void GetFieldValue_TypedRow_ReadsPropertyPath()
    {
        var id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var row = new PersonRow { PlaceOfBirthAddressId = id };

        var value = RelatedProjection.GetFieldValue(row, "PlaceOfBirthAddressId");

        Assert.Equal(id, value);
    }

    [Fact]
    public void PropertyPath_FromLambda_ReturnsName()
    {
        Expression<Func<PersonRow, object?>> expr = x => x.PlaceOfBirthAddressId;

        Assert.Equal("PlaceOfBirthAddressId", RelatedProjection.PropertyPath(expr));
    }

    [Fact]
    public void IdKey_EmptyGuid_ReturnsNull()
    {
        Assert.Null(RelatedProjection.IdKey(Guid.Empty));
        Assert.Null(RelatedProjection.IdKey(null));
        Assert.Equal("55555555-5555-5555-5555-555555555555", RelatedProjection.IdKey(Guid.Parse("55555555-5555-5555-5555-555555555555")));
    }

    [Fact]
    public void Lookup_SetAndGet_RoundTrips()
    {
        var lookup = new RelatedEntityLookup();
        var id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var address = new AddressLookup { Id = id, City = "Denver" };
        lookup.Set("PersonAddress", typeof(AddressLookup), id, address);

        Assert.True(lookup.TryGet<AddressLookup>("PersonAddress", id, out var found));
        Assert.Equal("Denver", found?.City);
        Assert.False(lookup.TryGet<AddressLookup>("PersonAddress", Guid.NewGuid(), out _));
    }
}
