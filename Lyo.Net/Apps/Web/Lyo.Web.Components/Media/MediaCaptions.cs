using Lyo.Exceptions;

namespace Lyo.Web.Components.Media;

/// <summary>Turns caption tracks into the attributes an HTML <c>&lt;track&gt;</c> expects.</summary>
public static class MediaCaptions
{
    /// <summary>Drops tracks with a blank <see cref="MediaCaptionTrack.Src" />, trims the rest, and keeps a single default. A null list is empty.</summary>
    public static IReadOnlyList<MediaCaptionTrack> Resolve(IReadOnlyList<MediaCaptionTrack>? tracks)
    {
        if (tracks is null || tracks.Count == 0)
            return [];

        var resolved = new List<MediaCaptionTrack>(tracks.Count);
        var hasDefault = false;
        foreach (var track in tracks) {
            var src = MediaSource.Normalize(track.Src);
            if (src is null)
                continue;

            var isDefault = track.Default && !hasDefault;
            if (isDefault)
                hasDefault = true;

            resolved.Add(track with { Src = src, Default = isDefault });
        }

        return resolved;
    }

    /// <summary>HTML <c>kind</c> token for <paramref name="kind" />.</summary>
    public static string HtmlKind(MediaCaptionKind kind)
    {
        ArgumentHelpers.ThrowIfNotDefined(kind);
        return kind switch {
            MediaCaptionKind.Subtitles => "subtitles",
            MediaCaptionKind.Captions => "captions",
            MediaCaptionKind.Descriptions => "descriptions",
            MediaCaptionKind.Chapters => "chapters",
            MediaCaptionKind.Metadata => "metadata",
            var _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Caption kind is not mapped.")
        };
    }

    /// <summary>Attribute bag for one resolved track. <c>default</c> is included only when the track is the default.</summary>
    public static IReadOnlyDictionary<string, object> TrackAttributes(MediaCaptionTrack track)
    {
        ArgumentHelpers.ThrowIfNull(track);
        var attributes = new Dictionary<string, object> { ["src"] = track.Src, ["kind"] = HtmlKind(track.Kind) };
        if (!string.IsNullOrWhiteSpace(track.Label))
            attributes["label"] = track.Label;

        if (!string.IsNullOrWhiteSpace(track.Language))
            attributes["srclang"] = track.Language;

        if (track.Default)
            attributes["default"] = true;

        return attributes;
    }
}