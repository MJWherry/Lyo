using Lyo.Exceptions;

namespace Lyo.EntityReference.Models;

/// <summary>Optional validation that <see cref="EntityRef.EntityType"/> is one of a closed set (feature gates, API bindings).</summary>
public static class EntityRefTypeGuard
{
    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="entityRef"/>.EntityType is not in <paramref name="allowedTypes"/>.</summary>
    /// <param name="entityRef">Reference whose type must be allow-listed.</param>
    /// <param name="allowedTypes">Supported logical entity type names.</param>
    /// <param name="paramName">Optional parameter name for the exception.</param>
    /// <exception cref="ArgumentNullException"><paramref name="allowedTypes"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The entity type is not contained in <paramref name="allowedTypes"/>.</exception>
    public static void EnsureKnown(EntityRef entityRef, IReadOnlyCollection<string> allowedTypes, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNull(allowedTypes);
        if (!allowedTypes.Contains(entityRef.EntityType))
            throw new ArgumentOutOfRangeException(paramName ?? nameof(entityRef),
                entityRef.EntityType,
                $"Entity type '{entityRef.EntityType}' is not in the allowed set for this operation.");
    }
}
