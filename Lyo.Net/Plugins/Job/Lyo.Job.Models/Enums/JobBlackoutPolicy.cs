namespace Lyo.Job.Models.Enums;

/// <summary>How scheduled runs that land inside a calendar blackout window are treated.</summary>
public enum JobBlackoutPolicy
{
    /// <summary>That slot's run is skipped.</summary>
    Skip = 0,

    /// <summary>The run is held until the blackout window ends.</summary>
    Defer = 1
}