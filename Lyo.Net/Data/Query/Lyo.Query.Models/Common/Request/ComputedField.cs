using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>
/// Computed field that applies a SmartFormat template to projected row data and adds the result as a named column. Needs IFormatterService registered. Templates
/// use SmartFormat syntax, e.g. "{LastName}, {FirstName}" or "{CreatedAt:yyyy-MM-dd}".
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ComputedField
{
    /// <summary>Output column name for this computed field (key in the projected row dictionary).</summary>
    public string Name { get; set; } = "";

    /// <summary>SmartFormat template. Placeholders reference other selected field names, e.g. "{LastName} {FirstName}".</summary>
    public string Template { get; set; } = "";

    public ComputedField() { }

    public ComputedField(string name, string template)
    {
        Name = name;
        Template = template;
    }

    public override string ToString() => $"{Name} = {Template}";
}