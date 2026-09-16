using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Lyo.Desktop;

namespace Lyo.Screenshot.App;

/// <summary>Puts the shot on the clipboard as the image and as the file. Linux X11 serves both targets. Other desktops use the shared clipboard helper or Avalonia.</summary>
internal static class ShotClipboard
{
    public static async Task CopyShotAsync(IClipboard? clipboard, IStorageProvider? storage, string? path, byte[] png, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path) && await Task.Run(() => X11ShotClipboard.TryOffer(path, png), ct).ConfigureAwait(false))
            return;

        if (OperatingSystem.IsLinux() && DesktopClipboard.CanSetPng()) {
            await DesktopClipboard.SetPngAsync(png, ct).ConfigureAwait(false);
            return;
        }

        if (clipboard is null)
            throw new InvalidOperationException("Clipboard is not available.");

        IStorageFile? file = null;
        if (storage != null && !string.IsNullOrEmpty(path))
            file = await storage.TryGetFileFromPathAsync(path);
        using var stream = new MemoryStream(png);
        var bitmap = new Bitmap(stream);
        var transfer = new DataTransfer();
        var item = new DataTransferItem();
        item.SetBitmap(bitmap);
        if (file != null)
            item.SetFile(file);
        if (!string.IsNullOrEmpty(path))
            item.SetText(path);
        transfer.Add(item);
        await clipboard.SetDataAsync(transfer).ConfigureAwait(true);
        await clipboard.FlushAsync().ConfigureAwait(true);
    }
}
