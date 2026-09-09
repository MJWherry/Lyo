using Lyo.Exceptions;

namespace Lyo.Cli.Services;

/// <summary>Common file, pipe, and <c>-</c> I/O helpers used by stream-based CLI commands.</summary>
internal static class CliIO
{
    /// <summary>
    /// Opens a readable stream. A path opens that file; <c>-</c> or a missing path with redirected stdin reads standard input. Dispose the stream unless it is stdin
    /// (check <c>LeaveOpen</c> on the <see cref="OpenInput" /> result).
    /// </summary>
    public static (Stream Stream, bool LeaveOpen, string? PathOrNull) OpenInput(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && path != "-") {
            ArgumentHelpers.ThrowIf(!File.Exists(path), $"Input file not found: {path}");
            return (File.OpenRead(path), false, path);
        }

        if (path == "-" || Console.IsInputRedirected)
            return (Console.OpenStandardInput(), true, null);

        throw new InvalidOperationException("Input required: pass a file path, '-', or pipe data on stdin.");
    }

    /// <summary>
    /// Opens a writable stream. A set <paramref name="output" /> (or <c>-</c>) takes precedence; otherwise stdout if redirected or if input came from a pipe; if input was a file and
    /// stdout is a TTY, the path from <paramref name="defaultSiblingPath" /> is used.
    /// </summary>
    public static (Stream Stream, bool LeaveOpen, string? PathOrNull) OpenOutput(string? output, string? inputPath, Func<string, string>? defaultSiblingPath)
    {
        if (!string.IsNullOrWhiteSpace(output) && output != "-") {
            var dir = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            return (File.Create(output), false, output);
        }

        if (output == "-" || Console.IsOutputRedirected || inputPath is null)
            return (Console.OpenStandardOutput(), true, null);

        ArgumentHelpers.ThrowIfNull(defaultSiblingPath);
        var sibling = defaultSiblingPath(inputPath);
        var siblingDir = Path.GetDirectoryName(Path.GetFullPath(sibling));
        if (!string.IsNullOrEmpty(siblingDir))
            Directory.CreateDirectory(siblingDir);

        return (File.Create(sibling), false, sibling);
    }

    /// <summary>Writes UTF-8 text without a BOM to <paramref name="output" />, <c>-</c>, or stdout.</summary>
    public static async Task WriteTextAsync(string? output, string text, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(output) && output != "-") {
            var dir = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(output, text, ct).ConfigureAwait(false);
            return;
        }

        await Console.Out.WriteAsync(text.AsMemory(), ct).ConfigureAwait(false);
        if (!text.EndsWith('\n'))
            await Console.Out.WriteLineAsync().ConfigureAwait(false);
    }

    /// <summary>Writes one or more lines to an optional <paramref name="output" /> file, optionally copies them to the clipboard, and prints stdout unless <paramref name="quiet" />.</summary>
    public static async Task EmitTextAsync(IReadOnlyList<string> lines, string? output, bool copy, bool quiet, CancellationToken ct = default)
    {
        var text = string.Join('\n', lines);
        if (lines.Count > 0)
            text += "\n";

        if (!string.IsNullOrWhiteSpace(output) && output != "-") {
            var dir = Path.GetDirectoryName(Path.GetFullPath(output!));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(output!, text, ct).ConfigureAwait(false);
        }

        if (copy)
            await CliClipboard.CopyAsync(text.TrimEnd('\n'), ct).ConfigureAwait(false);

        if (!quiet)
            await Console.Out.WriteAsync(text.AsMemory(), ct).ConfigureAwait(false);
    }

    /// <summary>Single-line overload of <see cref="EmitTextAsync(IReadOnlyList{string}, string?, bool, bool, CancellationToken)" />.</summary>
    public static Task EmitTextAsync(string text, string? output, bool copy, bool quiet, CancellationToken ct = default) => EmitTextAsync([text], output, copy, quiet, ct);

    /// <summary>Loads the full text of a path, <c>-</c>, or redirected stdin.</summary>
    public static async Task<string> ReadAllTextAsync(string? path, CancellationToken ct = default)
    {
        var (stream, leaveOpen, _) = OpenInput(path);
        try {
            using var reader = new StreamReader(stream, leaveOpen: leaveOpen);
            return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }
        finally {
            if (!leaveOpen)
                await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public static string AppendExtension(string path, string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : path + extension;
    }

    public static string StripExtension(string path, string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path[..^extension.Length] : path;
    }
}