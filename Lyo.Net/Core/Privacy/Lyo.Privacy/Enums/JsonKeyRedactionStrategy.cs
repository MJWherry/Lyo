using Lyo.Privacy.Json;

namespace Lyo.Privacy.Enums;

public enum JsonKeyRedactionStrategy
{
    /// <summary>Replace scalar with <see cref="JsonRedactorOptions.Placeholder" /> string.</summary>
    Placeholder,

    /// <summary>Replace scalar with deterministic short hex derived from salt + key + value.</summary>
    HashStable,

    /// <summary>Omit property from parent object.</summary>
    Remove
}