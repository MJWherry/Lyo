using Lyo.Common;

namespace Lyo.Csv.Models;

/// <summary>Result of a CSV parse operation with CSV-specific properties.</summary>
/// <typeparam name="T">The type of objects parsed from CSV.</typeparam>
public sealed record CsvParseResult<T> : Result<IReadOnlyList<T>>
{
    /// <summary>The file path that was parsed (if applicable).</summary>
    public string? FilePath { get; init; }

    /// <summary>The number of rows parsed.</summary>
    public int? RowCount { get; init; }

    /// <summary>The parse errors encountered (if any).</summary>
    public IReadOnlyList<CsvParseError>? ParseErrors { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private CsvParseResult(bool isSuccess, List<T>? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful CsvParseResult with parsed data.</summary>
    public static CsvParseResult<T> FromSuccess(
        List<T> parsedData,
        string? filePath = null,
        int? rowCount = null,
        IReadOnlyList<CsvParseError>? parseErrors = null,
        string? message = null)
        => new(true, parsedData) {
            FilePath = filePath,
            RowCount = rowCount,
            ParseErrors = parseErrors,
            Message = message
        };

    /// <summary>Creates a failed CsvParseResult from an exception.</summary>
    public static CsvParseResult<T> FromException(Exception exception, string? filePath = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }

    /// <summary>Creates a failed CsvParseResult with a custom error message.</summary>
    public static CsvParseResult<T> FromError(string errorMessage, string errorCode, string? filePath = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }
}