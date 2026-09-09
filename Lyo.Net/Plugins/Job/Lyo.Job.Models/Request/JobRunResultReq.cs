using System.Diagnostics;
using Lyo.Common.Metadata.Records;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobRunResultReq
{
    public string Key { get; set; } = null!;

    /// <summary>CLR <see cref="Type.FullName" /> of the value (see <see cref="LyoTypeInfo" />).</summary>
    public string Type { get; set; } = "";

    /// <summary>JSON payload matching <see cref="Type" />.</summary>
    public string? Value { get; set; }

    public JobRunResultReq() { }

    public JobRunResultReq(string key, LyoTypeInfo type, object? value = null)
    {
        Key = key;
        Type = type.FullName;
        Value = value is null ? null : type.ToJson(value);
    }

    public JobRunResultReq(string key, string type, string? value)
    {
        Key = key;
        Type = type;
        Value = value;
    }

    public JobRunResultReq(string key, int value)
        : this(key, LyoTypeInfo.Int, value) { }

    public JobRunResultReq(string key, long value)
        : this(key, LyoTypeInfo.Long, value) { }

    public JobRunResultReq(string key, DateTime value)
        : this(key, LyoTypeInfo.DateTime, value) { }

    public JobRunResultReq(string key, Enum value)
        : this(key, LyoTypeInfo.Enum, value) { }

    public override string ToString() => $"({Type}) {Key}={Value}";
}
