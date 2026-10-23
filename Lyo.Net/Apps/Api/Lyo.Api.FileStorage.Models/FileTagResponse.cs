using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>A tag stored on a file.</summary>
/// <param name="Id">Assignment id.</param>
/// <param name="FileId">File id.</param>
/// <param name="TagType">Dimension.</param>
/// <param name="Value">Value for that dimension.</param>
/// <param name="Slug">Optional label. Not a folder name.</param>
/// <param name="Automatic">True when the service wrote the row.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileTagResponse(Guid Id, Guid FileId, string TagType, string Value, string Slug, bool Automatic);