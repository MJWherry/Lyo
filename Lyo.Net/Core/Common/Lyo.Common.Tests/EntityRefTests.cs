using Lyo.Common.Identifiers;

// ReSharper disable PropertyCanBeMadeInitOnly.Local

namespace Lyo.Common.Tests;

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