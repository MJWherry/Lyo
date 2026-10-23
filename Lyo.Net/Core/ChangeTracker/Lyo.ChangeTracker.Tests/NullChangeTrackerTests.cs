using Lyo.EntityReference.Models;

namespace Lyo.ChangeTracker.Tests;

public class NullChangeTrackerTests
{
    [Fact]
    public async Task NullChangeTracker_DiscardsAndReturnsEmptyResults()
    {
        var tracker = NullChangeTracker.Instance;
        var entityRef = EntityRef.ForKey("Order", "123");
        var change = new ChangeRecord(entityRef, new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" });
        await tracker.RecordChangeAsync(change, TestCt);
        await tracker.RecordChangeAsync(change, TestCt);
        await tracker.RecordChangesAsync([change], TestCt);
        await tracker.RecordChangesAsync([change], TestCt);
        var byId = await tracker.GetByIdAsync(change.Id, TestCt);
        var byEntity = await tracker.GetForEntityAsync(entityRef, TestCt);
        var byType = await tracker.GetForEntityTypeAsync("Order", ct: TestCt);
        Assert.Null(byId);
        Assert.Empty(byEntity);
        Assert.Empty(byType);
    }
}