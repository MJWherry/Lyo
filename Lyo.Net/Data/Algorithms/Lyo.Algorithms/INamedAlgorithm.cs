namespace Lyo.Algorithms;

/// <summary>Startup C# algorithm. Registered with <c>AddNamedAlgorithm</c> and frozen for the process. A catalog cannot introduce a new CLR type.</summary>
/// <typeparam name="TContext">Input the body reads.</typeparam>
/// <typeparam name="TResult">Value <see cref="IAlgorithm{TContext,TResult}.Execute" /> returns.</typeparam>
public interface INamedAlgorithm<in TContext, out TResult> : IAlgorithm<TContext, TResult>;