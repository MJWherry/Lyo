using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobRunParameterReq
{
    public string Key { get; set; } = null!;

    public string? Description { get; set; }

    public JobParameterType Type { get; set; }

    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public bool Enabled { get; set; }

    public JobRunParameterReq() { }

    public JobRunParameterReq(string key, JobParameterType type, string? value = null, string? description = null)
    {
        Key = key;
        Type = type;
        Value = value;
        Description = description;
    }

    public override string ToString() => $"({Type}) {Key}={Value}, {Description}";
}