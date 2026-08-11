using Lyo.Exceptions;

namespace Lyo.Cli.Services;

/// <summary>Shared file / pipe / <c>-</c> input-output helpers for stream-oriented CLI commands.</summary>
internal static class CliIO
{
    /// <summary>
    /// Opens an input stream. Path opens a file; <c>-</c> or omitted with redirected stdin uses standard input.
    /// Caller must dispose the returned stream (except when it is stdin — use <see cref="OpenInput" /> result's <c>LeaveOpen</c>).
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
    /// Opens an output stream. Explicit <paramref name="output" /> (or <c>-</c>) wins; otherwise stdout when redirected or when input was a pipe;
    /// when input was a file and stdout is a TTY, writes <paramref name="defaultSiblingPath" />.
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

    /// <summary>Writes text (UTF-8, no BOM) to <paramref name="output" />, <c>-</c>, or stdout.</summary>
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

    /// <summary>
    /// Emits one or more text lines: optional <paramref name="output" /> file, optional clipboard copy, and stdout unless <paramref name="quiet" />.
    /// </summary>
    public static async Task EmitTextAsync(
        IReadOnlyList<string> lines,
        string? output,
        bool copy,
        bool quiet,
        CancellationToken ct = default)
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

    /// <summary>Same as <see cref="EmitTextAsync(IReadOnlyList{string}, string?, bool, bool, CancellationToken)" /> for a single line.</summary>
    public static Task EmitTextAsync(string text, string? output, bool copy, bool quiet, CancellationToken ct = default)
        => EmitTextAsync([text], output, copy, quiet, ct);

    /// <summary>Reads all text from a path, <c>-</c>, or redirected stdin.</summary>
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
        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? path[..^extension.Length]
            : path;
    }
}
