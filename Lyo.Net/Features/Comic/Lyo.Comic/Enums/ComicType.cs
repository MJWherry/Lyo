namespace Lyo.Comic.Enums;

/// <summary>The publication style and region of origin for a comic series.</summary>
public enum ComicType
{
    Unknown = 0,

    /// <summary>Japanese comics; typically read right-to-left.</summary>
    Manga = 1,

    /// <summary>Korean comics; typically read left-to-right or as vertical-scroll webtoons.</summary>
    Manhwa = 2,

    /// <summary>Chinese comics (manhua); typically read left-to-right.</summary>
    Manhua = 3,

    /// <summary>Vertical-scroll digital comics, predominantly Korean/global origin.</summary>
    Webtoon = 4,

    /// <summary>Western-origin comics (US, European, etc.); typically read left-to-right.</summary>
    Western = 5
}
