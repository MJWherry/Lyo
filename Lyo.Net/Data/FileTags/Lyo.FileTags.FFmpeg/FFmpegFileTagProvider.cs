using System.Text;
using System.Text.Json;
using CliWrap;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Result;

namespace Lyo.FileTags.FFmpeg;

/// <summary>Parses ffprobe JSON into <see cref="FileTags" />. Format tags overwrite stream tags. Unknown names stay <see cref="FileTagKey.Custom" />.</summary>
internal static class FFmpegTagJson
{
    public static FileTags Parse(string json)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var root = document.RootElement;
        if (root.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array) {
            foreach (var stream in streams.EnumerateArray())
                CopyTags(stream, merged);
        }

        if (root.TryGetProperty("format", out var format))
            CopyTags(format, merged);

        var tags = new Dictionary<FileTagKey, string>();
        foreach (var pair in merged)
            tags[FileTagKey.TryFromId(pair.Key) ?? FileTagKey.Custom(pair.Key)] = pair.Value;

        return new(tags);
    }

    private static void CopyTags(JsonElement owner, Dictionary<string, string> merged)
    {
        if (!owner.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in tags.EnumerateObject()) {
            var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.GetRawText();
            if (!string.IsNullOrEmpty(value))
                merged[property.Name] = value;
        }
    }
}

/// <summary>Reads and writes audio and video tags through ffprobe and a stream-copy ffmpeg. Images are not claimed. Write uses <c>-c copy</c> and does not re-encode.</summary>
public sealed class FFmpegFileTagProvider : IFileTagReader, IFileTagWriter
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) {
        ".mp3",
        ".m4a",
        ".flac",
        ".wav",
        ".ogg",
        ".mp4",
        ".mov",
        ".mkv",
        ".webm"
    };

    private readonly FFmpegOptions _options;

    /// <summary>Uses <paramref name="options" /> for the ffmpeg and ffprobe paths. Null paths mean the tools are on PATH.</summary>
    public FFmpegFileTagProvider(FFmpegOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    /// <inheritdoc />
    public bool CanRead(string path) => HasExtension(path);

    /// <inheritdoc />
    public async Task<Result<FileTags>> ReadAsync(string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        var args = new[] { "-hide_banner", "-loglevel", "error", "-print_format", "json", "-show_format", "-show_streams", path };
        var run = await RunAsync(ProbePath(), args, ct).ConfigureAwait(false);
        if (run.ExitCode != 0)
            return Result<FileTags>.Failure(string.IsNullOrWhiteSpace(run.StdErr) ? "ffprobe failed." : run.StdErr.Trim(), "FileTagProbeFailed");

        try {
            return Result<FileTags>.Success(FFmpegTagJson.Parse(run.StdOut));
        }
        catch (JsonException ex) {
            return Result<FileTags>.Failure(ex, "FileTagProbeFailed");
        }
    }

    /// <inheritdoc />
    public bool CanWrite(string path) => HasExtension(path);

    /// <inheritdoc />
    public async Task<Result<FileTags>> WriteAsync(string path, FileTagWriteRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(request);
        if (!request.ClearExisting && request.Tags.Count == 0)
            return Result<FileTags>.Failure("No tags to write.", "FileTagNothingToWrite");

        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full) ?? ".";
        var temp = Path.Combine(directory, Path.GetFileNameWithoutExtension(full) + ".lyotag" + Path.GetExtension(full));
        try {
            var args = BuildWriteArguments(full, temp, request);
            var run = await RunAsync(ToolPath(), args, ct).ConfigureAwait(false);
            if (run.ExitCode != 0)
                return Result<FileTags>.Failure(string.IsNullOrWhiteSpace(run.StdErr) ? "ffmpeg tag write failed." : run.StdErr.Trim(), "FileTagWriteFailed");

            File.Move(temp, full, true);
            return Result<FileTags>.Success(new(request.Tags));
        }
        finally {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    /// <summary>Argument list for a stream-copy tag write. <see cref="FileTagWriteRequest.ClearExisting" /> emits <c>-map_metadata -1</c> before <c>-metadata</c>.</summary>
    public static IReadOnlyList<string> BuildWriteArguments(string inputPath, string outputPath, FileTagWriteRequest request)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfNull(request);
        var args = new List<string> { "-y", "-i", inputPath };
        if (request.ClearExisting) {
            args.Add("-map_metadata");
            args.Add("-1");
        }

        args.Add("-c");
        args.Add("copy");
        foreach (var pair in request.Tags) {
            args.Add("-metadata");
            args.Add($"{pair.Key.Id}={pair.Value}");
        }

        args.Add(outputPath);
        return args;
    }

    private static bool HasExtension(string path) => !string.IsNullOrWhiteSpace(path) && Extensions.Contains(Path.GetExtension(path));

    private string ToolPath() => string.IsNullOrWhiteSpace(_options.FFmpegPath) ? "ffmpeg" : _options.FFmpegPath;

    private string ProbePath() => string.IsNullOrWhiteSpace(_options.FfprobePath) ? "ffprobe" : _options.FfprobePath;

    private async Task<ToolRun> RunAsync(string executable, IReadOnlyList<string> args, CancellationToken ct)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (_options.ProcessTimeout is { } limit)
            timeout.CancelAfter(limit);

        var result = await Cli.Wrap(executable)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
            .ExecuteAsync(timeout.Token)
            .ConfigureAwait(false);

        return new(result.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private sealed record ToolRun(int ExitCode, string StdOut, string StdErr);
}