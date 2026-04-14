namespace Lyo.Xlsx;

/// <summary>Error codes used by XLSX services.</summary>
public static class XlsxErrorCodes
{
    /// <summary>Failed to export to XLSX.</summary>
    public const string ExportFailed = "XLSX_EXPORT_FAILED";

    /// <summary>Failed to parse XLSX file.</summary>
    public const string ParseFailed = "XLSX_PARSE_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "XLSX_OPERATION_CANCELLED";

    /// <summary>File operation failed.</summary>
    public const string FileOperationFailed = "XLSX_FILE_OPERATION_FAILED";

    /// <summary>Failed to convert XLSX to CSV.</summary>
    public const string ConvertToCsvFailed = "XLSX_CONVERT_TO_CSV_FAILED";
}