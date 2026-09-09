namespace Lyo.Validation.Models;

/// <summary>Optional error code and message for a failed where-clause condition, keyed by dotted field path when <see cref="WhereClauseExplainResultExtensions.ToErrors" /> runs.</summary>
public sealed class WhereClauseErrorOverride
{
    /// <summary>Overrides the default <see cref="Lyo.Result.Error.Code" /> for this field.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Overrides the default <see cref="Lyo.Result.Error.Message" /> for this field.</summary>
    public string? ErrorMessage { get; set; }
}
