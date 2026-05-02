using Lyo.Common.Identifiers;

namespace Lyo.ChangeTracker.Tests;

public class NullChangeTrackerTests
{
    [Fact]
    public async Task NullChangeTracker_DiscardsAndReturnsEmptyResults()
    {
        var tracker = NullChangeTracker.Instance;
        var entityRef = EntityRef.ForKey("Order", "123");
        var change = new ChangeRecord(entityRef, new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" });
        await tracker.RecordChangeAsync(change, TestContext.Current.CancellationToken);
        await tracker.RecordChangeAsync(change, TestContext.Current.CancellationToken);
        await tracker.RecordChangesAsync([change], TestContext.Current.CancellationToken);
        await tracker.RecordChangesAsync([change], TestContext.Current.CancellationToken);
        var byId = await tracker.GetByIdAsync(change.Id, TestContext.Current.CancellationToken);
        var byEntity = await tracker.GetForEntityAsync(entityRef, TestContext.Current.CancellationToken);
        var byType = await tracker.GetForEntityTypeAsync("Order", ct: TestContext.Current.CancellationToken);
        Assert.Null(byId);
        Assert.Empty(byEntity);
        Assert.Empty(byType);
    }
}