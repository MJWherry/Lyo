using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Indicators(
    int HasBankruptcyRecords,
    int HasBusinessRecords,
    int HasDivorceRecords,
    int HasDomainsRecords,
    int HasEvictionsRecords,
    int HasFeinRecords,
    int HasForeclosuresRecords,
    int HasForeclosuresV2Records,
    int HasJudgmentRecords,
    int HasLienRecords,
    int HasMarriageRecords,
    int HasProfessionalLicenseRecords,
    int HasPropertyRecords,
    int HasVehicleRegistrationsRecords,
    int HasWorkplaceRecords,
    int HasDeaRecords,
    int HasPropertyV2Records,
    int HasUccRecords,
    int HasUnbankedData,
    int HasMobilePhones,
    int HasLandLines,
    int HasEmails,
    int HasAddresses,
    int HasCurrentAddresses,
    int HasHistoricalAddresses,
    int HasDebtRecords)
{
    public override string ToString()
        => $"Indicators: bankruptcy={HasBankruptcyRecords}, liens={HasLienRecords}, judgments={HasJudgmentRecords}, " +
           $"property={HasPropertyRecords}, emails={HasEmails}, mobile={HasMobilePhones}, landlines={HasLandLines}, " +
           $"addresses={HasAddresses}, debt={HasDebtRecords}";
}