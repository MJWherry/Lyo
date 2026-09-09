namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// How <see cref="LyoParameterEditor" /> presents parameter rows. Hosts pick the default through <c>AddLyoParameterEditor</c>; users switch from the editor header
/// and their choice is remembered for that browser.
/// </summary>
public enum LyoParameterLayout
{
    /// <summary>Dense table, one row per parameter with an expander for details. Scans well on wide screens and when there are many parameters.</summary>
    Table = 0,

    /// <summary>One card per parameter whose fields reflow to the available width. Better on narrow screens and for parameters that have rich editors.</summary>
    Cards = 1
}
