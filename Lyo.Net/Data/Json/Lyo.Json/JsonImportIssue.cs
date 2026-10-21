namespace Lyo.Json;

/// <summary>One JSON element or line that could not be bound. The element is omitted from the good list.</summary>
public sealed class JsonImportIssue
{
    /// <summary>Zero-based index in the array, or zero-based line index for NDJSON.</summary>
    public int Index { get; init; }

    /// <summary>Property path that failed, when the element was an object with a bad property.</summary>
    public string? Field { get; init; }

    /// <summary>Why the element failed.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Source text of the element or line.</summary>
    public string? Raw { get; init; }
}