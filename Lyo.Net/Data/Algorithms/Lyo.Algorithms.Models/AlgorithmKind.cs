namespace Lyo.Algorithms.Models;

/// <summary>What a catalog revision stores.</summary>
public enum AlgorithmKind
{
    /// <summary>DynamicExpresso source compiled on first execute.</summary>
    Expression = 0,

    /// <summary>Name of a startup algorithm registered on the host. The catalog does not contain C#.</summary>
    Named = 1,

    /// <summary>Maps a context value to points. Missing keys score zero or fail, from the payload.</summary>
    DiscreteMap = 2,

    /// <summary>Half-open numeric bands. Each band is <c>[min, max)</c>.</summary>
    RangeBand = 3,

    /// <summary>Linear map from an input range to a points range.</summary>
    Linear = 4,

    /// <summary>Ordered children combined by sum, weighted sum, or a formula.</summary>
    Composite = 5
}
