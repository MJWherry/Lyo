using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Enrichment.Request;

/// <summary>Request body for Endato Contact Enrichment (<c>POST /Contact/Enrich</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class EnrichmentQuery
{
    /// <summary>First name identifier.</summary>
    public string? FirstName { get; set; }

    /// <summary>Middle name identifier.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Last name identifier.</summary>
    public string? LastName { get; set; }

    /// <summary>Address identifier.</summary>
    public Address? Address { get; set; }

    /// <summary>Date of birth identifier (wire name <c>dob</c>).</summary>
    [JsonPropertyName("Dob")]
    public string? DateOfBirth { get; set; }

    /// <summary>Age identifier.</summary>
    public int? Age { get; set; }

    /// <summary>Phone identifier.</summary>
    public string? Phone { get; set; }

    /// <summary>Email identifier.</summary>
    public string? Email { get; set; }

    public override string ToString()
        => $"EnrichmentQuery: FirstName='{FirstName}', LastName='{LastName}', Dob='{DateOfBirth}', Age={Age}, Phone='{Phone}', Email='{Email}', Address={Address}";
}
