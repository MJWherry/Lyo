using System.Collections.ObjectModel;
using Lyo.Desktop.Components;

namespace Lyo.FileManager.App;

/// <summary>One folder in the tree. Children load the first time it is expanded.</summary>
public sealed class FolderNode
{
    public string Name { get; init; } = "";

    public string? Prefix { get; init; }

    public FileManagerEntry? Entry { get; init; }

    public ObservableCollection<FolderNode> Children { get; } = [];

    public bool Loaded { get; set; }

    /// <summary>Kept on the node so a tab switch can show the same branches open.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>Dummy child that gives an unloaded folder an expander.</summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>Canonical node when this row is a search projection.</summary>
    public FolderNode? Source { get; init; }

    /// <summary>False for the expander placeholder.</summary>
    public bool ShowLabel => !IsPlaceholder;

    /// <summary>Icon for this row. A missing entry is a folder.</summary>
    public FileGlyph Glyph => FileKind.Glyph(Entry?.Name ?? Name, Entry?.IsDirectory != false);
}
