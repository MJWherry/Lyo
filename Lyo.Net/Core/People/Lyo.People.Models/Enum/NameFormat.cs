namespace Lyo.People.Models;

/// <summary>Name formatting options</summary>
public enum NameFormat
{
    /// <summary>First and last name only</summary>
    Full,

    /// <summary>First, middle, and last name</summary>
    FullWithMiddle,

    /// <summary>Formal name with prefix and suffix</summary>
    Formal,

    /// <summary>Display name (preferred name if available)</summary>
    Display,

    /// <summary>Last name first format</summary>
    LastNameFirst,

    /// <summary>Initials only</summary>
    Initials
}