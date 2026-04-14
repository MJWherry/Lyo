using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobRunLogRes(Guid Id, Guid JobRunId, JobLogLevel Level, string Message, string? Context, string? StackTrace, DateTime Timestamp) { }