namespace Lyo.Endato.Client.Models.Person.Response;

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
    bool Other);