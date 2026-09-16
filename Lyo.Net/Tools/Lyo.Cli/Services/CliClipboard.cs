using Lyo.Desktop;

namespace Lyo.Cli.Services;

/// <summary>Sends text to the OS clipboard through <see cref="DesktopClipboard" />.</summary>
internal static class CliClipboard
{
    /// <summary>Writes <paramref name="text" /> to the host clipboard.</summary>
    public static Task CopyAsync(string text, CancellationToken ct = default) => DesktopClipboard.SetTextAsync(text, ct);
}
