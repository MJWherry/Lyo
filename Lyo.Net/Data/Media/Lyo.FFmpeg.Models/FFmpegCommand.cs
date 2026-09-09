using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.FFmpeg.Models;

/// <summary>A finished FFmpeg or ffplay command line, ready to run through CliWrap.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FFmpegCommand
{
    /// <summary>Path to the executable.</summary>
    public string ExecutablePath { get; }

    /// <summary>Full argument string without the executable, for logs and display.</summary>
    public string Arguments { get; }

    /// <summary>Arguments as a list for CliWrap and ProcessStartInfo.ArgumentList.</summary>
    public IReadOnlyList<string> ArgumentList { get; }

    /// <summary>Builds a command from an executable and argument list. <see cref="Arguments" /> is derived for display.</summary>
    public FFmpegCommand(string executablePath, IReadOnlyList<string> argumentList)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentHelpers.ThrowIfNull(argumentList);
        ExecutablePath = executablePath;
        ArgumentList = argumentList;
        Arguments = string.Join(" ", argumentList.Select(EscapeForDisplay));
    }

    /// <summary>Executable plus arguments as one string, for display or a shell.</summary>
    public string GetFullCommand() => $"{ExecutablePath} {Arguments}";

    /// <inheritdoc />
    public override string ToString() => GetFullCommand();

    private static string EscapeForDisplay(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('\''))
            return $"\"{arg.Replace("\"", "\\\"")}\"";

        return arg;
    }
}
