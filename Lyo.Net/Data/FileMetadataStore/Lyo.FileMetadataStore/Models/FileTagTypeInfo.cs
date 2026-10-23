using System.Diagnostics;

namespace Lyo.FileMetadataStore.Models;

/// <summary>A tag type the catalog can group and sort by.</summary>
/// <param name="TagType">Dimension name.</param>
/// <param name="Automatic">True for service-owned types.</param>
/// <param name="ValueKind">How this type's stored text is parsed unless a grouping row chooses another kind.</param>
/// <param name="Sort">Folder order for this type's default kind.</param>
/// <param name="Grains">Grains a caller may request for the default kind. <see cref="FileTagGrain.Value" /> is omitted when the raw value is not a folder name.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileTagTypeInfo(string TagType, bool Automatic, FileTagValueKind ValueKind, FileTagSort Sort, IReadOnlyList<FileTagGrain> Grains);