namespace Lyo.Csv.Models;

/// <summary>Raised when CSV data is malformed (unclosed quote, inconsistent column count, etc.).</summary>
public sealed class CsvBadDataException(string message)
    : Exception(message);