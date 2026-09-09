namespace Lyo.Parameters;

/// <summary>
/// A parameter declared on a definition: the default plus the constraints every supplied value is checked against. <see cref="ILyoKeyedValue.Value" /> holds the default
/// rather than a supplied value.
/// </summary>
public interface ILyoParameterDefinition : ILyoParameterValue
{
    /// <summary>True when a value must be supplied. Ciphertext in <see cref="ILyoParameterValue.EncryptedValue" /> satisfies this.</summary>
    bool Required { get; }

    /// <summary>Regex the supplied value must match in full-string terms. Null or empty turns the check off.</summary>
    string? ValidationRegex { get; }

    /// <summary>Minimum length of the supplied value. Null turns the check off.</summary>
    int? MinLength { get; }

    /// <summary>Maximum length of the supplied value. Null turns the check off.</summary>
    int? MaxLength { get; }

    /// <summary>JSON array of permitted values. Null or empty allows any value.</summary>
    string? AllowedValues { get; }

    /// <summary>JSON picker source (static items or a root query). Null means no picker; the scalar default stands alone.</summary>
    string? Options { get; }

    /// <summary>Where the default comes from. <see cref="LyoParameterDefaultKind.Literal" /> reads <see cref="ILyoKeyedValue.Value" />, the historical behaviour.</summary>
    LyoParameterDefaultKind DefaultKind { get; }

    /// <summary>
    /// Template rendered to produce the default when <see cref="DefaultKind" /> is <see cref="LyoParameterDefaultKind.Expression" />, for example
    /// <c>{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}</c>. Null when the default is literal.
    /// </summary>
    string? DefaultTemplate { get; }
}
