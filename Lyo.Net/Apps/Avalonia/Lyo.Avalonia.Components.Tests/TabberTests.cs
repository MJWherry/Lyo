using Avalonia.Controls;

namespace Lyo.Desktop.Components.Tests;

public class TabberTests
{
    [Fact]
    public void Add_SelectsTheFirstPage_AndParentsEachPageOnce()
    {
        var record = new TextBox();
        var tools = new TextBox();
        var tabber = new Tabber();
        tabber.Add(new() { Header = "Record", Content = record });
        tabber.Add(new() { Header = "Tools", Content = tools });
        Assert.Equal(0, tabber.SelectedIndex);
        Assert.True(record.IsVisible);
        Assert.False(tools.IsVisible);
        Assert.Same(record.Parent, tools.Parent);
    }

    [Fact]
    public void Select_ShowsThatPageWithoutReparenting()
    {
        var record = new TextBox();
        var tools = new TextBox();
        var tabber = new Tabber();
        tabber.Add(new() { Header = "Record", Content = record });
        tabber.Add(new() { Header = "Tools", Content = tools });
        var parent = record.Parent;
        tabber.Select(1);
        tabber.Select(0);
        tabber.Select(1);
        Assert.Equal(1, tabber.SelectedIndex);
        Assert.False(record.IsVisible);
        Assert.True(tools.IsVisible);
        Assert.Same(parent, record.Parent);
        Assert.Same(parent, tools.Parent);
    }

    [Fact]
    public void Close_RemovesThePageAndSelectsTheNeighbor()
    {
        var tabber = new Tabber();
        tabber.Add(new() { Header = "One", Content = new TextBox(), CanClose = true });
        tabber.Add(new() { Header = "Two", Content = new TextBox(), CanClose = true });
        tabber.Add(new() { Header = "Three", Content = new TextBox(), CanClose = true });
        tabber.Select(1);
        tabber.Close(1);
        Assert.Equal(2, tabber.Pages.Count);
        Assert.Equal("Three", tabber.Pages[tabber.SelectedIndex].Header);
        Assert.Equal(1, tabber.SelectedIndex);
    }

    [Fact]
    public void IndexAt_FindsTheHeaderAndIgnoresThePage()
    {
        var record = new TextBox();
        var tools = new TextBox();
        var tabber = new Tabber();
        tabber.Add(new() { Header = "Record", Content = record });
        tabber.Add(new() { Header = "Tools", Content = tools });
        var headers = Assert.IsType<StackPanel>(tabber.Children[0]);
        var toolsHeader = Assert.IsType<Border>(headers.Children[1]);
        var label = Assert.IsType<TextBlock>(Assert.IsType<StackPanel>(toolsHeader.Child).Children[0]);
        Assert.Equal(1, tabber.IndexAt(label));
        Assert.Equal(-1, tabber.IndexAt(record));
    }
}