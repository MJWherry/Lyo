using Lyo.Result;

namespace Lyo.Xlsx.Models;

/// <summary>Outcome of an XLSX parse, including path and row count when known.</summary>
public sealed record XlsxParseResult : Result<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>>
{
    /// <summary>Source file path, when the parse read from disk.</summary>
    public string? FilePath { get; init; }

    /// <summary>Number of rows parsed.</summary>
    public int? RowCount { get; init; }

    /// <summary>Human-readable outcome text.</summary>
    public string? Message { get; init; }

    private XlsxParseResult(bool isSuccess, IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful parse result with the row/column map.</summary>
    public static XlsxParseResult FromSuccess(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> parsedData,
        string? filePath = null,
        int? rowCount = null,
        string? message = null)
        => new(true, parsedData) { FilePath = filePath, RowCount = rowCount, Message = message };

    /// <summary>Builds a failed parse result from an exception.</summary>
    public static XlsxParseResult FromException(Exception exception, string? filePath = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }

    /// <summary>Builds a failed parse result with a custom error message.</summary>
    public static XlsxParseResult FromError(string errorMessage, string errorCode, string? filePath = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }
}