namespace Lyo.Media.Models;

/// <summary>How a converter or player should read or write media that is not a seekable file path.</summary>
public enum MediaIoMode
{
    /// <summary>Stage the payload to a temp file so the CLI can seek. Default for stream and bytes overloads.</summary>
    TempFile = 0,

    /// <summary>Use <c>pipe:0</c> / <c>pipe:1</c> and stream pipes. No temp spool. Required for live streaming.</summary>
    Pipe = 1
}
