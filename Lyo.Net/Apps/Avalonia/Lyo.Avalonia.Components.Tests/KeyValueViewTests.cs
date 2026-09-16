namespace Lyo.Desktop.Components.Tests;

public class KeyValueViewTests
{
    [Fact]
    public void Text_KeepsTheValueForTheClipboard()
    {
        var entry = KeyValueEntry.FromText("automation_run_id", "  01a1171a-0fe0-7524-8ac8-dc4d861b83a0  ");

        Assert.Equal("automation_run_id", entry.Key);
        Assert.Equal("01a1171a-0fe0-7524-8ac8-dc4d861b83a0", entry.Text);
        Assert.Equal("01a1171a-0fe0-7524-8ac8-dc4d861b83a0", entry.ClipboardText);
    }

    [Fact]
    public void Text_Blank_HasNothingToCopy()
    {
        var entry = KeyValueEntry.FromText("session_id", "   ");

        Assert.Null(entry.Text);
        Assert.Null(entry.ClipboardText);
    }

    [Fact]
    public void Format_WritesEachKeyAndItsCopyText()
    {
        var text = KeyValueView.Format([
            KeyValueEntry.FromText("automation_run_id", "run-1"),
            KeyValueEntry.FromText("empty", null)
        ]);

        Assert.Equal("automation_run_id\trun-1" + Environment.NewLine + "empty" + Environment.NewLine, text);
    }
}
