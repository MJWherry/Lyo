using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Request;

/// <summary>Name fields used in Person Search aka and relative criteria.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonQueryName
{
    /// <summary>Honorific or prefix.</summary>
    public string? Prefix { get; set; }

    /// <summary>Given name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Second given name.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Family name.</summary>
    public string? LastName { get; set; }

    /// <summary>Generational or professional suffix.</summary>
    public string? Suffix { get; set; }

    public override string ToString()
    {
        var display = string.Join(" ", new[] { Prefix, FirstName, MiddleName, LastName, Suffix }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        return $"PersonQueryName: '{display}'";
    }
}