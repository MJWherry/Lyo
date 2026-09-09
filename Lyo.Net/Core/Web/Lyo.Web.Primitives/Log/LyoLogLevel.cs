namespace Lyo.Web.Primitives;

/// <summary>Severity of a <see cref="LyoLogEntry" />. Matches the usual .NET levels so job, diagnostic, and queue viewers can map without a separate translation table.</summary>
public enum LyoLogLevel
{
    /// <summary>Finest-grained diagnostic detail.</summary>
    Trace = 0,

    /// <summary>Detail intended for development only.</summary>
    Debug = 1,

    /// <summary>Routine progress messages.</summary>
    Information = 2,

    /// <summary>A recoverable problem.</summary>
    Warning = 3,

    /// <summary>An operation that failed.</summary>
    Error = 4,

    /// <summary>A failure that threatens the process.</summary>
    Critical = 5
}
