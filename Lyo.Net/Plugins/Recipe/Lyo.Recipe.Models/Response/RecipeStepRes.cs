namespace Lyo.Recipe.Models.Response;

/// <summary>One instruction as returned by the API.</summary>
public sealed class RecipeStepRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning recipe.</summary>
    public Guid RecipeId { get; set; }

    /// <summary>Display order, lowest first.</summary>
    public int SortOrder { get; set; }

    /// <summary>What to do.</summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>How long this step takes, when known.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>Section heading such as "Make the dough".</summary>
    public string? GroupLabel { get; set; }
}