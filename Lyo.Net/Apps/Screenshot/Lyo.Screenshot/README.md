# Lyo.Screenshot

Capture a rectangle until stop, then write one PNG, a looping GIF, or one tall receipt image. `ScreenGrabber.Create` uses GDI on Windows, XGetImage when `DISPLAY` is set, and `grim` on Wayland when X is not. `ShotSession` pulls an `IFrameSource` until the stop token fires. A GIF encodes each frame as it is captured and returns the first frame as a PNG preview. A grab rectangle that hangs off the screen is clipped to the display. A receipt writes each frame to a temp file under `ShotScratch.Directory` and drops it before the next frame, then stitches one frame at a time and deletes the file. `ShotScratch.Wipe` deletes that folder. Screen-edge placement lives in `Lyo.Desktop.Components`. Stitching matches vertical scroll and appends or prepends only the new rows. Global shortcuts, the clipboard, and the window list live in `Lyo.Desktop`. The desktop window is `lyo-shot` in `Lyo.Screenshot.App`.

## Features

- **Still, GIF, receipt.** `ShotSession.CaptureAsync` encodes the first frame, a GIF written one frame at a time, or a stitched PNG. The GIF preview is the first frame as PNG.
- **Scroll stitch.** Each frame is written to a temp file and dropped before the next. Stitching reads one frame from that file, then deletes it. Downward motion appends new rows. Upward motion prepends them. Matching stays at the top and bottom of the canvas so a long receipt does not rescan earlier rows. A frame that does not overlap those edges is appended whole. Duplicates add nothing.
- **Grabbers.** Windows BitBlt, Linux X11 `XGetImage` (optional XFixes cursor), and `grim` when there is no X display. `DesktopWindows.List` keeps titled windows from `Lyo.Desktop` and drops lyo-shot.

## Examples

### Stitch two scrolled frames

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using var first = Image.Load<Rgba32>("frame-0.png");
using var second = Image.Load<Rgba32>("frame-1.png");
using var receipt = ReceiptStitcher.Stitch([first, second]);
var png = await ShotEncoder.EncodePngAsync(receipt);
```

### Capture until stop

```csharp
using var grabber = ScreenGrabber.Create();
var session = new ShotSession(new GrabFrameSource(grabber));
var shot = await session.CaptureAsync(ShotKind.Receipt, grabber.VirtualScreen, framesPerSecond: 5, includeCursor: false, cancellationToken);
var png = shot.Bytes;
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Desktop` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `SixLabors.ImageSharp` `3.1.12` (direct, third-party)