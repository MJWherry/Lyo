namespace Lyo.IO.Temp.Enums;

/// <summary>Selects how middle segments of auto-generated file and directory names are produced.</summary>
public enum TempNamingStrategy
{
    /// <summary>Monotonically increasing numbers for the process (thread-safe).</summary>
    Sequential,

    /// <summary>UTC Unix milliseconds at generation time.</summary>
    Timestamp,

    /// <summary>32-character lowercase hex <see cref="Guid" /> without braces.</summary>
    Guid,

    /// <summary>Random alphanumeric segment similar to <see cref="Path.GetRandomFileName" /> without the dot.</summary>
    RandomChars
}