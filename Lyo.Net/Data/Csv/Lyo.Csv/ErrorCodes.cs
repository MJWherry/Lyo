namespace Lyo.Csv;

/// <summary>Error codes used by the CSV services.</summary>
public static class CsvErrorCodes
{
    /// <summary>Export to CSV failed.</summary>
    public const string ExportFailed = "CSV_EXPORT_FAILED";

    /// <summary>CSV parse failed.</summary>
    public const string ParseFailed = "CSV_PARSE_FAILED";

    /// <summary>Operation was canceled.</summary>
    public const string OperationCancelled = "CSV_OPERATION_CANCELLED";

    /// <summary>A file operation failed.</summary>
    public const string FileOperationFailed = "CSV_FILE_OPERATION_FAILED";

    /// <summary>CSV schema validation failed.</summary>
    public const string ValidationFailed = "CSV_VALIDATION_FAILED";
}