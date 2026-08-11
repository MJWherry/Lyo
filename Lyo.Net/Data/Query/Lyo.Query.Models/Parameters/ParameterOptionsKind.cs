namespace Lyo.Query.Models.Parameters;

/// <summary>Discriminator for <see cref="ParameterOptions" /> — static key/label list or root <c>/Query</c> template.</summary>
public enum ParameterOptionsKind
{
    /// <summary>Hardcoded <see cref="ParameterOptions.Items" />.</summary>
    Static,

    /// <summary>Options loaded by executing <see cref="ParameterOptions.Query" /> against the root query endpoint.</summary>
    Query
}
