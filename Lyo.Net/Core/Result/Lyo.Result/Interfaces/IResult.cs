namespace Lyo.Result.Interfaces;

/// <summary>Non-generic contract for any result type. Allows uniform handling without knowing the data type.</summary>
public interface IResult
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    bool IsSuccess { get; }

    /// <summary>Gets the errors produced by a failed operation, or null on success.</summary>
    IReadOnlyList<Error>? Errors { get; }

    /// <summary>Gets the UTC timestamp when the result was created.</summary>
    DateTime Timestamp { get; }

    /// <summary>Gets optional metadata attached to the result.</summary>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}

/// <summary>Generic contract for a result that carries a typed data value on success.</summary>
/// <typeparam name="T">The type of the data returned on success.</typeparam>
public interface IResult<out T> : IResult
{
    /// <summary>Gets the data value on success, or default on failure.</summary>
    T? Data { get; }
}