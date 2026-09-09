namespace Lyo.Comic.Web.Components;

/// <summary>Comic browse grids and lists layout mode.</summary>
public enum ComicBrowseViewMode
{
    /// <summary>1 column on mobile, 4 on desktop (large cards).</summary>
    GridLarge,

    /// <summary>2 columns on mobile, 8 on desktop (compact cards).</summary>
    GridSmall,

    /// <summary>Vertical rows with no cover thumbs.</summary>
    ListNoImage,

    /// <summary>Compact single-line rows.</summary>
    ListLine
}