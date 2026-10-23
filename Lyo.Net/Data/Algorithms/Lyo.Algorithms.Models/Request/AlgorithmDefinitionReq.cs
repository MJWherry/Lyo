namespace Lyo.Algorithms.Models.Request;

/// <summary>Caller-writable metadata on a catalog definition. The key is the route, not the body.</summary>
public sealed class AlgorithmDefinitionReq
{
    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}