using System.ComponentModel;

namespace Lyo.Job.Models.Enums;

public enum JobState
{
    [Description("Unknown")]
    Unknown = 0, [Description("Queued")]
    Queued = 1, [Description("Running")]
    Running = 2, [Description("Finished")]
    Finished = 3, [Description("Cancelled")]
    Cancelled = 4,

    /// <summary>A cancellation has been requested but the worker has not yet confirmed it stopped.</summary>
    [Description("Cancelling")]
    Cancelling = 5
}