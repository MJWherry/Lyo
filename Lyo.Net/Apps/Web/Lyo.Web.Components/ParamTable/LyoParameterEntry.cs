using Lyo.Parameters;
using Lyo.Web.Components.LyoType;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// One editable value for a parameter definition, as used by the "run this thing" dialogs (job runs, report generations). Wraps the definition so the dialog does not care
/// whether it
/// came from a job or a report, and keeps the user's in-progress JSON separate from the definition's default.
/// </summary>
/// <param name="definition">Definition being filled in. Supplies key, type, options, and required flag.</param>
/// <param name="resolver">
/// Shows expression defaults so the dialog opens with the resolved value in the type's own editor, for example a date picker showing yesterday instead of a template.
/// Null leaves an expression default unresolved, which the server then resolves on submit.
/// </param>
public sealed class LyoParameterEntry(ILyoParameterDefinition definition, LyoTemplateResolver? resolver = null)
{
    /// <summary>Definition this entry fills in.</summary>
    public ILyoParameterDefinition Definition { get; } = definition;

    /// <summary>
    /// Current editor value as JSON. Seeded from the resolved expression default, the definition's literal value, or the type's default when the parameter is required or boolean.
    /// Optional parameters without a stored default stay unset.
    /// </summary>
    public string? Value { get; set; } = SeedValue(definition, resolver);

    /// <summary>Caption naming the expression the seeded value came from, so an editable pre-filled value does not look hand-entered. Null for a literal default.</summary>
    public string? DefaultExpressionHint { get; } = LyoParameterDefaults.IsExpression(LyoParameterSpec.From(definition))
        ? $"Default from expression: {definition.DefaultTemplate}"
        : null;

    /// <summary>Whether the caller asked for this value to be encrypted at rest. Only meaningful where the dialog displays the Encrypt switch.</summary>
    public bool Encrypted { get; set; }

    /// <summary>Editor label: the key, with a trailing asterisk when required.</summary>
    public string Label => Definition.Required ? $"{Definition.Key} *" : Definition.Key;

    /// <summary>
    /// Whether the user has supplied nothing usable. Booleans are never empty (false is a real value), and a collection type with an empty list counts as empty even though
    /// its JSON
    /// is not blank.
    /// </summary>
    public bool IsEmpty()
    {
        if (LyoTypeUi.IsBoolean(Definition.Type))
            return false;

        if (string.IsNullOrWhiteSpace(Value) || Value == "null")
            return true;

        return LyoTypeUi.IsCollection(Definition.Type) && ParameterListJson.Parse(Value).Count == 0;
    }

    /// <summary>Value to send to the API, with blanks and the literal <c>null</c> normalized to null.</summary>
    public string? SubmitValue => string.IsNullOrWhiteSpace(Value) || Value == "null" ? null : Value;

    /// <summary>
    /// Whether this entry belongs in the request. Optional parameters the user left blank are omitted so the server falls back to the definition default, but booleans are always
    /// sent because omitting one is indistinguishable from false.
    /// </summary>
    public bool ShouldSubmit => SubmitValue is not null || Definition.Required || LyoTypeUi.IsBoolean(Definition.Type);

    /// <summary>Constructs entries for a definition's parameters.</summary>
    /// <param name="definitions">Parameter definitions. Kept in source order unless <paramref name="order" /> is supplied.</param>
    /// <param name="order">Sort key for definitions that carry an explicit display order; ties break on key.</param>
    /// <param name="resolver">Draws expression defaults so those entries open pre-filled. See the constructor.</param>
    public static List<LyoParameterEntry> From<TDefinition>(
        IEnumerable<TDefinition>? definitions,
        Func<TDefinition, int>? order = null,
        LyoTemplateResolver? resolver = null)
        where TDefinition : ILyoParameterDefinition
    {
        if (definitions is null)
            return [];

        if (order is not null)
            definitions = definitions.OrderBy(order).ThenBy(d => d.Key, StringComparer.OrdinalIgnoreCase);

        return definitions.Select(d => new LyoParameterEntry(d, resolver)).ToList();
    }

    /// <summary>True when every required entry has a value. Use to gate the dialog's submit button.</summary>
    public static bool RequiredSatisfied(IEnumerable<LyoParameterEntry> entries) => entries.Where(e => e.Definition.Required).All(e => !e.IsEmpty());

    /// <summary>Key to current value for every entry, which query-backed option lists use to resolve <c>{{Key}}</c> placeholders against sibling parameters.</summary>
    public static Dictionary<string, string?> SiblingMap(IEnumerable<LyoParameterEntry> entries)
        => entries.ToDictionary(e => e.Definition.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Starting JSON for the editor. An expression default is rendered and normalized to the declared type so the dialog displays the type's own editor pre-filled; a template that
    /// will not resolve falls back to the type default, leaving the server to report the problem instead of the dialog silently showing a broken value. Optional parameters
    /// without a stored default stay unset so a dropdown (enum, options) can be left empty instead of snapping to the type's first/zero value.
    /// </summary>
    private static string? SeedValue(ILyoParameterDefinition definition, LyoTemplateResolver? resolver)
    {
        var spec = LyoParameterSpec.From(definition);
        if (LyoParameterDefaults.IsExpression(spec) && LyoParameterDefaults.TryResolve(spec, definition.Value, resolver, out var resolved, out _) && resolved is not null)
            return resolved;

        if (!string.IsNullOrEmpty(definition.Value))
            return definition.Value;

        if (definition.Required || LyoTypeUi.IsBoolean(definition.Type))
            return LyoTypeUi.DefaultJson(definition.Type);

        return null;
    }
}
