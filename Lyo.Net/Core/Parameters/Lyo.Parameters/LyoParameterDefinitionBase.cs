namespace Lyo.Parameters;

/// <summary>Request-shape base for a parameter declared on a definition: the default from <see cref="LyoParameterValueBase" /> plus the constraints applied at run time.</summary>
public abstract class LyoParameterDefinitionBase : LyoParameterValueBase, ILyoParameterDefinition
{
    /// <inheritdoc />
    public bool Required { get; set; }

    /// <inheritdoc />
    public string? ValidationRegex { get; set; }

    /// <inheritdoc />
    public int? MinLength { get; set; }

    /// <inheritdoc />
    public int? MaxLength { get; set; }

    /// <inheritdoc />
    public string? AllowedValues { get; set; }

    /// <inheritdoc />
    public string? Options { get; set; }

    /// <inheritdoc />
    public LyoParameterDefaultKind DefaultKind { get; set; }

    /// <inheritdoc />
    public string? DefaultTemplate { get; set; }
}
