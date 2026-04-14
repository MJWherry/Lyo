using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobRunResultReq
{
    public string Key { get; set; } = null!;

    public JobParameterType Type { get; set; }

    public string? Value { get; set; }

    public JobRunResultReq() { }

    public JobRunResultReq(string key, JobParameterType type, object? value = null)
    {
        Key = key;
        Type = type;
        Value = value?.ToString();
    }

    public JobRunResultReq(string key, int value)
        : this(key, JobParameterType.Int, value) { }

    public JobRunResultReq(string key, long value)
        : this(key, JobParameterType.Long, value) { }

    public JobRunResultReq(string key, DateTime value, string? format = null)
        : this(key, JobParameterType.DateTime, string.IsNullOrEmpty(format) ? value.ToString() : value.ToString(format)) { }

    public JobRunResultReq(string key, Enum value)
        : this(key, JobParameterType.String, value.ToString()) { }

    public JobRunResultReq(string key, string value, JobParameterType type = JobParameterType.String)
        : this(key, type, value) { }

    public override string ToString() => $"({Type}) {Key}={Value}";
}