using Lyo.EntityReference.Models;

namespace Lyo.Audit.Tests;

public class AuditChangeRecordTests
{
    [Fact]
    public void AuditChange_WithDefaults_HasEmptyCollectionsAndGeneratedId()
    {
        var change = new AuditChange(EntityRef.ForKey("Test.Entity", Guid.NewGuid().ToString()), new Dictionary<string, object?>(), new Dictionary<string, object?>());
        Assert.NotEqual(Guid.Empty, change.Id);
        Assert.True(change.Timestamp <= DateTime.UtcNow.AddSeconds(1) && change.Timestamp >= DateTime.UtcNow.AddSeconds(-1));
        Assert.Empty(change.OldValues);
        Assert.Empty(change.ChangedProperties);
        Assert.Null(change.Actor);
    }

    [Fact]
    public void AuditChange_WithExpression_CreatesCopyWithNewValues()
    {
        var original = EntityRef.ForKey("Test.Entity", "1");
        var replacement = EntityRef.ForKey("Other.Entity", "2");
        var change = new AuditChange(original, new Dictionary<string, object?> { ["A"] = 1 }, new Dictionary<string, object?> { ["A"] = 2 });
        var updated = change with { Entity = replacement };
        Assert.Equal(replacement, updated.Entity);
        Assert.Equal(change.OldValues, updated.OldValues);
        Assert.Equal(change.ChangedProperties, updated.ChangedProperties);
    }

    [Fact]
    public void AuditChange_ValueEquality_ComparesByValues()
    {
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var entity = EntityRef.ForKey("T", "A");
        var actor = EntityRef.ForKey("User", "u1");
        var oldValues = new Dictionary<string, object?> { ["x"] = 1 };
        var changedProps = new Dictionary<string, object?> { ["x"] = 2 };
        var a = new AuditChange(entity, oldValues, changedProps) { Id = id, Timestamp = timestamp, Actor = actor };
        var b = new AuditChange(entity, oldValues, changedProps) { Id = id, Timestamp = timestamp, Actor = actor };
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}