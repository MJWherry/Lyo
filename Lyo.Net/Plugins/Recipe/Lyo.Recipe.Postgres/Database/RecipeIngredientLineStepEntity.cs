namespace Lyo.Recipe.Postgres.Database;

/// <summary>Links one ingredient line to one step on the same recipe.</summary>
public sealed class RecipeIngredientLineStepEntity
{
    public Guid LineId { get; set; }

    public Guid StepId { get; set; }
}
