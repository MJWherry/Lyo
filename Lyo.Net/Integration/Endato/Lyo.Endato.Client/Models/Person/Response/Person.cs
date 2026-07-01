using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Person record returned by Endato Person Search.</summary>
[DebuggerDisplay("{ToString(),nq}")]
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
    string DobFirstSeen,
    string DobLastSeen,
    IReadOnlyList<Name> MergedNames,
    IReadOnlyList<Location>? Locations,
    IReadOnlyList<Address> Addresses,
    IReadOnlyList<Email> EmailAddresses,
    IReadOnlyList<Phone> PhoneNumbers,
    IReadOnlyList<AssociateSummary> AssociateSummaries,
    Indicators Indicators,
    bool HasAdditionalData,
    IReadOnlyList<Associate>? Associates = null,
    IReadOnlyList<Name>? Akas = null,
    IReadOnlyList<RelativeSummary>? RelativesSummary = null,
    IReadOnlyList<DeathRecord>? DeathRecords = null)
{
    public override string ToString()
        => $"Person: TahoeId={TahoeId}, FullName='{FullName}', Age={Age}, Dob={Dob}, Premium={IsPremium}, Addresses={Addresses.Count}, Emails={EmailAddresses.Count}, Phones={PhoneNumbers.Count}, Associates={Associates?.Count ?? 0}, Akas={Akas?.Count ?? 0}, Relatives={RelativesSummary?.Count ?? 0}, Public={IsPublic}, OptOut={IsOptedOut}";
}