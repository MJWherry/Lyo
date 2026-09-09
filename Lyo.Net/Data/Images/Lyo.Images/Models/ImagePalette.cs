using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Color palette taken from an image.</summary>
/// <param name="Colors">Palette colors as hex strings (e.g., "#RRGGBB").</param>
[DebuggerDisplay("{ToString(),nq}")]
public record ImagePalette(IReadOnlyList<string> Colors)
{
    /// <inheritdoc />
    public override string ToString() => $"{Colors.Count} colors";
}