using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CriteriaType(
    bool Ssn,
    bool TahoeId,
    bool NameState,
    bool NameCityState,
    bool NameZip,
    bool NameAddress,
    bool NameDob,
    bool AddressOnly,
    bool Email,
    bool Phone,
    bool DriverLicense,
    bool Other)
{
    public override string ToString()
    {
        var enabled = new List<string>();
        if (Ssn)
            enabled.Add(nameof(Ssn));

        if (TahoeId)
            enabled.Add(nameof(TahoeId));

        if (NameState)
            enabled.Add(nameof(NameState));

        if (NameCityState)
            enabled.Add(nameof(NameCityState));

        if (NameZip)
            enabled.Add(nameof(NameZip));

        if (NameAddress)
            enabled.Add(nameof(NameAddress));

        if (NameDob)
            enabled.Add(nameof(NameDob));

        if (AddressOnly)
            enabled.Add(nameof(AddressOnly));

        if (Email)
            enabled.Add(nameof(Email));

        if (Phone)
            enabled.Add(nameof(Phone));

        if (DriverLicense)
            enabled.Add(nameof(DriverLicense));

        if (Other)
            enabled.Add(nameof(Other));

        return enabled.Count == 0 ? "CriteriaType: (none)" : $"CriteriaType: {string.Join(", ", enabled)}";
    }
}