namespace Lyo.Parameters;

/// <summary>
/// How a declared parameter produces its default. Keeps default authoring separate from <see cref="ILyoParameterValue.Type" />, which always describes the value itself, so a
/// <c>System.DateTime</c> parameter can default to "yesterday" and still be edited and validated as a date.
/// </summary>
public enum LyoParameterDefaultKind
{
    /// <summary>The default is the literal in <see cref="ILyoKeyedValue.Value" />, already valid for the declared type.</summary>
    Literal = 0,

    /// <summary>
    /// The default is the template in <see cref="ILyoParameterDefinition.DefaultTemplate" />, rendered when no value is supplied. The rendered text must satisfy the declared
    /// type so validation stays type-strict.
    /// </summary>
    Expression
}
