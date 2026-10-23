namespace Lyo.Algorithms;

/// <summary>A callable algorithm. Startup C# types and compiled expressions both implement this.</summary>
/// <typeparam name="TContext">Input the body reads (a DTO or <see cref="AlgorithmContextBag" />).</typeparam>
/// <typeparam name="TResult">Value <see cref="Execute" /> returns.</typeparam>
public interface IAlgorithm<in TContext, out TResult>
{
    /// <summary>Registry key.</summary>
    string Key { get; }

    /// <summary>Runs the body. Compiled expressions do not touch the database.</summary>
    TResult Execute(TContext context);
}