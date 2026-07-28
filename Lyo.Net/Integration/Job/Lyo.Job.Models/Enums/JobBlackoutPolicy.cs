namespace Lyo.Job.Models.Enums;

/// <summary>How scheduled runs that fall inside a calendar blackout window are handled.</summary>
public enum JobBlackoutPolicy
{
    /// <summary>The run is skipped for that slot.</summary>
    Skip = 0,

    /// <summary>The run is deferred until the blackout window ends.</summary>
    Defer = 1
}