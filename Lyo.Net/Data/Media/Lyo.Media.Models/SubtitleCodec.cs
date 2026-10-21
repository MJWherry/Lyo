using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>Subtitle codec token passed to <c>-c:s</c>. Lookups are thread-safe.</summary>
public sealed record SubtitleCodec
{
    private static readonly ConcurrentDictionary<string, SubtitleCodec> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, SubtitleCodec> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>MP4 timed text (<c>mov_text</c>).</summary>
    public static readonly SubtitleCodec MovText = Create("MovText", "mov_text");

    /// <summary>WebVTT (<c>webvtt</c>).</summary>
    public static readonly SubtitleCodec WebVtt = Create("WebVtt", "webvtt");

    /// <summary>ASS/SSA (<c>ass</c>).</summary>
    public static readonly SubtitleCodec Ass = Create("Ass", "ass");

    /// <summary>SubRip (<c>srt</c>).</summary>
    public static readonly SubtitleCodec Srt = Create("Srt", "srt");

    /// <summary>Stream copy (<c>copy</c>).</summary>
    public static readonly SubtitleCodec Copy = Create("Copy", "copy");

    /// <summary>Every built-in codec. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<SubtitleCodec> BuiltIns { get; } = [MovText, WebVtt, Ass, Srt, Copy];

    /// <summary>Stable display name (e.g. <c>MovText</c>).</summary>
    public string Name { get; }

    /// <summary>FFmpeg subtitle codec name.</summary>
    public string Id { get; }

    private SubtitleCodec(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a codec by <see cref="Id" />.</summary>
    public static SubtitleCodec? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a codec by display name.</summary>
    public static SubtitleCodec? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a codec for an arbitrary id. Reuses a registered instance when the id is already known.</summary>
    public static SubtitleCodec Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static SubtitleCodec Create(string name, string id) => new(name, id);
}