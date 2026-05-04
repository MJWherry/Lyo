using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Counts(
    int SearchResults,
    bool SearchResultsOverflow,
    int Names,
    int SocialSecurityNumbers,
    int DatesOfBirth,
    int DatesOfDeath,
    int Addresses,
    int PhoneNumbers,
    int EmailAddresses,
    int Relatives,
    int Associates,
    int BusinessRecord,
    int DebtRecords,
    int EvictionRecords,
    int ForeclosureRecords,
    int ForeclosureV2Records,
    int ProfessionalLicenseRecords,
    int ExpectedCount)
{
    public override string ToString()
        => $"Counts: SearchResults={SearchResults}, Overflow={SearchResultsOverflow}, Expected={ExpectedCount}, " +
           $"Names={Names}, Addresses={Addresses}, Phones={PhoneNumbers}, Emails={EmailAddresses}, Associates={Associates}";
}