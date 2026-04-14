namespace Lyo.Csv;

/// <summary>Error codes used by CSV services.</summary>
public static class CsvErrorCodes
{
    /// <summary>Failed to export to CSV.</summary>
    public const string ExportFailed = "CSV_EXPORT_FAILED";

    /// <summary>Failed to parse CSV file.</summary>
    public const string ParseFailed = "CSV_PARSE_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "CSV_OPERATION_CANCELLED";

    /// <summary>File operation failed.</summary>
    public const string FileOperationFailed = "CSV_FILE_OPERATION_FAILED";

    /// <summary>CSV validation failed.</summary>
    public const string ValidationFailed = "CSV_VALIDATION_FAILED";
}