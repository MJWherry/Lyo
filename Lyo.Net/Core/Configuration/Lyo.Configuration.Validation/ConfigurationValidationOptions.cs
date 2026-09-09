namespace Lyo.Configuration.Validation;

/// <summary>Settings that steer <see cref="ConfigurationClauseEvaluator" /> and <see cref="ConfigurationValidator" />.</summary>
public sealed class ConfigurationValidationOptions
{
    /// <summary>Configuration section these options bind from.</summary>
    public const string SectionName = "ConfigurationValidation";

    /// <summary>When true (the default), a <c>.</c> in a rule field path is rewritten to the configuration key delimiter <c>:</c>.</summary>
    /// <remarks>
    /// Where clauses spell nested paths with dots because they were designed for entity property paths, while configuration nests with colons. Leaving this on lets one schema
    /// read naturally in both worlds. Turn it off only when a configuration key legitimately contains a dot.
    /// </remarks>
    public bool TreatDotAsKeyDelimiter { get; set; } = true;

    /// <summary>When true (the default), a schema key missing from the store fails validation.</summary>
    /// <remarks>Set to <c>false</c> for hosts that fetch schemas from an API and should still start when that API has not published rules yet.</remarks>
    public bool FailWhenSchemaMissing { get; set; } = true;

    /// <summary>Section validated when a call does not name one. Null or empty means the configuration root.</summary>
    public string? DefaultSectionName { get; set; }

    /// <summary>Throws when the options contradict themselves.</summary>
    public void Validate()
    {
        if (DefaultSectionName is { Length: > 0 } section && section.Trim().Length == 0)
            throw new ConfigurationValidationException($"{nameof(DefaultSectionName)} must be null, empty, or a non-whitespace section name.");
    }
}
