namespace Lyo.Web.Components.Media;

/// <summary>HTML <c>&lt;track&gt;</c> kind for a caption file.</summary>
public enum MediaCaptionKind
{
    /// <summary>Dialogue subtitles. HTML kind <c>subtitles</c>.</summary>
    Subtitles,

    /// <summary>Captions, including non-speech audio. HTML kind <c>captions</c>.</summary>
    Captions,

    /// <summary>Text description of the video. HTML kind <c>descriptions</c>.</summary>
    Descriptions,

    /// <summary>Chapter navigation. HTML kind <c>chapters</c>.</summary>
    Chapters,

    /// <summary>Timed metadata. HTML kind <c>metadata</c>.</summary>
    Metadata
}
