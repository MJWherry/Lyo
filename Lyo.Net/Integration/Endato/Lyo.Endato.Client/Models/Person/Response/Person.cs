namespace Lyo.Endato.Client.Models.Person.Response;

public sealed record Person(
    string TahoeId,
    Name Name,
    bool IsPublic,
    bool IsOptedOut,
    int SparseFlag,
    bool IsPremium,
    string FullName,
    int Age,
    string Dob,
    IReadOnlyList<DateOfBirth> DatesOfBirth,
    //"datesOfBirth": [],
    string DobFirstSeen,
    string DobLastSeen,
    //"datesOfDeath": [],
    IReadOnlyList<Name> MergedNames,
    IReadOnlyList<Location> Locations,
    IReadOnlyList<Address> Addresses,
    IReadOnlyList<Email> EmailAddresses,
    IReadOnlyList<Phone> PhoneNumbers,
    //"relativesSummary": [],
    IReadOnlyList<AssociateSummary> AssociateSummaries,
    IReadOnlyList<Associate> Associates,
    Indicators Indicators,
    //"driversLicenseDetail": [],
    bool HasAdditionalData
    //"propensityToPayScore"
);