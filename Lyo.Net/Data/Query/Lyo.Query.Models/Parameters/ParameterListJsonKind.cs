namespace Lyo.Query.Models.Parameters;

/// <summary>Wire shape for elements written by <see cref="ParameterListJson.Serialize" />.</summary>
public enum ParameterListJsonKind
{
    /// <summary>JSON string elements.</summary>
    String,

    /// <summary>JSON number elements (invariant parse from string chips).</summary>
    Number,

    /// <summary>JSON boolean elements.</summary>
    Bool
}