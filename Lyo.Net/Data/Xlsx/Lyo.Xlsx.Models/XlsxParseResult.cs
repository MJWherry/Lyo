using Lyo.Common;

namespace Lyo.Xlsx.Models;

/// <summary>Result of an XLSX parse operation with XLSX-specific properties.</summary>
public sealed record XlsxParseResult : Result<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>>
{
    /// <summary>The file path that was parsed (if applicable).</summary>
    public string? FilePath { get; init; }

    /// <summary>The number of rows parsed.</summary>
    public int? RowCount { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private XlsxParseResult(bool isSuccess, IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful XlsxParseResult with parsed data.</summary>
    public static XlsxParseResult FromSuccess(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> parsedData,
        string? filePath = null,
        int? rowCount = null,
        string? message = null)
        => new(true, parsedData) { FilePath = filePath, RowCount = rowCount, Message = message };

    /// <summary>Creates a failed XlsxParseResult from an exception.</summary>
    public static XlsxParseResult FromException(Exception exception, string? filePath = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }

    /// <summary>Creates a failed XlsxParseResult with a custom error message.</summary>
    public static XlsxParseResult FromError(string errorMessage, string errorCode, string? filePath = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }
}