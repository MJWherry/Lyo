# Lyo.Screenshot.App

Avalonia window named `lyo-shot`. Pick Still, GIF, or Receipt, arm a region, a window, or the full screen, then Start. An optional delay counts down first. GIF and receipt keep
grabbing until Stop. The window hides for the shot so it is not in the image. A GIF or receipt stays on screen when the global shortcuts are not registered, so Stop remains
available. Start and Stop are buttons and global hotkeys (default `Ctrl+Shift+S` and `Ctrl+Shift+X`). Save file and the delay are on the main row, next to Start. A Dock button on
the right of that type row docks the window to the nearest screen edge as a camera semicircle. Drag the bubble onto any edge. Click it to open the window again. The last shot stays
in the window until the next one. Stop brings the window back before a receipt is stitched. Copy places the image and the file on the clipboard, and the preview can be dragged.
Open file and Open folder work even when Save file is off, using a temp directory. Closing the window deletes that scratch folder. The clipboard still has the image. Frame rate,
hotkeys, the save folder, copy, open, the cursor, and always on top live under More. Settings persist under `$XDG_CONFIG_HOME/lyo/screenshot.json` or
`%AppData%\Lyo\screenshot.json`.

## Features

- **Region, window, or full screen.** Drag a rectangle on a dim overlay, or pick an open window. Escape cancels the overlay only. A window is measured again when the shot starts.
- **Delay.** The start row has a delay of 0, 1, 2, 3, 5, or 10 seconds, next to Save file. Stop or Escape cancels the countdown.
- **Start and stop.** Scroll the page, then Stop. Escape also stops a GIF or receipt while it is recording.
- **Dock.** The Dock button on the type row collapses the window into a semicircle on the nearest edge of the screen. The camera is the window icon and the bubble. Drag moves the
  bubble to any edge. Hover opens Region, Window, and Full screen. Region draws the overlay and then takes the shot, Window opens the window list and takes the shot after a click,
  and Full screen takes the shot immediately. A click on the bubble opens the window again.
- **After the shot.** The image stays in the window until the next shot. Copy puts the image and the file on the clipboard. Drag the preview to drop the file. The file is written
  even when Save file is off, under the temp directory. Closing the window deletes that folder. Open file, Open folder, and Save stay available afterward. Save writes it into the
  save folder. Copy and open under More still run automatically when those boxes are checked.

## Examples

### Run the window

```text
dotnet run --project Lyo.Net/Apps/Screenshot/Lyo.Screenshot.App
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Avalonia.Components` (direct, lyo)
- `Lyo.Desktop` (direct, lyo)
- `Lyo.Screenshot` (direct, lyo)
- `Avalonia` `12.1.3` (direct, third-party)
- `Avalonia.Desktop` `12.1.3` (direct, third-party)
- `Avalonia.Fonts.Inter` `12.1.3` (direct, third-party)
- `Avalonia.Themes.Fluent` `12.1.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `SixLabors.ImageSharp` `3.1.12` (transitive, third-party)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)