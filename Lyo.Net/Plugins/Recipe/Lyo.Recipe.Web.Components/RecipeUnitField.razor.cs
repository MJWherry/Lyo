namespace Lyo.Recipe.Web.Components;

/// <summary>Unit text box that suggests catalog abbreviations and still accepts a typed value.</summary>
public partial class RecipeUnitField
{
    /// <summary>Stored unit text. A catalog pick writes the abbreviation. Typed text is kept as-is.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Raised when the cook picks or types a unit.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Known units. The field searches name and abbreviation.</summary>
    [Parameter]
    public IReadOnlyList<UnitRes> Units { get; set; } = [];

    /// <summary>Field label.</summary>
    [Parameter]
    public string Label { get; set; } = "Unit";

    private Task<IEnumerable<string>> Search(string? value, CancellationToken ct)
    {
        IEnumerable<UnitRes> matches = Units;
        if (!string.IsNullOrWhiteSpace(value)) {
            matches = Units.Where(u => u.Abbreviation.Contains(value, StringComparison.OrdinalIgnoreCase) || u.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        var abbreviations = matches.Select(u => u.Abbreviation).Distinct(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(value) && abbreviations.All(a => !string.Equals(a, value, StringComparison.OrdinalIgnoreCase)))
            abbreviations = abbreviations.Prepend(value);

        return Task.FromResult(abbreviations);
    }
}