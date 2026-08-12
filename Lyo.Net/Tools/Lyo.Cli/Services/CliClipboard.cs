using System.Diagnostics;
using System.Runtime.InteropServices;
using Lyo.Exceptions;

namespace Lyo.Cli.Services;

/// <summary>Copies text to the system clipboard via native OS utilities (no NuGet clipboard package).</summary>
internal static class CliClipboard
{
    /// <summary>Pipes <paramref name="text" /> into the platform clipboard tool.</summary>
    public static async Task CopyAsync(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text);
        var (fileName, arguments) = ResolveClipboardCommand() ?? throw new InvalidOperationException(MissingToolMessage());
        using var process = new Process {
            StartInfo = new() {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try {
            if (!process.Start())
                throw new InvalidOperationException($"Failed to start clipboard tool '{fileName}'.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException) {
            throw new InvalidOperationException($"Failed to start clipboard tool '{fileName}': {ex.Message}", ex);
        }

        await process.StandardInput.WriteAsync(text.AsMemory(), ct).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
        process.StandardInput.Close();
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        if (process.ExitCode != 0) {
            var err = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"Clipboard tool '{fileName}' exited with code {process.ExitCode}.{(string.IsNullOrWhiteSpace(err) ? "" : " " + err.Trim())}");
        }
    }

    private static (string FileName, string Arguments)? ResolveClipboardCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return CommandExists("clip") || CommandExists("clip.exe") ? ("clip", "") : null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return CommandExists("pbcopy") ? ("pbcopy", "") : null;

        if (CommandExists("wl-copy"))
            return ("wl-copy", "");

        if (CommandExists("xclip"))
            return ("xclip", "-selection clipboard");

        if (CommandExists("xsel"))
            return ("xsel", "--clipboard --input");

        return null;
    }

    private static string MissingToolMessage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Clipboard tool not found. Expected 'clip.exe' on PATH.";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "Clipboard tool not found. Expected 'pbcopy' on PATH.";

        return "Clipboard tool not found. Install one of: wl-copy (Wayland), xclip, or xsel.";
    }

    private static bool CommandExists(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return false;

        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [""];

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)) {
            foreach (var ext in extensions) {
                var candidate = Path.Combine(dir, name + (ext.StartsWith('.') || ext.Length == 0 ? ext : "." + ext));
                if (File.Exists(candidate))
                    return true;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(Path.Combine(dir, name)))
                return true;
        }

        return false;
    }
}