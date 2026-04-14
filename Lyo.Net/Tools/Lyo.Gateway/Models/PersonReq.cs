namespace Lyo.Gateway.Models;

public sealed class PersonReq
{
    public Guid? EndatoEnrichedPersonId { get; set; }

    public Guid? EndatoPersonId { get; set; }

    public string? Prefix { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Suffix { get; set; }

    public string Source { get; set; } = "Manual";

    public List<PersonAddressReq> PersonAddresses { get; set; } = [];
}