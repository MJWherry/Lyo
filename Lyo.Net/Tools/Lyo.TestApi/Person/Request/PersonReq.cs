namespace Lyo.TestApi.Person.Request;

public class PersonReq
{
    public Guid? EndatoEnrichedPersonId { get; set; }

    public Guid? EndatoPersonId { get; set; }

    public string? Prefix { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Suffix { get; set; }

    public string Source { get; set; } = null!;

    public List<PersonAddressReq> PersonAddresses { get; set; } = [];

    public List<PersonEmailAddressReq> PersonEmails { get; set; } = [];

    public List<PersonPhoneNumberReq> PersonPhoneNumbers { get; set; } = [];
}