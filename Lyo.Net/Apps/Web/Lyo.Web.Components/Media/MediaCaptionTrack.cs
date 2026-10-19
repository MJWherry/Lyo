namespace Lyo.Web.Components.Media;

/// <summary>One WebVTT (or other timed-text) file shown as an HTML <c>&lt;track&gt;</c>.</summary>
/// <param name="Src">Track URL. Blank values are dropped by <see cref="MediaCaptions.Resolve" />.</param>
/// <param name="Label">Human-readable name shown in the player menu.</param>
/// <param name="Language">BCP 47 language tag written to <c>srclang</c>.</param>
/// <param name="Kind">HTML track kind. Defaults to subtitles.</param>
/// <param name="Default">When true, the browser enables this track. <see cref="MediaCaptions.Resolve" /> keeps only the first default.</param>
public sealed record MediaCaptionTrack(
    string Src,
    string? Label = null,
    string? Language = null,
    MediaCaptionKind Kind = MediaCaptionKind.Subtitles,
    bool Default = false);
