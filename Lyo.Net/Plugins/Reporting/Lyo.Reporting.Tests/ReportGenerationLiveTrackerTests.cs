using Lyo.Reporting.Postgres;

namespace Lyo.Reporting.Tests;

/// <summary>
/// The tracker is what lets stuck-run recovery tell "no progress since" from "still working". a generation row carries no heartbeat. These pin the scope semantics recovery
/// depends on.
/// </summary>
public class ReportGenerationLiveTrackerTests
{
    [Fact]
    public void Track_MarksLiveUntilTheScopeIsDisposed()
    {
        var tracker = new ReportGenerationLiveTracker();
        var id = Guid.NewGuid();
        Assert.False(tracker.IsLive(id));

        var scope = tracker.Track(id);
        Assert.True(tracker.IsLive(id));
        Assert.Contains(id, tracker.LiveIds);

        scope.Dispose();
        Assert.False(tracker.IsLive(id));
        Assert.Empty(tracker.LiveIds);
    }

    [Fact]
    public void Track_HandlesConcurrentGenerationsIndependently()
    {
        var tracker = new ReportGenerationLiveTracker();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        using var firstScope = tracker.Track(first);
        var secondScope = tracker.Track(second);
        Assert.Equal(2, tracker.LiveIds.Count);

        secondScope.Dispose();
        Assert.True(tracker.IsLive(first));
        Assert.False(tracker.IsLive(second));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var tracker = new ReportGenerationLiveTracker();
        var id = Guid.NewGuid();
        var scope = tracker.Track(id);
        scope.Dispose();
        scope.Dispose();
        Assert.False(tracker.IsLive(id));
    }

    /// <summary>Recovery snapshots <c>LiveIds</c> once per pass, so the snapshot cannot change underneath it when a generation finishes.</summary>
    [Fact]
    public void LiveIds_IsASnapshot()
    {
        var tracker = new ReportGenerationLiveTracker();
        var id = Guid.NewGuid();
        var scope = tracker.Track(id);
        var snapshot = tracker.LiveIds;
        scope.Dispose();
        Assert.Contains(id, snapshot);
    }
}
