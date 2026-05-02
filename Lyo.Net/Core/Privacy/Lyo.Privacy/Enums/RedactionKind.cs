namespace Lyo.Privacy.Enums;

/// <summary>Category of redacted content for audit summaries. Not a legal taxonomy.</summary>
public enum RedactionKind
{
    None = 0,
    Email,
    Phone,
    PaymentCard,
    Url,
    IpAddress,
    JsonKey,
    Regex,
    Literal,
    Custom,
    Composite,

    /// <summary>Physical address pattern (best-effort regex; expect false positives).</summary>
    Address,

    /// <summary>International bank account number (IBAN) after structural / MOD-97 checks.</summary>
    Iban,

    /// <summary>Generic digit block that looks like a bank / account number (heuristic).</summary>
    BankAccountNumber,

    /// <summary>API keys, PATs, and KEY=value-style high-entropy material.</summary>
    ApiSecret,

    /// <summary>Opt-in national tax / ID packs (SSN, NINO, etc.).</summary>
    TaxId,

    /// <summary>XML element or attribute value redacted by local name.</summary>
    XmlSensitive
}