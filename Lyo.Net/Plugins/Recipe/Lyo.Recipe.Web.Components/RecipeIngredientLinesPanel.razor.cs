using Lyo.Common.Core.Identifiers;

namespace Lyo.Recipe.Web.Components;

/// <summary>Reorderable ingredient lines. The parent owns the list and saves it with the recipe.</summary>
public partial class RecipeIngredientLinesPanel
{
    /// <summary>Lines in display order.</summary>
    [Parameter]
    public List<RecipeIngredientLineReq> Lines { get; set; } = [];

    /// <summary>Catalog ingredients for the name field.</summary>
    [Parameter]
    public IReadOnlyList<IngredientRes> Ingredients { get; set; } = [];

    /// <summary>Known units for the unit field.</summary>
    [Parameter]
    public IReadOnlyList<UnitRes> Units { get; set; } = [];

    /// <summary>Steps the cook can tag onto a line. Chip text is the step's current position.</summary>
    [Parameter]
    public IReadOnlyList<RecipeStepReq> Steps { get; set; } = [];

    /// <summary>Raised after a line is added, removed, or retagged.</summary>
    [Parameter]
    public EventCallback Changed { get; set; }

    private string IngredientLabel(RecipeIngredientLineReq line)
    {
        if (line.IngredientId is Guid id) {
            var match = Ingredients.FirstOrDefault(i => i.Id == id);
            if (match != null)
                return match.Name;
        }

        return line.IngredientText ?? "";
    }

    private Task<IEnumerable<string>> SearchIngredients(string? value, CancellationToken ct)
    {
        IEnumerable<string> names = Ingredients.Select(i => i.Name);
        if (!string.IsNullOrWhiteSpace(value))
            names = names.Where(n => n.Contains(value, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(value) && names.All(n => !string.Equals(n, value, StringComparison.OrdinalIgnoreCase)))
            names = names.Prepend(value);
        return Task.FromResult(names);
    }

    private async Task OnIngredientChanged(RecipeIngredientLineReq line, string? value)
    {
        var match = string.IsNullOrWhiteSpace(value)
            ? null
            : Ingredients.FirstOrDefault(i => string.Equals(i.Name, value.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match != null) {
            line.IngredientId = match.Id;
            line.IngredientText = null;
            if (string.IsNullOrWhiteSpace(line.Unit) && !string.IsNullOrWhiteSpace(match.DefaultUnit))
                line.Unit = match.DefaultUnit;
        }
        else {
            line.IngredientId = null;
            line.IngredientText = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        if (Changed.HasDelegate)
            await Changed.InvokeAsync();
    }

    private async Task ToggleStep(RecipeIngredientLineReq line, Guid stepId)
    {
        if (!line.StepIds.Remove(stepId))
            line.StepIds.Add(stepId);
        if (Changed.HasDelegate)
            await Changed.InvokeAsync();
    }

    private async Task Add()
    {
        Lines.Add(new RecipeIngredientLineReq { Id = LyoGuid.CreateV7(), SortOrder = Lines.Count });
        if (Changed.HasDelegate)
            await Changed.InvokeAsync();
    }

    private async Task Remove(RecipeIngredientLineReq line)
    {
        Lines.Remove(line);
        if (Changed.HasDelegate)
            await Changed.InvokeAsync();
    }
}
