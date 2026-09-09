using System.Runtime.InteropServices;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;

namespace Lyo.FFmpeg;

/// <summary>Classifies ffmpeg inputs so local-file existence checks do not reject pipes, URLs, or lavfi names.</summary>
internal static class FFmpegPathKind
{
    public const string PipeInput = "pipe:0";

    public const string PipeOutput = "pipe:1";

    /// <summary>True when the input should exist on the local filesystem.</summary>
    public static bool IsLocalFilePath(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (input.StartsWith("pipe:", StringComparison.OrdinalIgnoreCase))
            return false;

        if (input.Contains("://", StringComparison.Ordinal))
            return false;

        if (input.Contains('=') && !input.Contains('/') && !input.Contains('\\'))
            return false;

        return true;
    }

    public static void ThrowIfLocalFileMissing(string input)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(input);
        if (!IsLocalFilePath(input))
            return;

        ArgumentHelpers.ThrowIfFileNotFound(new FileInfo(input));
    }

    public static string ResolveFfplayPath(FFmpegOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        if (!string.IsNullOrWhiteSpace(options.FfplayPath))
            return options.FfplayPath;

        if (string.IsNullOrWhiteSpace(options.FFmpegPath))
            return "ffplay";

        var dir = Path.GetDirectoryName(options.FFmpegPath) ?? "";
        var name = "ffplay" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
        return Path.Combine(dir, name);
    }
}
