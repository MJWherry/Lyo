using Lyo.Exceptions;

namespace Lyo.Seed;

/// <summary>
/// Ordered generation graph. <see cref="Entity{T}(int, Func{int, T})"/> factories return each item. The engine never references Bogus.
/// Child <see cref="ForEach{TParent, TChild}"/> factories get the parent so they can fill foreign keys.
/// </summary>
public sealed class SeedGraph
{
    private readonly List<ISeedStep> _steps = [];
    private Func<ISeedTransport, CancellationToken, Task<bool>>? _skipWhen;
    private Func<ISeedTransport, CancellationToken, Task<bool>>? _defaultOccupied;
    private Func<SeedSession, CancellationToken, Task>? _onClear;

    /// <summary>CLR type of the first <see cref="Entity{T}(int, Func{int, T})"/> registration, used for the default skip-if-not-empty check.</summary>
    internal Type? RootType { get; private set; }

    internal Func<ISeedTransport, CancellationToken, Task<bool>>? SkipWhenCallback => _skipWhen ?? _defaultOccupied;

    internal Func<SeedSession, CancellationToken, Task>? OnClearCallback => _onClear;

    /// <summary>Builds <paramref name="count"/> items with <paramref name="create"/> (0-based index) and writes them as one batch.</summary>
    public SeedEntity<T> Entity<T>(int count, Func<int, T> create)
        where T : class
    {
        ArgumentHelpers.ThrowIfNull(create);
        if (count < 0)
            throw new SeedException($"{nameof(count)} must be >= 0.");

        var step = new EntityStep<T>(count, create);
        AddEntityStep<T>(step);
        return new(this, step);
    }

    /// <summary>Writes an already-built list of items.</summary>
    public SeedEntity<T> Entity<T>(IEnumerable<T> items)
        where T : class
    {
        ArgumentHelpers.ThrowIfNull(items);
        var list = items as IReadOnlyList<T> ?? items.ToArray();
        var step = new EntityStep<T>(list);
        AddEntityStep<T>(step);
        return new(this, step);
    }

    /// <summary>
    /// After each generated <typeparamref name="TParent"/>, builds N children. A prior <see cref="Entity{T}(int, Func{int, T})"/> or <c>ForEach</c> for
    /// <typeparamref name="TParent"/> is required.
    /// </summary>
    public SeedEntity<TChild> ForEach<TParent, TChild>(Func<TParent, int> count, Func<TParent, int, TChild> create)
        where TParent : class
        where TChild : class
    {
        ArgumentHelpers.ThrowIfNull(count);
        ArgumentHelpers.ThrowIfNull(create);
        var parent = FindParentStep<TParent>()
            ?? throw new SeedException($"No parent set registered for {typeof(TParent).Name}. Call Entity<{typeof(TParent).Name}> first.");
        var child = parent.AddChild(count, create);
        return new(this, child);
    }

    /// <summary>Runs after earlier entity batches have been written. Use <see cref="SeedSession.Generated{T}"/> and <see cref="SeedSession.PersistAsync{T}"/>.</summary>
    public SeedGraph After(Func<SeedSession, CancellationToken, Task> step)
    {
        ArgumentHelpers.ThrowIfNull(step);
        _steps.Add(new AfterStep(step));
        return this;
    }

    /// <summary>Replaces the skip-if-not-empty check. Return true when the destination should be treated as occupied.</summary>
    public SeedGraph SkipWhen(Func<ISeedTransport, CancellationToken, Task<bool>> isOccupied)
    {
        ArgumentHelpers.ThrowIfNull(isOccupied);
        _skipWhen = isOccupied;
        return this;
    }

    /// <summary>Required for <see cref="SeedConflictMode.Replace"/>. Delete existing rows in a schema-safe order.</summary>
    public SeedGraph OnClear(Func<SeedSession, CancellationToken, Task> clear)
    {
        ArgumentHelpers.ThrowIfNull(clear);
        _onClear = clear;
        return this;
    }

    internal async Task ExecuteAsync(SeedSession session, CancellationToken ct)
    {
        foreach (var step in _steps)
            await step.ExecuteAsync(session, ct).ConfigureAwait(false);
    }

    private void AddEntityStep<T>(ISeedStep step)
        where T : class
    {
        if (RootType == null) {
            RootType = typeof(T);
            _defaultOccupied = async (t, ct) => !await t.IsEmptyAsync<T>(ct).ConfigureAwait(false);
        }

        _steps.Add(step);
    }

    private IParentStep? FindParentStep<TParent>()
        where TParent : class
    {
        for (var i = _steps.Count - 1; i >= 0; i--) {
            if (_steps[i] is IParentStep parent && parent.ItemType == typeof(TParent))
                return parent;
            if (_steps[i] is IParentStep nested) {
                var found = nested.FindDescendant(typeof(TParent));
                if (found != null)
                    return found;
            }
        }

        return null;
    }
}

