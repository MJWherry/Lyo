using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobRunLogReq
{
    public JobLogLevel Level { get; set; }

    public string Message { get; set; } = null!;

    public string? Context { get; set; }

    public string? StackTrace { get; set; }

    public DateTime Timestamp { get; set; }

    public JobRunLogReq() { }

    public JobRunLogReq(JobLogLevel level, string message, string? context = null, string? stackTrace = null, DateTime? timestamp = null)
    {
        Level = level;
        Message = message;
        Context = context;
        StackTrace = stackTrace;
        Timestamp = timestamp ?? DateTime.UtcNow;
    }

    public override string ToString() => $"{Timestamp:g} {Level.ToString()}: {Message}{(StackTrace is null ? null : Environment.NewLine + StackTrace)}";
}