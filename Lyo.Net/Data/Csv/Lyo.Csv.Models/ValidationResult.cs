namespace Lyo.Csv.Models;

/// <summary>Outcome of validating a CSV against a <see cref="CsvSchema"/>.</summary>
/// <param name="IsValid">Whether the CSV satisfied the schema.</param>
/// <param name="Errors">Validation errors; null or empty if none.</param>
/// <param name="Warnings">Non-fatal warnings; null or empty if none.</param>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string>? Errors = null, IReadOnlyList<string>? Warnings = null);