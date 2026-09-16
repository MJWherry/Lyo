namespace Lyo.Recipe.Models.Request;

/// <summary>Create or update a cooking unit. An empty <see cref="Id"/> inserts a row.</summary>
public sealed class UnitReq
{
    /// <summary>Existing unit id. Empty creates a new unit.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name, such as "teaspoon".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short form, such as "tsp". Unique per tenant.</summary>
    public string Abbreviation { get; set; } = string.Empty;

    /// <summary>Display order, lowest first.</summary>
    public int SortOrder { get; set; }
}
