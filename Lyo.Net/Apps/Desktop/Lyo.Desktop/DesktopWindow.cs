using System.Diagnostics;

namespace Lyo.Desktop;

/// <summary>A rectangle on the desktop, in pixels.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DesktopRect(int X, int Y, int Width, int Height);

/// <summary>A top-level window. <see cref="Id" /> is the platform handle.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DesktopWindow(long Id, string Title, DesktopRect Bounds);