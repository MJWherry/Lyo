using System.Diagnostics;

namespace Lyo.Recipe.Postgres.Database;

/// <summary>Links one ingredient line to one step on the same recipe.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RecipeIngredientLineStepEntity
{
    public Guid LineId { get; set; }

    public Guid StepId { get; set; }

    /// <summary>Type name and primary key.</summary>
    public override string ToString() => $"{GetType().Name} {LineId} {StepId}";
}