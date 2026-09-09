namespace Lyo.Xlsx;

/// <summary>Error codes used by the XLSX services.</summary>
public static class XlsxErrorCodes
{
    /// <summary>Export to XLSX failed.</summary>
    public const string ExportFailed = "XLSX_EXPORT_FAILED";

    /// <summary>XLSX parse failed.</summary>
    public const string ParseFailed = "XLSX_PARSE_FAILED";

    /// <summary>Operation was canceled.</summary>
    public const string OperationCancelled = "XLSX_OPERATION_CANCELLED";

    /// <summary>A file operation failed.</summary>
    public const string FileOperationFailed = "XLSX_FILE_OPERATION_FAILED";

    /// <summary>XLSX-to-CSV conversion failed.</summary>
    public const string ConvertToCsvFailed = "XLSX_CONVERT_TO_CSV_FAILED";
}