using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Inbox;

namespace Lyo.Diagnostic.Tests;

public sealed class InMemoryErrorInboxTests
{
    [Fact]
    public async Task RecordAsync_TrimsOldest_WhenOverMax()
    {
        var inbox = new InMemoryErrorInbox(new() { MaxOccurrences = 2 });
        var r1 = Sample("a");
        var r2 = Sample("b");
        var r3 = Sample("c");
        await inbox.RecordAsync(r1, TestCt);
        await inbox.RecordAsync(r2, TestCt);
        await inbox.RecordAsync(r3, TestCt);
        Assert.False(inbox.TryGetOccurrence(r1.OccurrenceId, out var _));
        Assert.True(inbox.TryGetOccurrence(r2.OccurrenceId, out var _));
        Assert.True(inbox.TryGetOccurrence(r3.OccurrenceId, out var _));
    }

    [Fact]
    public async Task ListGroups_AggregatesByKey()
    {
        var inbox = new InMemoryErrorInbox();
        var ct = TestCt;
        await inbox.RecordAsync(Sample("x"), ct);
        await inbox.RecordAsync(Sample("y"), ct);
        var groups = inbox.ListGroups(TimeSpan.FromHours(1));
        Assert.Single(groups);
        Assert.Equal(2, groups[0].OccurrenceCount);
    }

    private static ErrorOccurrenceRecord Sample(string occurrenceId, ExceptionKind kind = ExceptionKind.Unknown)
        => new(occurrenceId, "FP", kind.ToString(), "Svc", ExceptionSeverity.High, DateTimeOffset.UtcNow, null, null, "msg", "test", 0, null);
}