namespace Lyo.Parameters;

/// <summary>JSON element shape written by <see cref="ParameterListJson.Serialize" />.</summary>
public enum ParameterListJsonKind
{
    /// <summary>String elements on the wire.</summary>
    String,

    /// <summary>Number elements (invariant parse from string chips).</summary>
    Number,

    /// <summary>Boolean elements on the wire.</summary>
    Bool
}