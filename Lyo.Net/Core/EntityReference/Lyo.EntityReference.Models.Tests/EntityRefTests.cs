using System.Text.Json;

// ReSharper disable PropertyCanBeMadeInitOnly.Local

namespace Lyo.EntityReference.Models.Tests;

public class EntityRefTests
{
    [Fact]
    public void For_WithEntityAndSelector_SingleKey_CreatesCorrectRef()
    {
        var docket = new TestDocket { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000") };
        var entityRef = EntityRef.For(docket, d => d.Id);
        Assert.Equal(typeof(TestDocket).FullName, entityRef.EntityType);
        Assert.Equal(docket.Id.ToString(), entityRef.EntityId);
    }

    [Fact]
    public void For_WithEntityAndSelector_CompositeKeys_OrdersConsistently()
    {
        var order = new TestOrder { OrderId = "ord-1", LineId = "line-2" };
        var entityRef = EntityRef.For(order, o => new object[] { o.OrderId, o.LineId });
        Assert.Equal("line-2:ord-1", entityRef.EntityId);
    }

    [Fact]
    public void For_WithEntityAndSelector_NullEntity_Throws()
    {
        TestDocket? docket = null;
        Assert.Throws<ArgumentNullException>(() => EntityRef.For(docket!, d => d.Id));
    }

    [Fact]
    public void For_WithEntityAndSelector_NullSelector_Throws()
    {
        var docket = new TestDocket { Id = Guid.NewGuid() };
        Assert.Throws<ArgumentNullException>(() => EntityRef.For(docket, null!));
    }

    [Fact]
    public void For_WithEntityAndSelector_SelectorReturnsNull_Throws()
    {
        var docket = new TestDocket { Id = Guid.NewGuid() };
        Assert.Throws<ArgumentNullException>(() => EntityRef.For(docket, _ => null));
    }

    [Fact]
    public void ForGuid_CreatesCorrectRef()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var entityRef = EntityRef.ForGuid("Docket", guid);
        Assert.Equal("Docket", entityRef.EntityType);
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", entityRef.EntityId);
    }

    [Fact]
    public void ForKey_CreatesCorrectRef()
    {
        var entityRef = EntityRef.ForKey("User", "123");
        Assert.Equal("User", entityRef.EntityType);
        Assert.Equal("123", entityRef.EntityId);
    }

    [Fact]
    public void ForT_SingleKey_UsesFullTypeName()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var entityRef = EntityRef.For<TestEntity>(guid);
        Assert.Equal(typeof(TestEntity).FullName, entityRef.EntityType);
        Assert.Equal(guid.ToString(), entityRef.EntityId);
    }

    [Fact]
    public void ForT_CompositeKeys_OrdersConsistently()
    {
        var a = EntityRef.For<TestEntity>("ord-1", "line-2");
        var b = EntityRef.For<TestEntity>("line-2", "ord-1");
        Assert.Equal(a.EntityId, b.EntityId);
        Assert.Equal("line-2:ord-1", a.EntityId);
    }

    [Fact]
    public void ForT_EmptyKeys_Throws() => Assert.Throws<ArgumentException>(() => EntityRef.For<TestEntity>());

    [Fact]
    public void ForT_NullKeys_Throws() => Assert.Throws<ArgumentNullException>(() => EntityRef.For<TestEntity>(null!));

    [Fact]
    public void Record_ValueEquality()
    {
        var a = new EntityRef("User", "123");
        var b = new EntityRef("User", "123");
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ForT_Uses_EntityRefLogicalTypeAttribute_WhenPresent()
    {
        var r = EntityRef.For<AttributedEntity>(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"));
        Assert.Equal("Stable.Order", r.EntityType);
    }

    [Fact]
    public void ForT_CompositeKeys_WithColonInSegment_EncodesAndSplitsRoundTrip()
    {
        var r = EntityRef.For<TestEntity>("z", "a:b");
        Assert.Equal(@"a\:b:z", r.EntityId);
        var parts = EntityRefCompositeEncoding.SplitComposite(r.EntityId);
        Assert.Equal(new[] { "a:b", "z" }, parts);

        var joined = EntityRefCompositeEncoding.JoinComposite(parts.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(r.EntityId, joined);
    }

    [Fact]
    public void OpaqueToken_RoundTrips()
    {
        var r = EntityRef.ForKey("Kind", "id:with:parts");
        var token = r.ToOpaqueToken();
        Assert.True(EntityRef.TryParseOpaque(token.AsSpan(), out var parsed));
        Assert.Equal(r, parsed);
        Assert.Equal(r, EntityRef.ParseOpaque(token));
    }

    [Fact]
    public void EntityRefJsonConverter_RoundTrips()
    {
        var opts = new JsonSerializerOptions { Converters = { new EntityRefJsonConverter() } };
        var r = new EntityRef("X", "y");
        var json = JsonSerializer.Serialize(r, opts);
        Assert.Contains("entityType", json, StringComparison.Ordinal);
        Assert.Equal(r, JsonSerializer.Deserialize<EntityRef>(json, opts));
    }

    [Fact]
    public void EntityRefTypeGuard_AllowsKnown()
    {
        var allowed = new HashSet<string> { "A", "B" };
        EntityRefTypeGuard.EnsureKnown(EntityRef.ForKey("A", "1"), allowed);
    }

    [Fact]
    public void EntityRefTypeGuard_RejectsUnknown()
    {
        var allowed = new HashSet<string> { "A" };
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EntityRefTypeGuard.EnsureKnown(EntityRef.ForKey("Z", "1"), allowed));
    }

    [EntityRefLogicalType("Stable.Order")]
    private sealed class AttributedEntity;

    private sealed class TestEntity;

    private sealed class TestDocket
    {
        public Guid Id { get; set; }
    }

    private sealed class TestOrder
    {
        public string OrderId { get; set; } = "";

        public string LineId { get; set; } = "";
    }
}
