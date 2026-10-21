namespace Lyo.FFmpeg;

/// <summary>Concat demuxer list lines.</summary>
internal static class FFmpegConcatList
{
    public static string FormatLine(string path)
    {
        var escaped = path.Replace("'", "'\\''", StringComparison.Ordinal);
        return $"file '{escaped}'";
    }

    public static async Task<string> WriteAsync(IReadOnlyList<string> inputs, CancellationToken ct)
    {
        var path = FFmpegTempHelper.CreateTempFilePath(".txt");
        var lines = inputs.Select(FormatLine);
        await File.WriteAllLinesAsync(path, lines, ct).ConfigureAwait(false);
        return path;
    }
}