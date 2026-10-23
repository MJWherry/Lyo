namespace Lyo.Recipe;

/// <summary>A known cooking unit. Lines may still store a unit string that is not in this catalog.</summary>
public sealed class UnitRecord
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name, such as "teaspoon".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short form stored on a line when the cook picks this unit, such as "tsp".</summary>
    public string Abbreviation { get; set; } = string.Empty;

    /// <summary>Display order, lowest first.</summary>
    public int SortOrder { get; set; }
}