using Lyo.Privacy.Json;

namespace Lyo.Privacy.Enums;

public enum JsonKeyRedactionStrategy
{
    /// <summary>Replace the scalar with the <see cref="JsonRedactorOptions.Placeholder" /> string.</summary>
    Placeholder,

    /// <summary>Replace the scalar with deterministic short hex derived from salt + key + value.</summary>
    HashStable,

    /// <summary>Drop the property from the parent object.</summary>
    Remove
}