using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Person.Request;

/// <summary>Request body for Endato Person Search (<c>POST /PersonSearch</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonQuery
{
    /// <summary>First name filter.</summary>
    public string? FirstName { get; set; }

    /// <summary>Middle name filter.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Last name filter.</summary>
    public string? LastName { get; set; }

    /// <summary>Date of birth filter (wire name <c>dob</c>).</summary>
    [JsonPropertyName("Dob")]
    public string? DateOfBirth { get; set; }

    /// <summary>Age filter.</summary>
    public int? Age { get; set; }

    /// <summary>Minimum age when using an age range.</summary>
    public int? AgeRangeMinAge { get; set; }

    /// <summary>Maximum age when using an age range.</summary>
    public int? AgeRangeMaxAge { get; set; }

    /// <summary>Age range expression.</summary>
    public string? AgeRange { get; set; }

    /// <summary>Also-known-as name rows.</summary>
    public IReadOnlyList<PersonQueryName>? Akas { get; set; }

    /// <summary>Relative name rows.</summary>
    public IReadOnlyList<PersonQueryName>? Relatives { get; set; }

    /// <summary>Address rows used as search criteria.</summary>
    public IReadOnlyList<PersonQueryAddress>? Addresses { get; set; }

    /// <summary>Email filter.</summary>
    public string? Email { get; set; }

    /// <summary>Phone filter.</summary>
    public string? Phone { get; set; }

    /// <summary>Client IP forwarded to Endato when required by the account.</summary>
    public string? ClientIp { get; set; }

    /// <summary>Endato Tahoe identifiers.</summary>
    public IReadOnlyList<string>? TahoeIds { get; set; }

    /// <summary>First-name character offset for fuzzy matching.</summary>
    public int? FirstNameCharOffset { get; set; }

    /// <summary>Last-name character offset for fuzzy matching.</summary>
    public int? LastNameCharOffset { get; set; }

    /// <summary>Expected date-of-birth format for <see cref="DateOfBirth" />.</summary>
    public string? DobFormat { get; set; }

    /// <summary>Maximum age in years for returned addresses.</summary>
    public int? MaxAddressYears { get; set; }

    /// <summary>Maximum age in years for returned phone numbers.</summary>
    public int? MaxPhoneYears { get; set; }

    /// <summary>Page number (1-based).</summary>
    public int? Page { get; set; }

    /// <summary>Results per page.</summary>
    public int ResultsPerPage { get; set; } = 2;

    /// <summary>Nested includes to expand on each person.</summary>
    public IReadOnlyList<string>? Includes { get; set; }

    /// <summary>Filter options applied to the search.</summary>
    public IReadOnlyList<string>? FilterOptions { get; set; }

    /// <summary>Creates an empty Person Search request.</summary>
    public PersonQuery() { }

    /// <summary>Creates a Person Search request by name and date of birth.</summary>
    /// <param name="firstName">First name filter.</param>
    /// <param name="lastName">Last name filter.</param>
    /// <param name="dateOfBirth">Date of birth in MM/dd/yyyy format.</param>
    /// <param name="resultsPerPage">Results per page.</param>
    public PersonQuery(string firstName, string lastName, string dateOfBirth, int resultsPerPage = 10)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        ResultsPerPage = resultsPerPage;
    }

    /// <summary>Creates a Person Search request by name and age.</summary>
    /// <param name="firstName">First name filter.</param>
    /// <param name="lastName">Last name filter.</param>
    /// <param name="age">Age filter.</param>
    /// <param name="middleName">Optional middle name filter.</param>
    /// <param name="resultsPerPage">Results per page.</param>
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