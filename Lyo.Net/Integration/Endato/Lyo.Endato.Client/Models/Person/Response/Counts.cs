namespace Lyo.Endato.Client.Models.Person.Response;

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
    int ExpectedCount);