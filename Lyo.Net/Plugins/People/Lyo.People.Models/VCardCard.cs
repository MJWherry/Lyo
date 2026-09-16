namespace Lyo.People.Models;

/// <summary>vCard 3.0 fields this library reads and writes. Repeated properties stay in list order.</summary>
public sealed class VCardCard
{
    /// <summary>Formatted name (FN).</summary>
    public string? FormattedName { get; set; }

    /// <summary>Family name from N.</summary>
    public string? FamilyName { get; set; }

    /// <summary>Given name from N.</summary>
    public string? GivenName { get; set; }

    /// <summary>Telephone values, in file order.</summary>
    public List<string> Telephones { get; } = [];

    /// <summary>Email values, in file order.</summary>
    public List<string> Emails { get; } = [];

    /// <summary>Addresses, in file order.</summary>
    public List<VCardAddress> Addresses { get; } = [];

    /// <summary>Organization (ORG).</summary>
    public string? Organization { get; set; }

    /// <summary>Job title (TITLE).</summary>
    public string? Title { get; set; }

    /// <summary>URL.</summary>
    public string? Url { get; set; }
}
