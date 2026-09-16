namespace Lyo.Recipe.Models.Response;

/// <summary>A cooking unit as returned by the API.</summary>
public sealed class UnitRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short form stored on a line when this unit is picked.</summary>
    public string Abbreviation { get; set; } = string.Empty;

    /// <summary>Display order, lowest first.</summary>
    public int SortOrder { get; set; }
}
