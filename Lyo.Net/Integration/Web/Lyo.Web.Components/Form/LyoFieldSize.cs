namespace Lyo.Web.Components.Form;

/// <summary>
/// Layout width hint for a <see cref="LyoFormInput{TModel,TValue}" /> hosted inside a <c>LyoFormGrid</c>.
/// <c>Auto</c> derives a sensible width from the property type (bools/numerics/times narrow, strings wide).
/// </summary>
public enum LyoFieldSize
{
    /// <summary>Derive the width from the property type.</summary>
    Auto,

    /// <summary>Narrow column for bools, numerics, and time values (xs=6 sm=4 md=3).</summary>
    Small,

    /// <summary>Medium column for enums, dates, and GUIDs (xs=12 sm=6 md=4).</summary>
    Medium,

    /// <summary>Wide column for short strings (xs=12 md=6).</summary>
    Large,

    /// <summary>Full row, for multiline text and editors (xs=12).</summary>
    Full
}
