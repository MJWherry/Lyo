using Lyo.Common;

namespace Lyo.ChangeTracker.Tests;

public class ChangeRecordTests
{
    [Fact]
    public void ChangeRecord_WithDefaults_HasGeneratedIdAndTimestamp()
    {
        var change = new ChangeRecord(
            EntityRef.ForKey("Order", "123"), new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" });

        Assert.NotEqual(Guid.Empty, change.Id);
        Assert.True(change.Timestamp <= DateTime.UtcNow.AddSeconds(1) && change.Timestamp >= DateTime.UtcNow.AddSeconds(-1));
        Assert.Equal("Order", change.ForEntity.EntityType);
        Assert.Equal("123", change.ForEntity.EntityId);
        Assert.Single(change.OldValues);
        Assert.Single(change.ChangedProperties);
    }

    [Fact]
    public void ChangeRecord_WithExpression_CreatesCopyWithNewValues()
    {
        var original = new ChangeRecord(
            EntityRef.ForKey("Order", "123"), new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" }) {
            ChangeType = "Updated"
        };

        var updated = original with { Message = "Order submitted" };
        Assert.Equal("Updated", updated.ChangeType);
        Assert.Equal("Order submitted", updated.Message);
        Assert.Equal(original.ForEntity, updated.ForEntity);
        Assert.Equal(original.OldValues, updated.OldValues);
        Assert.Equal(original.ChangedProperties, updated.ChangedProperties);
    }
}