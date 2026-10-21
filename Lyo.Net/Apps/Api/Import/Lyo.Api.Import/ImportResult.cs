using Lyo.Api.Models.Common.Response;

namespace Lyo.Api.Import;

/// <summary>Typed import outcome. Partial row failures stay successful. Document failures have no rows.</summary>
/// <typeparam name="T">Row type.</typeparam>
public sealed class ImportResult<T>
{
    /// <summary>False only for a document-level failure.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Document-level message.</summary>
    public string? Message { get; init; }

    /// <summary>Rows that bound.</summary>
    public IReadOnlyList<T> Rows { get; init; } = [];

    /// <summary>Rows that did not bind.</summary>
    public IReadOnlyList<ImportIssue> Issues { get; init; } = [];

    /// <summary>Success, including a partial success.</summary>
    public static ImportResult<T> Ok(IReadOnlyList<T> rows, IReadOnlyList<ImportIssue>? issues = null) => new() { IsSuccess = true, Rows = rows, Issues = issues ?? [] };

    /// <summary>Document failure. Rows stay empty.</summary>
    public static ImportResult<T> Fail(string message) => new() { IsSuccess = false, Message = message };
}