namespace Lyo.Algorithms;

/// <summary>Thrown when an algorithm key is missing, a label cannot move, or execute-time resolution fails.</summary>
public sealed class AlgorithmException(string message, string? code = null) : Exception(message)
{
    /// <summary>Stable error code for catalog <see cref="Lyo.Result.Result{T}" /> failures, when this exception was raised by a store.</summary>
    public string? Code { get; } = code;
}
