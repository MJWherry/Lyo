using Lyo.Exceptions;

namespace Lyo.Desktop;

/// <summary>Picks the clipboard tool for an OS. Windows text reads and PNG writes stay on the Win32 clipboard.</summary>
internal static class ClipboardCommand
{
    private const string LinuxMissing = "Clipboard tool not found. Install one of: wl-copy (Wayland), xclip, or xsel.";

    internal static ClipboardRoute TextWrite(DesktopOs os, Func<string, bool> exists)
    {
        ArgumentHelpers.ThrowIfNull(exists);
        if (os == DesktopOs.Windows)
            return ToolOrThrow(exists, "clip", "", "clip.exe", "Clipboard tool not found. Expected 'clip.exe' on PATH.");

        if (os == DesktopOs.Mac)
            return ToolOrThrow(exists, "pbcopy", "", "pbcopy", "Clipboard tool not found. Expected 'pbcopy' on PATH.");

        if (exists("wl-copy"))
            return new ClipboardRoute.Tool("wl-copy", "");

        if (exists("xclip"))
            return new ClipboardRoute.Tool("xclip", "-selection clipboard");

        if (exists("xsel"))
            return new ClipboardRoute.Tool("xsel", "--clipboard --input");

        throw new InvalidOperationException(LinuxMissing);
    }

    internal static ClipboardRoute TextRead(DesktopOs os, Func<string, bool> exists)
    {
        ArgumentHelpers.ThrowIfNull(exists);
        if (os == DesktopOs.Windows)
            return new ClipboardRoute.Native();

        if (os == DesktopOs.Mac)
            return ToolOrThrow(exists, "pbpaste", "", "pbpaste", "Clipboard tool not found. Expected 'pbpaste' on PATH.");

        if (exists("wl-paste"))
            return new ClipboardRoute.Tool("wl-paste", "");

        if (exists("xclip"))
            return new ClipboardRoute.Tool("xclip", "-selection clipboard -o");

        if (exists("xsel"))
            return new ClipboardRoute.Tool("xsel", "--clipboard --output");

        throw new InvalidOperationException("Clipboard tool not found. Install one of: wl-paste (Wayland), xclip, or xsel.");
    }

    internal static ClipboardRoute PngWrite(DesktopOs os, bool wayland, Func<string, bool> exists)
    {
        ArgumentHelpers.ThrowIfNull(exists);
        if (os == DesktopOs.Windows)
            return new ClipboardRoute.Native();

        if (os != DesktopOs.Linux)
            throw new InvalidOperationException("Clipboard tool not found. Expected 'pngpaste' on PATH.");

        if (wayland && exists("wl-copy"))
            return new ClipboardRoute.Tool("wl-copy", "--type image/png");

        if (exists("xclip"))
            return new ClipboardRoute.Tool("xclip", "-selection clipboard -t image/png");

        if (exists("wl-copy"))
            return new ClipboardRoute.Tool("wl-copy", "--type image/png");

        throw new InvalidOperationException(LinuxMissing);
    }

    private static ClipboardRoute ToolOrThrow(Func<string, bool> exists, string fileName, string arguments, string alternate, string missing)
    {
        if (exists(fileName) || exists(alternate))
            return new ClipboardRoute.Tool(fileName, arguments);

        throw new InvalidOperationException(missing);
    }
}