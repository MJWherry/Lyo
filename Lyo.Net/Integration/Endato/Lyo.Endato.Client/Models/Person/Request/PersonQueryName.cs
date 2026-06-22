using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Request;

/// <summary>Name parts used in Person Search aka and relative criteria.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonQueryName
{
    /// <summary>Name prefix.</summary>
    public string? Prefix { get; set; }

    /// <summary>First name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Middle name.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Last name.</summary>
    public string? LastName { get; set; }

    /// <summary>Name suffix.</summary>
    public string? Suffix { get; set; }

    public override string ToString()
    {
        var display = string.Join(" ", new[] { Prefix, FirstName, MiddleName, LastName, Suffix }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        return $"PersonQueryName: '{display}'";
    }
}
