# Lyo.Avalonia.Components

Desktop apps share this library the way Blazor hosts share `Lyo.Web.Components`. `ToolDockBuilder` lays named panes around a center control on the left, right, or bottom. `ToolDockHit` picks the drop edge while a title bar is dragged. `ScreenEdgeDock` parks a window on the nearest screen edge. `EdgeDockBar` is the tab on that edge: the app sets its size, icon, and buttons. `EdgeDockWindow` hides the window contents, places the bubble, and puts the window back when the drag leaves the edge. Click, double-click, and the buttons stay with the app. `AvaloniaDesktop.Configure` starts a desktop app with platform detection and the Inter font. `Tabber` is the shared tab strip: each page is parented once, and selecting a tab only shows that page.

## Features

- **Tool windows.** `ToolDockState` records each pane's edge, visibility, and whether it is minimized. `ToolDockBuilder.Fill` stacks left and right panes vertically and bottom panes sideways, with splitters and a header the user can drag. Minimize collapses that pane to its header and shrinks the edge when every pane there is minimized. Close hides the pane; the host rebuilds from the changed callback.
- **Screen edge.** `ScreenEdgeDock.Place` chooses the nearest edge from the window center and clamps the tab inside the work area. `Expand` grows the window inward. `Clamp` pulls an open tab back onto the screen without leaving its edge. `IconBias` shifts the icon toward a corner so turning onto the next edge does not throw it outward. `PulledOff` says when a drag has left the edge.
- **Edge tab.** `EdgeDockBar` draws the icon and any number of buttons. `Thickness`, `Length`, and `Corner` set the shape. Hover opens the buttons, and leaving closes them. Click, double-click, and drag are events the app handles. `EdgeDockWindow` owns the docked flag, the rest position, and the settle loop that puts the title bar back. `EdgeDockRest` is the open size: a fixed height, or height from the content.
- **Prompts.** `PromptDialog.AskAsync`, `ConfirmAsync`, and `ShowTextAsync` are owned dialogs with the shared dark chrome. `Labeled`, `Stack`, and `Buttons` are the pieces a domain dialog reuses.
- **Name and value.** `NameValueList` draws labeled rows. Play context, a captured request, and an element inspection share it.
- **JSON.** `JsonEditor` shows a JSON document as a virtualized tree or as raw text. The tree searches, expands, collapses, colors values, and copies one node. A body that is not JSON stays as text.
- **Tabs.** `Tabber` keeps each page parented once and only changes which page is visible. Headers can close, rename, and disable.
- **Log.** `LogView` is a virtualized log fed by `LogSink`. Each row has a timestamp, a level chip, and the message. The toolbar filters by level and text, follows the tail, and copies or exports the visible lines.

## Examples

### Start a desktop app

```csharp
public static AppBuilder BuildAvaloniaApp() => AvaloniaDesktop.Configure<App>();
```

### Dock a bubble to the nearest edge

```csharp
var anchor = ScreenEdgeDock.Place(0, 0, 1920, 1080, 1700, 340, 200, 400, ScreenEdgeDock.Thickness, ScreenEdgeDock.Diameter);
var corners = ScreenEdgeDock.Corners(anchor.Edge);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Avalonia` `12.1.3` (direct, third-party)
- `Avalonia.Desktop` `12.1.3` (direct, third-party)
- `Avalonia.Fonts.Inter` `12.1.3` (direct, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)