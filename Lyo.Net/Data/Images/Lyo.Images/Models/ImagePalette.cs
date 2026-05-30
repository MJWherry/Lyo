using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Represents a color palette extracted from an image.</summary>
/// <param name="Colors">The palette colors as hex strings (e.g., "#RRGGBB").</param>
[DebuggerDisplay("{ToString(),nq}")]
public record ImagePalette(IReadOnlyList<string> Colors)
{
    /// <inheritdoc />
    public override string ToString() => $"{Colors.Count} colors";
}