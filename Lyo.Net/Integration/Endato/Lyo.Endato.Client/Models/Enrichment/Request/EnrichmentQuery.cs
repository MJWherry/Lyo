using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Enrichment.Request;

/// <summary>Request payload for Endato Contact Enrichment (<c>POST /Contact/Enrich</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class EnrichmentQuery
{
    /// <summary>Given-name id.</summary>
    public string? FirstName { get; set; }

    /// <summary>Middle-name id.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Family-name id.</summary>
    public string? LastName { get; set; }

    /// <summary>Address id.</summary>
    public Address? Address { get; set; }

    /// <summary>Date-of-birth id (wire name <c>dob</c>).</summary>
    [JsonPropertyName("Dob")]
    public string? DateOfBirth { get; set; }

    /// <summary>Age id.</summary>
    public int? Age { get; set; }

    /// <summary>Phone id.</summary>
    public string? Phone { get; set; }

    /// <summary>Email id.</summary>
    public string? Email { get; set; }

    public override string ToString()
        => $"EnrichmentQuery: FirstName='{FirstName}', LastName='{LastName}', Dob='{DateOfBirth}', Age={Age}, Phone='{Phone}', Email='{Email}', Address={Address}";
}