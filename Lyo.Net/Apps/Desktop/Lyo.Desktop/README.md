# Lyo.Desktop

`HotkeyChord` parses a settings string such as `Ctrl+Shift+W`. `GlobalHotkeys.Create` grabs those chords on the X root window or with `RegisterHotKey`, so they fire while another
window is focused. `Bind` replaces the whole set. An empty list releases every grab. Caps Lock and Num Lock do not change which chord matches. Call
`GlobalHotkeys.InitializeThreading` before any other X11 use, including Avalonia startup. `DesktopClipboard` reads and writes text, and writes a PNG. `DesktopWindows` lists, finds,
and raises top-level windows. `DesktopInput` moves the pointer, clicks, and sends keys. Linux uses X11 and XTest. Windows uses Win32. Wayland without `DISPLAY` can still use the
clipboard tools. `X11WindowPlace` moves a mapped window when a plain configure would be dropped. `DesktopConfig.File` is the path of a JSON file under `$XDG_CONFIG_HOME/lyo` or
`%AppData%\Lyo`.

## Features

- **Chords.** `HotkeyChord.Parse` accepts modifiers in any order. A single-letter key is stored in upper case. `ToString` writes Ctrl, Shift, Alt, then Meta.
- **Grab.** `IGlobalHotkeys.Bind` replaces the grabbed chords. The same chord cannot appear twice. A chord another program already owns throws. The message is that the shortcut is
  already in use.
- **Clipboard.** `DesktopClipboard.SetTextAsync` prefers `wl-copy`, then `xclip`, then `xsel`. Windows writes with `clip.exe` and reads with Win32. `SetPngAsync` offers `image/png`
  on Linux and a bitmap on Windows.
- **Windows.** `DesktopWindows.List` returns top-level windows. `Find` matches a title substring and skips a blank title. `Activate` raises the window. An id of 0 throws.
- **Input.** `DesktopInput.Click` moves, presses, and releases. `Send` types text or one chord. Modifiers go down first and come up last.
- **Place.** `X11WindowPlace.TryPlace` sends `_NET_MOVERESIZE_WINDOW`. A width or height of 0 leaves that axis alone. `HasTitleBar` reads `_NET_FRAME_EXTENTS`.
- **Config.** `DesktopConfig.File("screenshot.json")` is `%AppData%\Lyo\screenshot.json` on Windows. Elsewhere it is `$XDG_CONFIG_HOME/lyo/screenshot.json`, or
  `~/.config/lyo/screenshot.json` when that variable is unset.

## Examples

### Parse a hotkey

```csharp
var chord = HotkeyChord.Parse("Ctrl+Shift+W");
Console.WriteLine(chord);
```

### Grab start and stop

```csharp
GlobalHotkeys.InitializeThreading();
using var keys = GlobalHotkeys.Create();
keys.Bind([
    new(HotkeyChord.Parse("Ctrl+Shift+W"), Start),
    new(HotkeyChord.Parse("Ctrl+Shift+Q"), Stop)
]);
```

### Copy text

```csharp
await DesktopClipboard.SetTextAsync("hello");
var text = await DesktopClipboard.GetTextAsync();
```

### Find and click

```csharp
var editor = DesktopWindows.Find(DesktopWindows.List(), "Editor");
if (editor is not null)
{
    DesktopWindows.Activate(editor.Value.Id);
    DesktopInput.Click(editor.Value.Bounds.X + 8, editor.Value.Bounds.Y + 8);
}
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)