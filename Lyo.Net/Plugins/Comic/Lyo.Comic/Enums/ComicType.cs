namespace Lyo.Comic.Enums;

/// <summary>A comic series the publication style and region of origin.</summary>
public enum ComicType
{
    Unknown = 0,

    /// <summary>Manga; usually read right-to-left.</summary>
    Manga = 1,

    /// <summary>Manhwa; usually left-to-right or vertical-scroll webtoons.</summary>
    Manhwa = 2,

    /// <summary>Manhua; usually read left-to-right.</summary>
    Manhua = 3,

    /// <summary>Vertical-scroll digital comics, mostly Korean or global.</summary>
    Webtoon = 4,

    /// <summary>Western comics (US, Europe, and similar); usually left-to-right.</summary>
    Western = 5
}