namespace Lyo.Csv.Models;

/// <summary>Thrown when CSV data is malformed (unclosed quote, inconsistent column count, etc.).</summary>
public sealed class CsvBadDataException(string message) : Exception(message);
