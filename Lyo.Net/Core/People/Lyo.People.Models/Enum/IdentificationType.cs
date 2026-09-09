namespace Lyo.People.Models.Enum;

/// <summary>Kind of identity document.</summary>
public enum IdentificationType
{
    /// <summary>Passport.</summary>
    Passport,

    /// <summary>Driver's license.</summary>
    DriversLicense,

    /// <summary>National identity card.</summary>
    NationalId,

    /// <summary>US Social Security Number.</summary>
    SSN,

    /// <summary>Tax identification number.</summary>
    TaxId,

    /// <summary>Voter registration identifier.</summary>
    VoterId,

    /// <summary>Unspecified or other document.</summary>
    Other
}
