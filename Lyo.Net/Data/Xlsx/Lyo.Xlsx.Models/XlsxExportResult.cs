using Lyo.Result;

namespace Lyo.Xlsx.Models;

/// <summary>Outcome of an XLSX export, including path, sheet name, and row count when known.</summary>
public sealed record XlsxExportResult : Result<byte[]>
{
    /// <summary>Destination file path, when the export wrote to disk.</summary>
    public string? FilePath { get; init; }

    /// <summary>Worksheet name used, when applicable.</summary>
    public string? WorksheetName { get; init; }

    /// <summary>Number of rows written.</summary>
    public int? RowCount { get; init; }

    /// <summary>Human-readable outcome text.</summary>
    public string? Message { get; init; }

    private XlsxExportResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful export result with the XLSX payload.</summary>
    public static XlsxExportResult FromSuccess(byte[] xlsxData, string? filePath = null, string? worksheetName = null, int? rowCount = null, string? message = null)
        => new(true, xlsxData) {
            FilePath = filePath,
            WorksheetName = worksheetName,
            RowCount = rowCount,
            Message = message
        };

    /// <summary>Builds a failed export result from an exception.</summary>
    public static XlsxExportResult FromException(Exception exception, string? filePath = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }

    /// <summary>Builds a failed export result with a custom error message.</summary>
    public static XlsxExportResult FromError(string errorMessage, string errorCode, string? filePath = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }
}