using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Person.Request;

public class PersonQuery
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [JsonPropertyName("Dob")]
    public string? DateOfBirth { get; set; }

    public int ResultsPerPage { get; set; } = 10;

    public PersonQuery() { }

    /// <summary></summary>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="dateOfBirth">MM/dd/yyyy</param>
    public PersonQuery(string firstName, string lastName, string dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth; //.ToString("MM/dd/yyyy");
    }
}