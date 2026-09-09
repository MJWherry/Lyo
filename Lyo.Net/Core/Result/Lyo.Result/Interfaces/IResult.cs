namespace Lyo.Result.Interfaces;

/// <summary>Non-generic contract for any result type. Allows uniform handling without knowing the data type.</summary>
public interface IResult
{
    /// <summary>True when the operation succeeded.</summary>
    bool IsSuccess { get; }

    /// <summary>Errors produced by a failed operation, or null on success.</summary>
    IReadOnlyList<Error>? Errors { get; }

    /// <summary>UTC timestamp when the result was created.</summary>
    DateTime Timestamp { get; }

    /// <summary>Optional metadata attached to the result.</summary>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}

/// <summary>Generic contract for a result that carries a typed data value on success.</summary>
/// <typeparam name="T">Type of the data returned on success.</typeparam>
public interface IResult<out T> : IResult
{
    /// <summary>Data value on success, or default on failure.</summary>
    T? Data { get; }
}