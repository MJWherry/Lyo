namespace Lyo.Algorithms;

/// <summary>How an <see cref="AlgorithmRef" /> picks a catalog revision.</summary>
public enum AlgorithmAddress
{
    /// <summary>Follow <see cref="AlgorithmDefinitionRes.CurrentRevision" />.</summary>
    Latest = 0,

    /// <summary>A specific revision number. Never moves.</summary>
    Revision = 1,

    /// <summary>A named label such as <c>live</c> or <c>game.1.0</c>.</summary>
    Label = 2
}
