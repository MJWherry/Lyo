namespace Lyo.Parameters;

/// <summary>Kind of <see cref="ParameterOptions" /> — a static key/label list, a root <c>/Query</c> template, or a stored procedure.</summary>
public enum ParameterOptionsKind
{
    /// <summary>Fixed <see cref="ParameterOptions.Items" />.</summary>
    Static,

    /// <summary>Choices loaded by running <see cref="ParameterOptions.Query" /> against the root query endpoint.</summary>
    Query,

    /// <summary>Choices or dataset rows loaded by running <see cref="ParameterOptions.StoredProcName" />.</summary>
    Sproc
}
