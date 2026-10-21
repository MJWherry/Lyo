using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Desktop;

/// <summary>Reads and writes the desktop clipboard. Linux and macOS use the host clipboard tools. Windows text reads and PNG writes use Win32.</summary>
public static class DesktopClipboard
{
    /// <summary>Writes <paramref name="text" />. Linux prefers <c>wl-copy</c>, then <c>xclip</c>, then <c>xsel</c>. Windows uses <c>clip.exe</c>. macOS uses <c>pbcopy</c>.</summary>
    public static async Task SetTextAsync(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text);
        var route = ClipboardCommand.TextWrite(CurrentOs(), CommandExists);
        if (route is not ClipboardRoute.Tool tool)
            throw new InvalidOperationException("Text write on this desktop uses a clipboard tool.");

        await WriteToolAsync(tool, Encoding.UTF8.GetBytes(text), ct).ConfigureAwait(false);
    }

    /// <summary>Reads plain text. Windows uses the Win32 clipboard. Linux prefers <c>wl-paste</c>, then <c>xclip</c>, then <c>xsel</c>. macOS uses <c>pbpaste</c>.</summary>
    public static async Task<string> GetTextAsync(CancellationToken ct = default)
    {
        var route = ClipboardCommand.TextRead(CurrentOs(), CommandExists);
        if (route is ClipboardRoute.Native) {
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException("Native clipboard text is only available on Windows.");

            return WindowsClipboard.GetText();
        }

        return await ReadToolAsync((ClipboardRoute.Tool)route, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PNG. On Wayland, <c>wl-copy --type image/png</c> is preferred. Otherwise Linux uses <c>xclip</c> with <c>image/png</c>, then <c>wl-copy</c>. Windows places a
    /// bitmap on the clipboard.
    /// </summary>
    public static async Task SetPngAsync(byte[] png, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(png);
        var route = ClipboardCommand.PngWrite(CurrentOs(), Wayland(), CommandExists);
        if (route is ClipboardRoute.Native) {
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException("Native clipboard images are only available on Windows.");

            WindowsClipboard.SetPng(png);
            return;
        }

        await WriteToolAsync((ClipboardRoute.Tool)route, png, ct).ConfigureAwait(false);
    }

    /// <summary>True when <see cref="SetPngAsync" /> has a tool or a native bitmap path on this desktop.</summary>
    public static bool CanSetPng()
    {
        try {
            ClipboardCommand.PngWrite(CurrentOs(), Wayland(), CommandExists);
            return true;
        }
        catch (InvalidOperationException) {
            return false;
        }
    }

    private static DesktopOs CurrentOs()
    {
        if (OperatingSystem.IsWindows())
            return DesktopOs.Windows;

        if (OperatingSystem.IsMacOS())
            return DesktopOs.Mac;

        return DesktopOs.Linux;
    }

    private static bool Wayland() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    private static async Task WriteToolAsync(ClipboardRoute.Tool tool, byte[] payload, CancellationToken ct)
    {
        using var process = Start(tool, true, false);
        await process.StandardInput.BaseStream.WriteAsync(payload, ct).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
        process.StandardInput.Close();
        var exited = await Task.Run(() => process.WaitForExit(2000), ct).ConfigureAwait(false);
        if (!exited)
            return;

        if (process.ExitCode == 0)
            return;

        var err = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        throw new InvalidOperationException($"Clipboard tool '{tool.FileName}' exited with code {process.ExitCode}.{(string.IsNullOrWhiteSpace(err) ? "" : " " + err.Trim())}");
    }

    private static async Task<string> ReadToolAsync(ClipboardRoute.Tool tool, CancellationToken ct)
    {
        using var process = Start(tool, false, true);
        var text = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        if (process.ExitCode == 0)
            return text;

        var err = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        throw new InvalidOperationException($"Clipboard tool '{tool.FileName}' exited with code {process.ExitCode}.{(string.IsNullOrWhiteSpace(err) ? "" : " " + err.Trim())}");
    }

    private static Process Start(ClipboardRoute.Tool tool, bool redirectInput, bool redirectOutput)
    {
        var process = new Process {
            StartInfo = new() {
                FileName = tool.FileName,
                Arguments = tool.Arguments,
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try {
            if (!process.Start())
                throw new InvalidOperationException($"Failed to start clipboard tool '{tool.FileName}'.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException) {
            throw new InvalidOperationException($"Failed to start clipboard tool '{tool.FileName}': {ex.Message}", ex);
        }

        return process;
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
                var suffix = ext.StartsWith('.') || ext.Length == 0 ? ext : "." + ext;
                if (File.Exists(Path.Combine(dir, name + suffix)))
                    return true;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(Path.Combine(dir, name)))
                return true;
        }

        return false;
    }
}