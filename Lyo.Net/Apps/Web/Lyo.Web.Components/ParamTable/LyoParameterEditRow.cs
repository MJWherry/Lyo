using Lyo.Common.Metadata.Records;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Editable parameter row for <see cref="LyoParameterEditor" />. Job and report hosts map their API DTOs into this type on load/save.
/// </summary>
public sealed class LyoParameterEditRow
{
    /// <summary>Existing row id; null until the host persists a new row.</summary>
    public Guid? Id { get; set; }

    /// <summary>Parameter key unique within the owning definition or schedule.</summary>
    public string Key { get; set; } = "";

    /// <summary>CLR FullName (or package-owned catalog name) for the JSON <see cref="Value" />.</summary>
    public string Type { get; set; } = LyoTypeInfo.String.FullName;

    /// <summary>JSON payload. API mask <c>***</c> is stripped by <see cref="FromMasked" />.</summary>
    public string? Value { get; set; }

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>If true, run/generate requires a value.</summary>
    public bool Required { get; set; }

    /// <summary>If true, the schedule override is active. Unused on report rows (default true).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>True until the host has POSTed this row.</summary>
    public bool IsNew { get; set; }

    /// <summary>True when the stored value is encrypted server-side; the API masks plaintext as <c>***</c>.</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>Ciphertext when the client still has it; empty marker means encrypt-at-rest without a default.</summary>
    public byte[]? EncryptedValue { get; set; }

    /// <summary>JSON array of allowed values for server validation / simple select.</summary>
    public string? AllowedValues { get; set; }

    /// <summary>JSON picker source (static items or root QueryReq).</summary>
    public string? Options { get; set; }

    /// <summary>Display order among parameters on this definition. Lower values appear first. Unused on report rows.</summary>
    public int Order { get; set; }

    /// <summary>Where the default comes from. <see cref="LyoParameterDefaultKind.Expression" /> authors it as a template instead of a typed literal.</summary>
    public LyoParameterDefaultKind DefaultKind { get; set; }

    /// <summary>Template producing the default when <see cref="DefaultKind" /> is <see cref="LyoParameterDefaultKind.Expression" />.</summary>
    public string? DefaultTemplate { get; set; }

    /// <summary>Whether this row authors its default as a template. Drives the Default column's editor.</summary>
    public bool UsesExpressionDefault => DefaultKind == LyoParameterDefaultKind.Expression;

    /// <summary>JSON to persist: the API mask is never sent back as a value.</summary>
    public string? ValueForSave => string.Equals(Value, "***", StringComparison.Ordinal) ? null : Value;

    /// <summary>Template to persist, cleared for literal defaults so a half-set default channel never reaches the API.</summary>
    public string? DefaultTemplateForSave => UsesExpressionDefault ? DefaultTemplate : null;

    /// <summary>Ciphertext marker when <see cref="IsEncrypted" /> is on; otherwise null so the host clears encryption.</summary>
    public byte[]? EncryptedValueForSave => IsEncrypted ? EncryptedValue ?? [] : null;

    /// <summary>Picker source in effect: explicit <see cref="Options" /> when set, else a static list synthesized from <see cref="AllowedValues" />.</summary>
    public string? EffectiveOptions => string.IsNullOrWhiteSpace(Options) ? ParameterOptionsJson.FromAllowedValues(AllowedValues) : Options;

    /// <summary>Picker kind in effect, or null when this parameter has no value picker and only takes a typed default.</summary>
    public ParameterOptionsKind? EffectiveKind => ParameterOptionsJson.TryGetKind(EffectiveOptions);

    /// <summary>
    /// Builds a row from an API response, treating <c>***</c> as a masked encrypted value instead of plaintext.
    /// </summary>
    public static LyoParameterEditRow FromMasked(
        Guid id,
        string key,
        string type,
        string? value,
        string? description = null,
        bool required = false,
        byte[]? encryptedValue = null,
        string? allowedValues = null,
        string? options = null,
        int order = 0,
        bool enabled = true,
        LyoParameterDefaultKind defaultKind = LyoParameterDefaultKind.Literal,
        string? defaultTemplate = null)
    {
        var masked = string.Equals(value, "***", StringComparison.Ordinal);
        return new() {
            Id = id,
            Key = key,
            Type = type,
            Value = masked ? null : value,
            Description = description,
            Required = required,
            Enabled = enabled,
            IsEncrypted = masked || encryptedValue is { Length: > 0 },
            EncryptedValue = encryptedValue,
            AllowedValues = allowedValues,
            Options = options,
            Order = order,
            DefaultKind = defaultKind,
            DefaultTemplate = defaultTemplate
        };
    }

    /// <summary>Applies the POST create response so the row is no longer treated as new.</summary>
    public void ApplyCreated(Guid id, string? options, string? allowedValues, string? value)
    {
        Id = id;
        IsNew = false;
        Options = options;
        AllowedValues = allowedValues;
        Value = string.Equals(value, "***", StringComparison.Ordinal) ? null : value;
    }
}
