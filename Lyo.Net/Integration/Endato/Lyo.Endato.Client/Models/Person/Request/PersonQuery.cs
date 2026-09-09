using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Person.Request;

/// <summary>Request payload for Endato Person Search (<c>POST /PersonSearch</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonQuery
{
    /// <summary>Filter on given name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Filter on middle name.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Filter on family name.</summary>
    public string? LastName { get; set; }

    /// <summary>Date-of-birth filter (wire name <c>dob</c>).</summary>
    [JsonPropertyName("Dob")]
    public string? DateOfBirth { get; set; }

    /// <summary>Filter on age.</summary>
    public int? Age { get; set; }

    /// <summary>Lower age bound for a range.</summary>
    public int? AgeRangeMinAge { get; set; }

    /// <summary>Upper age bound for a range.</summary>
    public int? AgeRangeMaxAge { get; set; }

    /// <summary>Age-range expression.</summary>
    public string? AgeRange { get; set; }

    /// <summary>AKA name rows.</summary>
    public IReadOnlyList<PersonQueryName>? Akas { get; set; }

    /// <summary>Relative-name rows.</summary>
    public IReadOnlyList<PersonQueryName>? Relatives { get; set; }

    /// <summary>Address rows treated as search criteria.</summary>
    public IReadOnlyList<PersonQueryAddress>? Addresses { get; set; }

    /// <summary>Filter on email.</summary>
    public string? Email { get; set; }

    /// <summary>Filter on phone.</summary>
    public string? Phone { get; set; }

    /// <summary>Client IP sent to Endato when required by the account.</summary>
    public string? ClientIp { get; set; }

    /// <summary>Tahoe ids from Endato.</summary>
    public IReadOnlyList<string>? TahoeIds { get; set; }

    /// <summary>Fuzzy matching first-name character offset.</summary>
    public int? FirstNameCharOffset { get; set; }

    /// <summary>Fuzzy matching last-name character offset.</summary>
    public int? LastNameCharOffset { get; set; }

    /// <summary>Expected DOB format for <see cref="DateOfBirth" />.</summary>
    public string? DobFormat { get; set; }

    /// <summary>Returned addresses maximum age in years.</summary>
    public int? MaxAddressYears { get; set; }

    /// <summary>Returned phone numbers maximum age in years.</summary>
    public int? MaxPhoneYears { get; set; }

    /// <summary>1-based page index.</summary>
    public int? Page { get; set; }

    /// <summary>Page size of the result set.</summary>
    public int ResultsPerPage { get; set; } = 2;

    /// <summary>Includes expanded on each person.</summary>
    public IReadOnlyList<string>? Includes { get; set; }

    /// <summary>Search filter options.</summary>
    public IReadOnlyList<string>? FilterOptions { get; set; }

    /// <summary>Builds an empty Person Search request.</summary>
    public PersonQuery() { }

    /// <summary>Builds a Person Search request by name and date of birth.</summary>
    /// <param name="firstName">Filter on given name.</param>
    /// <param name="lastName">Filter on family name.</param>
    /// <param name="dateOfBirth">Birth date as MM/dd/yyyy.</param>
    /// <param name="resultsPerPage">Page size of the result set.</param>
    public PersonQuery(string firstName, string lastName, string dateOfBirth, int resultsPerPage = 10)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        ResultsPerPage = resultsPerPage;
    }

    /// <summary>Builds a Person Search request by name and age.</summary>
    /// <param name="firstName">Filter on given name.</param>
    /// <param name="lastName">Filter on family name.</param>
    /// <param name="age">Filter on age.</param>
    /// <param name="middleName">Optional filter on middle name.</param>
    /// <param name="resultsPerPage">Page size of the result set.</param>
    public PersonQuery(string firstName, string lastName, int age, string? middleName = null, int resultsPerPage = 10)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        Age = age;
        ResultsPerPage = resultsPerPage;
    }

    public override string ToString()
        => $"PersonQuery: FirstName='{FirstName}', LastName='{LastName}', Dob='{DateOfBirth}', Age={Age}, ResultsPerPage={ResultsPerPage}, Page={Page}";
}