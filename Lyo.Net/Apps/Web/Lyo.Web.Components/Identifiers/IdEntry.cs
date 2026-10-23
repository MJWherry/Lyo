using System.Diagnostics;

namespace Lyo.Web.Components.Identifiers;

/// <summary>A generated identifier value with an optional embedded timestamp label shown to the user.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record IdEntry(string Value, string? Timestamp = null);