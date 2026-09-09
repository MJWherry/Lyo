namespace Lyo.Privacy.Enums;

/// <summary>Category of redacted content for audit summaries. This is not a legal taxonomy.</summary>
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

    /// <summary>Physical address pattern (best-effort regex; false positives are expected).</summary>
    Address,

    /// <summary>International bank account number (IBAN) after structural and MOD-97 checks.</summary>
    Iban,

    /// <summary>Generic digit block that looks like a bank or account number (heuristic).</summary>
    BankAccountNumber,

    /// <summary>API keys, PATs, and KEY=value-style high-entropy material.</summary>
    ApiSecret,

    /// <summary>Opt-in national tax and ID packs (SSN, NINO, and similar).</summary>
    TaxId,

    /// <summary>XML element or attribute value redacted using the local name.</summary>
    XmlSensitive
}