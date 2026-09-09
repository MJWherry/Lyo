using System.Diagnostics;

namespace Lyo.Parameters;

/// <summary>
/// Request-shape base for a supplied parameter value. Feature packages derive from this and add only their parent foreign key, so the shared shape lives in one place while each
/// DTO keeps its own name and serialization contract.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class LyoParameterValueBase : ILyoParameterValue
{
    /// <inheritdoc />
    public string Key { get; set; } = null!;

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public string Type { get; set; } = "";

    /// <inheritdoc />
    public string? Value { get; set; }

    /// <inheritdoc />
    public byte[]? EncryptedValue { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"({Type}) {Key}={Value}, {Description}";
}
