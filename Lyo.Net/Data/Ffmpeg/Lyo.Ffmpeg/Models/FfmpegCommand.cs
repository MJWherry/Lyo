using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lyo.Ffmpeg.Models;

/// <summary>Represents a built FFmpeg command ready for execution.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FfmpegCommand
{
    /// <summary>Gets the path to the ffmpeg executable.</summary>
    public string ExecutablePath { get; }

    /// <summary>Gets the full command-line arguments (excluding executable).</summary>
    public string Arguments { get; }

    /// <summary>Gets the arguments as a list for ProcessStartInfo.ArgumentList.</summary>
    public IReadOnlyList<string> ArgumentList { get; }

    internal FfmpegCommand(string executablePath, string arguments, IReadOnlyList<string> argumentList)
    {
        ExecutablePath = executablePath;
        Arguments = arguments;
        ArgumentList = argumentList;
    }

    /// <summary>Gets the full command string (executable + arguments) for display or shell execution.</summary>
    [return: NotNullIfNotNull(nameof(ExecutablePath))]
    public string GetFullCommand() => $"{ExecutablePath} {Arguments}";

    public override string ToString() => GetFullCommand();
}