namespace Lyo.Audit.Tests;

public class AuditChangeRecordTests
{
    [Fact]
    public void AuditChange_WithDefaults_HasEmptyCollectionsAndGeneratedId()
    {
        var change = new AuditChange("Test.Entity, Test", new Dictionary<string, object?>(), new Dictionary<string, object?>());
        Assert.NotEqual(Guid.Empty, change.Id);
        Assert.True(change.Timestamp <= DateTime.UtcNow.AddSeconds(1) && change.Timestamp >= DateTime.UtcNow.AddSeconds(-1));
        Assert.Empty(change.OldValues);
        Assert.Empty(change.ChangedProperties);
    }

    [Fact]
    public void AuditChange_WithExpression_CreatesCopyWithNewValues()
    {
        var change = new AuditChange("Test.Entity, Test", new Dictionary<string, object?> { ["A"] = 1 }, new Dictionary<string, object?> { ["A"] = 2 });
        var updated = change with { TypeAssemblyFullName = "Other.Entity, Other" };
        Assert.Equal("Other.Entity, Other", updated.TypeAssemblyFullName);
        Assert.Equal(change.OldValues, updated.OldValues);
        Assert.Equal(change.ChangedProperties, updated.ChangedProperties);
    }

    [Fact]
    public void AuditChange_ValueEquality_ComparesByValues()
    {
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var oldValues = new Dictionary<string, object?> { ["x"] = 1 };
        var changedProps = new Dictionary<string, object?> { ["x"] = 2 };
        var a = new AuditChange("T, A", oldValues, changedProps) { Id = id, Timestamp = timestamp };
        var b = new AuditChange("T, A", oldValues, changedProps) { Id = id, Timestamp = timestamp };
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}