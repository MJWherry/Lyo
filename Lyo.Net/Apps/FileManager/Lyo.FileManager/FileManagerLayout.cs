namespace Lyo.FileManager;

/// <summary>Edge a tool window can dock to. The file list stays in the center.</summary>
public enum FileManagerDockEdge
{
    /// <summary>Left of the file list.</summary>
    Left,

    /// <summary>Right of the file list.</summary>
    Right,

    /// <summary>Under the file list.</summary>
    Bottom
}

/// <summary>Tool window. The file list is not a tool.</summary>
public enum FileManagerTool
{
    /// <summary>Folder tree.</summary>
    Tree,

    /// <summary>Selected file metadata.</summary>
    Metadata,

    /// <summary>Log lines.</summary>
    Log
}

/// <summary>One docked tool window.</summary>
public sealed class FileManagerToolWindow
{
    /// <summary>Which window this is.</summary>
    public FileManagerTool Tool { get; set; }

    /// <summary>Edge it occupies.</summary>
    public FileManagerDockEdge Edge { get; set; }

    /// <summary>False when the user has hidden it. The log starts hidden.</summary>
    public bool Visible { get; set; } = true;
}

/// <summary>Where the tree, metadata, and log sit around the file list.</summary>
public sealed class FileManagerLayout
{
    /// <summary>Width of the left edge, in pixels.</summary>
    public double Left { get; set; } = 280;

    /// <summary>Width of the right edge, in pixels.</summary>
    public double Right { get; set; } = 280;

    /// <summary>Height of the bottom edge, in pixels.</summary>
    public double Bottom { get; set; } = 160;

    /// <summary>Tree, metadata, and log. Never the file list.</summary>
    public List<FileManagerToolWindow> Tools { get; set; } = [];

    /// <summary>Tree on the left, metadata on the right, log hidden on the bottom.</summary>
    public static FileManagerLayout CreateDefault()
        => new() {
            Tools = [
                new() { Tool = FileManagerTool.Tree, Edge = FileManagerDockEdge.Left, Visible = true },
                new() { Tool = FileManagerTool.Metadata, Edge = FileManagerDockEdge.Right, Visible = true },
                new() { Tool = FileManagerTool.Log, Edge = FileManagerDockEdge.Bottom, Visible = false }
            ]
        };

    /// <summary>The window for <paramref name="tool" />.</summary>
    public FileManagerToolWindow Window(FileManagerTool tool)
        => Tools.First(item => item.Tool == tool);

    /// <summary>Edge of <paramref name="tool" />.</summary>
    public FileManagerDockEdge Edge(FileManagerTool tool) => Window(tool).Edge;

    /// <summary>Whether <paramref name="tool" /> is shown.</summary>
    public bool IsVisible(FileManagerTool tool) => Window(tool).Visible;

    /// <summary>Moves <paramref name="tool" /> onto <paramref name="edge" /> and shows it. The file list stays in the center.</summary>
    public void Move(FileManagerTool tool, FileManagerDockEdge edge)
    {
        var window = Window(tool);
        window.Edge = edge;
        window.Visible = true;
    }

    /// <summary>Shows or hides <paramref name="tool" /> without changing its edge.</summary>
    public void SetVisible(FileManagerTool tool, bool visible) => Window(tool).Visible = visible;
}

/// <summary>Servers and dock layout stored in one settings file.</summary>
public sealed class FileManagerSettingsDocument
{
    /// <summary>Saved servers.</summary>
    public List<FileManagerServer> Servers { get; set; } = [];

    /// <summary>Saved key stores. A server points at one with <see cref="FileManagerServer.KeyStoreId" />.</summary>
    public List<FileManagerKeyStore> KeyStores { get; set; } = [];

    /// <summary>Dock layout. Missing files use <see cref="FileManagerLayout.CreateDefault" />.</summary>
    public FileManagerLayout Layout { get; set; } = FileManagerLayout.CreateDefault();

    /// <summary>Screen-edge bubble. Missing files leave the window open.</summary>
    public FileManagerDockBubble Bubble { get; set; } = new();

    /// <summary>True when the folder tree also lists files. Missing files show folders only.</summary>
    public bool ShowTreeFiles { get; set; }
}

/// <summary>Where the file-manager window sits when it is docked to a screen edge.</summary>
public sealed class FileManagerDockBubble
{
    /// <summary>True when the window is the edge bubble.</summary>
    public bool Docked { get; set; }

    /// <summary>Edge as <c>ScreenEdge</c>: left 0, right 1, top 2, bottom 3.</summary>
    public int Edge { get; set; } = 1;

    /// <summary>Horizontal position of the bubble, in screen pixels.</summary>
    public int X { get; set; }

    /// <summary>Vertical position of the bubble, in screen pixels.</summary>
    public int Y { get; set; }

    /// <summary>Window X before it was docked, in screen pixels. Used to put the window back.</summary>
    public int RestX { get; set; }

    /// <summary>Window Y before it was docked, in screen pixels. Used to put the window back.</summary>
    public int RestY { get; set; }
}
