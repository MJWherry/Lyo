using Lyo.Exceptions;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Host defaults for <see cref="LyoParameterEditor" />. Wire with <c>AddLyoParameterEditor</c>; hosts that never register it get the table layout with the
/// toggle available.
/// </summary>
public sealed class LyoParameterEditorOptions
{
    /// <summary>Configuration section bound by <c>AddLyoParameterEditorFromConfiguration</c>.</summary>
    public const string SectionName = "ParameterEditor";

    /// <summary>Layout used until the user picks a different one. A stored user choice always wins over this.</summary>
    public LyoParameterLayout Layout { get; set; } = LyoParameterLayout.Table;

    /// <summary>Show the layout toggle in the editor header. Set false to pin every editor to <see cref="Layout" />.</summary>
    public bool AllowLayoutToggle { get; set; } = true;

    /// <summary>Throws when <see cref="Layout" /> is not a declared enum value, which configuration binding can produce.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNotDefined(Layout);
}
