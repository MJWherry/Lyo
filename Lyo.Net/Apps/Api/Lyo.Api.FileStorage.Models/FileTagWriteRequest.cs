using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>Body for adding a manual file tag.</summary>
/// <param name="TagType">Dimension, such as <c>client</c>.</param>
/// <param name="Value">Value, such as <c>clienta</c>.</param>
/// <param name="Slug">Optional label.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileTagWriteRequest(string TagType, string Value, string? Slug);