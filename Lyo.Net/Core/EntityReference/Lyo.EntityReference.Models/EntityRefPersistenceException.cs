namespace Lyo.EntityReference.Models;

/// <summary>Thrown when an <see cref="EntityRef"/> cannot be persisted under Option A (single <see cref="Guid"/> key).</summary>
public sealed class EntityRefPersistenceException : Exception
{
    /// <summary>Creates an exception describing invalid persisted-ref input.</summary>
    /// <param name="message">Human-readable explanation.</param>
    public EntityRefPersistenceException(string message)
        : base(message) { }

    /// <summary>Creates an exception with an inner cause.</summary>
    /// <param name="message">Human-readable explanation.</param>
    /// <param name="innerException">Underlying error, if any.</param>
    public EntityRefPersistenceException(string message, Exception innerException)
        : base(message, innerException) { }
}
