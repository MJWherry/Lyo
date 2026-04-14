namespace Lyo.People.Models.Enum;

/// <summary>Type of identification document</summary>
public enum IdentificationType
{
    /// <summary>Passport</summary>
    Passport,

    /// <summary>Driver's license</summary>
    DriversLicense,

    /// <summary>National ID card</summary>
    NationalId,

    /// <summary>Social Security Number (US)</summary>
    SSN,

    /// <summary>Tax identification number</summary>
    TaxId,

    /// <summary>Voter registration ID</summary>
    VoterId,

    /// <summary>Other or unspecified identification</summary>
    Other
}