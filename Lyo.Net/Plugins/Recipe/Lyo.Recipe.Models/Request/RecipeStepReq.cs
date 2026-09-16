namespace Lyo.Recipe.Models.Request;

/// <summary>One ordered instruction on a recipe.</summary>
public sealed class RecipeStepReq
{
    /// <summary>Existing step id. Empty creates a new step. Send the same id to update that row.</summary>
    public Guid Id { get; set; }

    /// <summary>Display order, lowest first.</summary>
    public int SortOrder { get; set; }

    /// <summary>What to do.</summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>How long this step takes, when known.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>Section heading such as "Make the dough".</summary>
    public string? GroupLabel { get; set; }
}
