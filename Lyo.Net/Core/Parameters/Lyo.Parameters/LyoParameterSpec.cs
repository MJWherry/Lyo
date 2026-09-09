using System.Diagnostics;

namespace Lyo.Parameters;

/// <summary>
/// Declared shape of one parameter, detached from whichever DTO or entity it was read from so <see cref="LyoParameterValidator" /> can serve Job, Reporting, and anything else
/// that grows parameters later.
/// </summary>
/// <param name="Key">Parameter name, matched case-insensitively against supplied values.</param>
/// <param name="Type">CLR <see cref="System.Type.FullName" /> the supplied value must parse as.</param>
/// <param name="Required">True when a value must be supplied. Ciphertext satisfies this.</param>
/// <param name="ValidationRegex">Regex the supplied value must match. Null or empty disables the check.</param>
/// <param name="MinLength">Minimum length of the supplied value. Null disables the check.</param>
/// <param name="MaxLength">Maximum length of the supplied value. Null disables the check.</param>
/// <param name="AllowedValues">JSON array of permitted values. Null or empty allows anything.</param>
/// <param name="DefaultKind">Where the default comes from. Expression defaults are rendered before the value is checked against <paramref name="Type" />.</param>
/// <param name="DefaultTemplate">Template rendered for an expression default. Null for literal defaults.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoParameterSpec(
    string Key,
    string Type,
    bool Required = false,
    string? ValidationRegex = null,
    int? MinLength = null,
    int? MaxLength = null,
    string? AllowedValues = null,
    LyoParameterDefaultKind DefaultKind = LyoParameterDefaultKind.Literal,
    string? DefaultTemplate = null)
{
    /// <summary>Builds a declared parameter from any source that exposes the shared definition contract.</summary>
    /// <param name="definition">Parameter definition, typically a <c>*ParameterRes</c> or <c>*ParameterReq</c>.</param>
    public static LyoParameterSpec From(ILyoParameterDefinition definition)
        => new(
            definition.Key, definition.Type, definition.Required, definition.ValidationRegex, definition.MinLength, definition.MaxLength, definition.AllowedValues,
            definition.DefaultKind, definition.DefaultTemplate);

    /// <inheritdoc />
    public override string ToString() => $"{Key} ({Type}){(Required ? " required" : "")}";
}

/// <summary>One supplied value to validate against a <see cref="LyoParameterSpec" />.</summary>
/// <param name="Key">Parameter name the value was supplied under.</param>
/// <param name="Value">Serialized value, or null when only ciphertext was supplied.</param>
/// <param name="HasEncryptedValue">True when the caller supplied ciphertext, which satisfies <see cref="LyoParameterSpec.Required" /> on its own.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoParameterValueSpec(string Key, string? Value, bool HasEncryptedValue = false)
{
    /// <summary>Builds a supplied value from any source that exposes the shared value contract.</summary>
    /// <param name="value">Supplied parameter, typically a <c>*ParameterReq</c>.</param>
    public static LyoParameterValueSpec From(ILyoParameterValue value) => new(value.Key, value.Value, value.EncryptedValue is not null);

    /// <inheritdoc />
    public override string ToString() => $"{Key}={(HasEncryptedValue ? "(encrypted)" : Value)}";
}
