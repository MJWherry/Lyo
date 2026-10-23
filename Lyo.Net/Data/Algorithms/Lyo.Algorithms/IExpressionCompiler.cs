using Lyo.Result;

namespace Lyo.Algorithms;

/// <summary>Compiles a math expression once and reuses the delegate. Identifiers are context properties or bag keys, not host types.</summary>
public interface IExpressionCompiler
{
    /// <summary>Parses and allowlists <paramref name="source" /> without binding a context type. Unknown names are treated as numeric inputs.</summary>
    Result<Unit> Validate(string source);

    /// <summary>Parses and allowlists <paramref name="source" /> using <paramref name="binding" /> for identifier and call types.</summary>
    Result<Unit> Validate(string source, AlgorithmTypeBinding binding);

    /// <summary>Compiles <paramref name="source" /> for <typeparamref name="TContext" />. A second call with the same source and types returns the same instance.</summary>
    Result<IAlgorithm<TContext, TResult>> Compile<TContext, TResult>(string source);

    /// <summary>
    /// Compiles <paramref name="source" /> for <typeparamref name="TContext" />. The cache key includes <paramref name="binding" />, so the same source with two maps does not share a
    /// delegate.
    /// </summary>
    Result<IAlgorithm<TContext, TResult>> Compile<TContext, TResult>(string source, AlgorithmTypeBinding binding);
}