using System.ComponentModel;

namespace Lyo.Job.Models.Enums;

public enum JobRunResult
{
    [Description("Unknown")]
    Unknown = 0, [Description("Success")]
    Success = 1, [Description("Success with warnings")]
    SuccessWithWarnings = 2, [Description("Partial Success")]
    PartialSuccess = 3, [Description("Failure")]
    Failure = 4, [Description("Cancelled")]
    Cancelled = 5, [Description("Skipped")]
    Skipped = 6, [Description("Timed out")]
    Timeout = 7
}