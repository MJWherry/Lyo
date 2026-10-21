namespace Lyo.Law.Postgres.Database;

/// <summary>A US state. <c>US</c> is not a valid key.</summary>
public sealed class LawJurisdictionEntity
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>State key, such as <c>US-PA</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>When the row was inserted.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Compilations in this state.</summary>
    public List<LawCodeEntity> Codes { get; set; } = [];
}