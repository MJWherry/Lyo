using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Represents a color palette extracted from an image.</summary>
/// <param name="Colors">The palette colors as hex strings (e.g., "#RRGGBB").</param>
[DebuggerDisplay("{Colors.Count} colors")]
public record ImagePalette(IReadOnlyList<string> Colors);