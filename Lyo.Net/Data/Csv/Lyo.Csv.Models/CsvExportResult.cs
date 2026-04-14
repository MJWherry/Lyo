using Lyo.Common;

namespace Lyo.Csv.Models;

/// <summary>Result of a CSV export operation with CSV-specific properties.</summary>
public sealed record CsvExportResult : Result<byte[]>
{
    /// <summary>The file path where the CSV was exported (if applicable).</summary>
    public string? FilePath { get; init; }

    /// <summary>The number of rows exported.</summary>
    public int? RowCount { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private CsvExportResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful CsvExportResult with exported data.</summary>
    public static CsvExportResult FromSuccess(byte[] csvData, string? filePath = null, int? rowCount = null, string? message = null)
        => new(true, csvData) { FilePath = filePath, RowCount = rowCount, Message = message };

    /// <summary>Creates a failed CsvExportResult from an exception.</summary>
    public static CsvExportResult FromException(Exception exception, string? filePath = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }

    /// <summary>Creates a failed CsvExportResult with a custom error message.</summary>
    public static CsvExportResult FromError(string errorMessage, string errorCode, string? filePath = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }
}