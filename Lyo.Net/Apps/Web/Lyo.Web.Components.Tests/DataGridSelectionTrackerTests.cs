using Lyo.Web.Components.DataGrid;

namespace Lyo.Web.Components.Tests;

public class DataGridSelectionTrackerTests
{
    private sealed record Row(int Id);

    private static object[] Key(Row row) => [row.Id];

    [Fact]
    public void ReplacePage_ThenItemsOnPage_KeepsOnlyOverlappingKeysAfterReorder()
    {
        var tracker = new DataGridSelectionTracker();
        Row[] pageA = [new(1), new(2), new(3)];
        tracker.ReplacePage(pageA, pageA, Key);

        Row[] reordered = [new(3), new(1), new(2)];
        var selected = tracker.ItemsOnPage(reordered, Key);

        Assert.Equal(3, tracker.Count);
        Assert.Equal(3, selected.Count);
        Assert.All(reordered, row => Assert.Contains(row, selected));
    }

    [Fact]
    public void ReplacePage_ThenItemsOnPage_DoesNotSelectEntireNewPage()
    {
        var tracker = new DataGridSelectionTracker();
        Row[] pageA = [new(1), new(2), new(3)];
        tracker.ReplacePage(pageA, pageA, Key);

        Row[] pageB = [new(3), new(4), new(5)];
        var selected = tracker.ItemsOnPage(pageB, Key);

        Assert.Equal(3, tracker.Count);
        Assert.Single(selected);
        Assert.Contains(pageB[0], selected);
        Assert.DoesNotContain(pageB[1], selected);
        Assert.DoesNotContain(pageB[2], selected);
    }

    [Fact]
    public void ReplacePage_EmptySelected_RemovesThisPageKeysOnly()
    {
        var tracker = new DataGridSelectionTracker();
        Row[] page1 = [new(1), new(2)];
        Row[] page2 = [new(3), new(4)];
        tracker.ReplacePage(page1, page1, Key);
        tracker.ReplacePage(page2, page2, Key);

        tracker.ReplacePage(page1, [], Key);

        Assert.Equal(2, tracker.Count);
        Assert.True(tracker.Contains([3]));
        Assert.True(tracker.Contains([4]));
        Assert.False(tracker.Contains([1]));
        Assert.Empty(tracker.ItemsOnPage(page1, Key));
    }

    [Fact]
    public void ItemsOnPage_WithoutReplacePage_PreservesKeysAcrossLoads()
    {
        var tracker = new DataGridSelectionTracker();
        Row[] page1 = [new(1), new(2)];
        tracker.ReplacePage(page1, page1, Key);

        Row[] page2 = [new(3), new(4)];
        var selectedOnPage2 = tracker.ItemsOnPage(page2, Key);

        Assert.Equal(2, tracker.Count);
        Assert.Empty(selectedOnPage2);
        Assert.Equal(2, tracker.ItemsOnPage<Row>([new(1), new(2)], Key).Count);
    }

    [Fact]
    public void Remove_DeletedKeys_ClearsPageSelection()
    {
        var tracker = new DataGridSelectionTracker();
        Row[] page = [new(1), new(2), new(3)];
        tracker.ReplacePage(page, page, Key);

        tracker.Remove([[1], [2], [3]]);

        Assert.Equal(0, tracker.Count);
        Assert.Empty(tracker.ItemsOnPage(page, Key));
    }

    [Fact]
    public void Toggle_AddsAndRemovesKey()
    {
        var tracker = new DataGridSelectionTracker();
        tracker.Toggle([7]);
        Assert.True(tracker.Contains([7]));
        tracker.Toggle([7]);
        Assert.False(tracker.Contains([7]));
        Assert.Equal(0, tracker.Count);
    }

    [Fact]
    public void ReplaceAll_RestoresPersistedKeys()
    {
        var tracker = new DataGridSelectionTracker();
        tracker.ReplaceAll([[1], [9]]);

        Assert.Equal(2, tracker.Count);
        Assert.Equal([1, 9], tracker.ItemsOnPage<Row>([new(1), new(2), new(9)], Key).Select(r => r.Id).OrderBy(id => id));
    }

    [Fact]
    public void Clear_DropsEveryKey()
    {
        var tracker = new DataGridSelectionTracker();
        tracker.ReplaceAll([[1], [2]]);
        tracker.Clear();
        Assert.Equal(0, tracker.Count);
        Assert.Empty(tracker.Keys);
    }
}

