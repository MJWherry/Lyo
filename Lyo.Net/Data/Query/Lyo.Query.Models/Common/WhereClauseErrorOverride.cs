namespace Lyo.Query.Models.Common;

/// <summary>Optional error code and message for a failed where-clause condition, keyed by dotted field path when calling <see cref="WhereClauseExplainResult.ToErrors" />.</summary>
public sealed class WhereClauseErrorOverride
{
    /// <summary>Replaces the default <see cref="Lyo.Result.Error.Code" /> for this field.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Replaces the default <see cref="Lyo.Result.Error.Message" /> for this field.</summary>
    public string? ErrorMessage { get; set; }
}
