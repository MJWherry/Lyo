namespace Lyo.IO.Temp.Enums;

/// <summary>How the variable middle of generated file and directory names is chosen.</summary>
public enum TempNamingStrategy
{
    /// <summary>Increasing integers for this process; safe across threads.</summary>
    Sequential,

    /// <summary>Unix milliseconds in UTC at the moment of generation.</summary>
    Timestamp,

    /// <summary>32 lowercase hex digits from a <see cref="Guid" />, no braces.</summary>
    Guid,

    /// <summary>Random alphanumeric chunk, like <see cref="Path.GetRandomFileName" /> without the dot.</summary>
    RandomChars
}