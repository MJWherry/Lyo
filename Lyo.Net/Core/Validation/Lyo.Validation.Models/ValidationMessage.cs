namespace Lyo.Validation.Models;

/// <summary>Optional error code and message override for a schema constraint, keyed by dotted field path in <see cref="ValidationSchema.Messages" />.</summary>
public sealed class ValidationMessage
{
    /// <summary>Overrides the default <see cref="Lyo.Result.Error.Code" /> for this field.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Overrides the default <see cref="Lyo.Result.Error.Message" /> for this field.</summary>
    public string? ErrorMessage { get; set; }
}