/// <summary>Fluent handle for one generated set. Sibling <see cref="ForEach{TChild}"/> calls stay here; chaining <c>ForEach</c> nests under the child type.</summary>
public sealed class SeedEntity<T>
    where T : class
{
    private readonly SeedGraph _graph;
    private readonly IParentStep _step;

    internal SeedEntity(SeedGraph graph, IParentStep step)
    {
        _graph = graph;
        _step = step;
    }

    /// <summary>For each item in this set, build N children and write them after the parent batch.</summary>
    public SeedEntity<TChild> ForEach<TChild>(Func<T, int> count, Func<T, int, TChild> create)
        where TChild : class
    {
        ArgumentHelpers.ThrowIfNull(count);
        ArgumentHelpers.ThrowIfNull(create);
        var child = _step.AddChild(count, create);
        return new(_graph, child);
    }

    /// <summary>Adds an <see cref="SeedGraph.After"/> callback on the parent graph.</summary>
    public SeedGraph After(Func<SeedSession, CancellationToken, Task> step) => _graph.After(step);
}

internal interface ISeedStep
{
    Task ExecuteAsync(SeedSession session, CancellationToken ct);
}

internal interface IParentStep : ISeedStep
{
    Type ItemType { get; }

    IParentStep AddChild<TParent, TChild>(Func<TParent, int> count, Func<TParent, int, TChild> create)
        where TParent : class
        where TChild : class;

    IParentStep? FindDescendant(Type type);
}

internal sealed class EntityStep<T> : IParentStep
    where T : class
{
    private readonly int _count;
    private readonly Func<int, T>? _create;
    private readonly IReadOnlyList<T>? _items;
    private readonly List<IParentStep> _children = [];

    public EntityStep(int count, Func<int, T> create)
    {
        _count = count;
        _create = create;
    }

    public EntityStep(IReadOnlyList<T> items) => _items = items;

    public Type ItemType => typeof(T);

    public IParentStep AddChild<TParent, TChild>(Func<TParent, int> count, Func<TParent, int, TChild> create)
        where TParent : class
        where TChild : class
    {
        if (typeof(TParent) != typeof(T))
            throw new SeedException($"Child parent type {typeof(TParent).Name} does not match {typeof(T).Name}.");

        var typedCount = (Func<T, int>)(object)count;
        var typedCreate = (Func<T, int, TChild>)(object)create;
        var child = new ForEachStep<T, TChild>(typedCount, typedCreate);
        _children.Add(child);
        return child;
    }

    public IParentStep? FindDescendant(Type type)
    {
        foreach (var child in _children) {
            if (child.ItemType == type)
                return child;
            var nested = child.FindDescendant(type);
            if (nested != null)
                return nested;
        }

        return null;
    }

    public async Task ExecuteAsync(SeedSession session, CancellationToken ct)
    {
        IReadOnlyList<T> generated;
        if (_items != null)
            generated = _items;
        else {
            ArgumentHelpers.ThrowIfNull(_create);
            var list = new List<T>(_count);
            for (var i = 0; i < _count; i++)
                list.Add(_create(i));
            generated = list;
        }

        await session.PersistAsync(generated, ct).ConfigureAwait(false);
        foreach (var child in _children)
            await child.ExecuteAsync(session, ct).ConfigureAwait(false);
    }
}

internal sealed class ForEachStep<TParent, TChild> : IParentStep
    where TParent : class
    where TChild : class
{
    private readonly Func<TParent, int> _count;
    private readonly Func<TParent, int, TChild> _create;
    private readonly List<IParentStep> _children = [];

    public ForEachStep(Func<TParent, int> count, Func<TParent, int, TChild> create)
    {
        _count = count;
        _create = create;
    }

    public Type ItemType => typeof(TChild);

    public IParentStep AddChild<TNestedParent, TNestedChild>(Func<TNestedParent, int> count, Func<TNestedParent, int, TNestedChild> create)
        where TNestedParent : class
        where TNestedChild : class
    {
        if (typeof(TNestedParent) != typeof(TChild))
            throw new SeedException($"Child parent type {typeof(TNestedParent).Name} does not match {typeof(TChild).Name}.");

        var typedCount = (Func<TChild, int>)(object)count;
        var typedCreate = (Func<TChild, int, TNestedChild>)(object)create;
        var child = new ForEachStep<TChild, TNestedChild>(typedCount, typedCreate);
        _children.Add(child);
        return child;
    }

    public IParentStep? FindDescendant(Type type)
    {
        foreach (var child in _children) {
            if (child.ItemType == type)
                return child;
            var nested = child.FindDescendant(type);
            if (nested != null)
                return nested;
        }

        return null;
    }

    public async Task ExecuteAsync(SeedSession session, CancellationToken ct)
    {
        var parents = session.Generated<TParent>();
        var children = new List<TChild>();
        foreach (var parent in parents) {
            var n = _count(parent);
            if (n < 0)
                throw new SeedException($"ForEach count for {typeof(TChild).Name} must be >= 0.");

            for (var i = 0; i < n; i++)
                children.Add(_create(parent, i));
        }

        await session.PersistAsync(children, ct).ConfigureAwait(false);
        foreach (var child in _children)
            await child.ExecuteAsync(session, ct).ConfigureAwait(false);
    }
}

internal sealed class AfterStep(Func<SeedSession, CancellationToken, Task> callback) : ISeedStep
{
    public Task ExecuteAsync(SeedSession session, CancellationToken ct) => callback(session, ct);
}
