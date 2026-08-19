namespace Lyo.Web.Components.DataGrid;

/// <summary>How <see cref="LyoTimestamp" /> renders a UTC instant in the browser time zone.</summary>
public enum LyoTimestampKind
{
    /// <summary>Local date and time (default <c>g</c> format) plus the zone label.</summary>
    Absolute = 0,

    /// <summary>Relative hours/minutes when the instant is within <see cref="LyoTimestamp.RelativeWindow" /> of now (past or future); otherwise absolute. Relative text has no zone abbrev.</summary>
    Relative = 1,

    /// <summary>Relative for a future instant within the window (<c>in 2h 15m</c>). Otherwise absolute.</summary>
    TimeUntil = 2,

    /// <summary>Relative for a past instant within the window (<c>3h ago</c>). Otherwise absolute.</summary>
    TimeSince = 3
}
