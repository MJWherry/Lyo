namespace Lyo.People.Models.Enum;

/// <summary>How a <see cref="PersonName" /> should be rendered.</summary>
public enum NameFormat
{
    /// <summary>Given name and family name only.</summary>
    Full,

    /// <summary>Given, middle, and family names.</summary>
    FullWithMiddle,

    /// <summary>Formal name with prefix and suffix.</summary>
    Formal,

    /// <summary>Display name (preferred name when set).</summary>
    Display,

    /// <summary>Family name first.</summary>
    LastNameFirst,

    /// <summary>Initials only.</summary>
    Initials
}
