namespace Lyo.Csv.Models;

/// <summary>Result of CSV validation.</summary>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string>? Errors = null, IReadOnlyList<string>? Warnings = null);