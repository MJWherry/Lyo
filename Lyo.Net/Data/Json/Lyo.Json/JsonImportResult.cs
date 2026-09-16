namespace Lyo.Json;

/// <summary>
/// Partial JSON import. <see cref="IsSuccess" /> stays true when individual elements fail. A null document, an empty document, or a root object without
/// <c>rowsPath</c> fails the document and returns no rows.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public sealed class JsonImportResult<T>
{
    /// <summary>False only for a document-level failure.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Document-level message. Null on success.</summary>
    public string? Message { get; init; }

    /// <summary>Elements that bound.</summary>
    public IReadOnlyList<T> Rows { get; init; } = [];

    /// <summary>Elements that did not bind.</summary>
    public IReadOnlyList<JsonImportIssue> Issues { get; init; } = [];

    /// <summary>Success, including a partial success that still has <paramref name="issues" />.</summary>
    public static JsonImportResult<T> Success(IReadOnlyList<T> rows, IReadOnlyList<JsonImportIssue>? issues = null)
        => new() { IsSuccess = true, Rows = rows, Issues = issues ?? [] };

    /// <summary>Document failure. Rows stay empty.</summary>
    public static JsonImportResult<T> DocumentError(string message)
        => new() { IsSuccess = false, Message = message };
}
