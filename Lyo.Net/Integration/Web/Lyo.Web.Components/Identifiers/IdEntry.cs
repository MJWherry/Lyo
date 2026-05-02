namespace Lyo.Web.Components.Identifiers;

/// <summary>A generated identifier value with an optional embedded timestamp label for display.</summary>
public sealed record IdEntry(string Value, string? Timestamp = null);