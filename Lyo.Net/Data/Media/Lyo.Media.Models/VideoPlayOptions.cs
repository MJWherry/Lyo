using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>
/// Playback knobs for <see cref="IVideoPlayer" />. NoDisplay defaults true; set it false to opt in to a window.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record VideoPlayOptions
{
    /// <summary>If true, playback runs with no video window. Starts as true. Set false to open a window.</summary>
    public bool NoDisplay { get; init; } = true;

    /// <summary>If true, the player exits when the file ends. Starts as true.</summary>
    public bool AutoExit { get; init; } = true;

    /// <summary>How stream and bytes overloads feed the player. Starts as <see cref="MediaIoMode.TempFile" />. Use <see cref="MediaIoMode.Pipe" /> so large video is not spooled.</summary>
    public MediaIoMode IoMode { get; init; } = MediaIoMode.TempFile;

    /// <summary>Input seek. Null means start at the beginning.</summary>
    public TimeSpan? StartTime { get; init; }

    /// <summary>How long to play. Null means play to the end.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Checks <see cref="IoMode" />.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNotDefined(IoMode);

    /// <inheritdoc />
    public override string ToString()
        => $"VideoPlayOptions(NoDisplay={NoDisplay}, AutoExit={AutoExit}, IoMode={IoMode}, StartTime={StartTime}, Duration={Duration})";
}
