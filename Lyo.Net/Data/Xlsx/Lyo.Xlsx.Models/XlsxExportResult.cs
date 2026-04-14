using Lyo.Common;

namespace Lyo.Xlsx.Models;

/// <summary>Result of an XLSX export operation with XLSX-specific properties.</summary>
public sealed record XlsxExportResult : Result<byte[]>
{
    /// <summary>The file path where the XLSX was exported (if applicable).</summary>
    public string? FilePath { get; init; }

    /// <summary>The worksheet name used (if applicable).</summary>
    public string? WorksheetName { get; init; }

    /// <summary>The number of rows exported.</summary>
    public int? RowCount { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private XlsxExportResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful XlsxExportResult with exported data.</summary>
    public static XlsxExportResult FromSuccess(byte[] xlsxData, string? filePath = null, string? worksheetName = null, int? rowCount = null, string? message = null)
        => new(true, xlsxData) {
            FilePath = filePath,
            WorksheetName = worksheetName,
            RowCount = rowCount,
            Message = message
        };

    /// <summary>Creates a failed XlsxExportResult from an exception.</summary>
    public static XlsxExportResult FromException(Exception exception, string? filePath = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }

    /// <summary>Creates a failed XlsxExportResult with a custom error message.</summary>
    public static XlsxExportResult FromError(string errorMessage, string errorCode, string? filePath = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, new List<Error> { error }) { FilePath = filePath };
    }
}