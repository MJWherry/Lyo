namespace Lyo.Comic.Web.Components;

/// <summary>Layout mode for comic browse grids and lists.</summary>
public enum ComicBrowseViewMode
{
    /// <summary>One column mobile, four columns desktop (large cards).</summary>
    GridLarge,

    /// <summary>Two columns mobile, eight columns desktop (compact cards).</summary>
    GridSmall,

    /// <summary>Vertical list rows without cover thumbnails.</summary>
    ListNoImage,

    /// <summary>Single-line compact rows.</summary>
    ListLine
}