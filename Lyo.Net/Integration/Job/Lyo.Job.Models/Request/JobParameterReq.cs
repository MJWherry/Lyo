using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobParameterReq
{
    public Guid JobDefinitionId { get; set; }

    public string Key { get; set; } = null!;

    public string? Description { get; set; }

    public JobParameterType Type { get; set; }

    public string? Value { get; set; } //todo change from string to object?

    public bool Required { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public bool AllowMultiple { get; set; }

    public bool Enabled { get; set; }

    /// <summary>Optional regex pattern the run value must match.</summary>
    public string? ValidationRegex { get; set; }

    /// <summary>Minimum string length. Null = no minimum.</summary>
    public int? MinLength { get; set; }

    /// <summary>Maximum string length. Null = no maximum.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Pipe-separated list of allowed values (e.g. <c>A|B|C</c>). Null = any value allowed.</summary>
    public string? AllowedValues { get; set; }

    public override string ToString() => $"({Type}) {Key}={Value}, {Description}";
}