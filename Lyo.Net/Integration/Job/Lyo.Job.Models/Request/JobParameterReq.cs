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

    public override string ToString() => $"({Type}) {Key}={Value}, {Description}";
}