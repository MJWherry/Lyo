namespace Lyo.Algorithms;

internal interface IAlgorithmRegistration
{
    string Key { get; }

    Type ContextType { get; }

    Type ResultType { get; }

    object Algorithm { get; }
}

internal sealed class AlgorithmRegistration<TContext, TResult>(string key, IAlgorithm<TContext, TResult> algorithm) : IAlgorithmRegistration
{
    public string Key { get; } = key;

    public Type ContextType => typeof(TContext);

    public Type ResultType => typeof(TResult);

    public object Algorithm => algorithm;
}

internal sealed class KeyedAlgorithm<TContext, TResult>(string key, IAlgorithm<TContext, TResult> inner) : IAlgorithm<TContext, TResult>, ICallsChildren
{
    public string Key { get; } = key;

    public bool CallsChildren { get; } = inner is ICallsChildren calls && calls.CallsChildren;

    public TResult Execute(TContext context) => inner.Execute(context);
}
