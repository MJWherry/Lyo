using System.Collections.Concurrent;

namespace Lyo.Algorithms;

/// <summary>
/// Resolves catalog revisions and publishes an immutable slot. A compiled latest or label delegate is invoked with <see cref="Volatile.Read{T}(ref T)" /> and does not open
/// the store.
/// </summary>
public sealed class RuntimeAlgorithmStore : IAlgorithmRuntimeStore
{
    [ThreadStatic]
    private static HashSet<string>? _warming;

    private readonly IExpressionCompiler _compiler;
    private readonly ConcurrentDictionary<string, SlotHolder> _labels = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SlotHolder> _latest = new(StringComparer.Ordinal);
    private readonly AlgorithmOptions _options;
    private readonly ConcurrentDictionary<PinKey, object> _pins = new();
    private readonly IAlgorithmStartupStore _startup;
    private readonly IAlgorithmDefinitionStore? _store;

    /// <summary>Creates a runtime store. <paramref name="store" /> null means every resolve misses (startup-only hosts).</summary>
    public RuntimeAlgorithmStore(IExpressionCompiler compiler, IAlgorithmStartupStore startup, AlgorithmOptions options, IAlgorithmDefinitionStore? store = null)
    {
        _compiler = compiler;
        _startup = startup;
        _options = options;
        _store = store;
    }

    /// <inheritdoc />
    public bool TryExecute<TContext, TResult>(AlgorithmRef reference, TContext context, out TResult result)
    {
        result = default!;
        var algorithm = FindCompiled<TContext, TResult>(reference);
        if (algorithm is null)
            return false;

        result = algorithm.Execute(context);
        return true;
    }

    /// <inheritdoc />
    public async Task<IAlgorithm<TContext, TResult>?> ResolveAsync<TContext, TResult>(AlgorithmRef reference, CancellationToken ct = default)
    {
        if (FindCompiled<TContext, TResult>(reference) is { } ready)
            return ready;

        if (_store is null)
            return null;

        if (!_options.AllowRuntimeOverride && _startup.ContainsKey(reference.Key))
            return null;

        var holder = Holder(reference);
        var published = holder?.Read();
        if (published is not null)
            return await CompileWarm<TContext, TResult>(published, reference, ct).ConfigureAwait(false);

        var definition = await _store.GetByKeyAsync(reference.Key, ct).ConfigureAwait(false);
        if (definition is null)
            return null;

        var selected = await SelectAsync(definition, reference, ct).ConfigureAwait(false);
        var revision = AlgorithmHelpers.ThrowIfNull(selected.Revision, $"Algorithm '{reference.Key}' has no revision for {Describe(reference)}.");
        var body = new Body(definition.Id, definition.Key, revision, selected.LastGood, null);
        if (reference.Address == AlgorithmAddress.Revision)
            return await CompileWarm<TContext, TResult>(body, reference, ct).ConfigureAwait(false);

        var slot = holder ?? Holder(reference, true)!;
        var raced = slot.Read();
        if (raced is not null && raced.Revision.Revision == body.Revision.Revision)
            return await CompileWarm<TContext, TResult>(raced, reference, ct).ConfigureAwait(false);

        slot.Publish(body);
        return await CompileWarm<TContext, TResult>(body, reference, ct).ConfigureAwait(false);
    }

    private async Task<IAlgorithm<TContext, TResult>> CompileWarm<TContext, TResult>(Body body, AlgorithmRef reference, CancellationToken ct)
    {
        AlgorithmHelpers.ThrowIf(!EnterWarm(body.Key), $"Algorithm '{body.Key}' calls itself.");
        try {
            await WarmAsync<TContext>(body.Revision, ct).ConfigureAwait(false);
            return CompileInto<TContext, TResult>(body, reference);
        }
        finally {
            LeaveWarm(body.Key);
        }
    }

    private async Task WarmAsync<TContext>(AlgorithmRevisionRes revision, CancellationToken ct)
    {
        foreach (var child in ChildKeys(revision))
            await ResolveAsync<TContext, double>(AlgorithmRef.Latest(child), ct).ConfigureAwait(false);
    }

    private static IEnumerable<string> ChildKeys(AlgorithmRevisionRes revision)
    {
        if (revision.Kind == AlgorithmKind.Expression)
            return AlgorithmCalls.Keys(revision.Source);

        if (revision.Kind != AlgorithmKind.Composite || string.IsNullOrWhiteSpace(revision.Payload))
            return [];

        var composite = AlgorithmPayload.ParseComposite(revision.Payload!);
        var keys = new List<string>();
        foreach (var child in composite.Children) {
            if (!string.IsNullOrWhiteSpace(child.Ref))
                keys.Add(child.Ref!);
        }

        if (!string.IsNullOrWhiteSpace(composite.Formula))
            keys.AddRange(AlgorithmCalls.Keys(composite.Formula));

        return keys;
    }

    private double InvokeChild<TContext>(TContext context, string childKey)
    {
        AlgorithmHelpers.ThrowIf(!TryExecute<TContext, double>(AlgorithmRef.Latest(childKey), context, out var value), $"Algorithm '{childKey}' is not compiled.");
        return value;
    }

    private static bool EnterWarm(string key)
    {
        _warming ??= new(StringComparer.Ordinal);
        return _warming.Add(key);
    }

    private static void LeaveWarm(string key) => _warming?.Remove(key);

    /// <summary>Publishes the latest slot for a revision saved in this process. The next execute compiles from this body.</summary>
    internal void NoteLatest(AlgorithmDefinitionRes definition, AlgorithmRevisionRes revision)
    {
        var holder = _latest.GetOrAdd(definition.Key, static _ => new());
        holder.Publish(new(definition.Id, definition.Key, revision, true, LastGoodOf(holder.Read())));
    }

    /// <summary>Publishes a label slot. Mutable labels keep the previous delegate when the new body fails to compile.</summary>
    internal void NoteLabel(string key, AlgorithmLabelRes label, AlgorithmRevisionRes revision)
    {
        var holder = _labels.GetOrAdd(LabelKey(key, label.Label), static _ => new());
        holder.Publish(new(revision.DefinitionId, key, revision, label.IsMutable, LastGoodOf(holder.Read())));
    }

    private static ConcurrentDictionary<TypePair, object>? LastGoodOf(Body? previous)
    {
        if (previous is null)
            return null;

        return previous.Compiled.IsEmpty ? previous.LastGoodDelegates : previous.Compiled;
    }

    private IAlgorithm<TContext, TResult>? FindCompiled<TContext, TResult>(AlgorithmRef reference)
    {
        if (reference.Address == AlgorithmAddress.Revision) {
            if (_pins.TryGetValue(new(reference.Key, reference.RevisionNumber!.Value, typeof(TContext), typeof(TResult)), out var pinned))
                return (IAlgorithm<TContext, TResult>)pinned;

            return null;
        }

        var body = Holder(reference)?.Read();
        if (body is null)
            return null;

        return body.Find<TContext, TResult>();
    }

    private IAlgorithm<TContext, TResult> CompileInto<TContext, TResult>(Body body, AlgorithmRef reference)
    {
        if (body.Find<TContext, TResult>() is { } ready)
            return ready;

        var pair = new TypePair(typeof(TContext), typeof(TResult));
        if (body.Failed.ContainsKey(pair))
            return LastGoodOrThrow<TContext, TResult>(body, pair);

        IAlgorithm<TContext, TResult> compiled;
        if (body.Revision.Kind == AlgorithmKind.Named) {
            _startup.TryGet<TContext, TResult>(body.Revision.Source, out var named);
            compiled = new KeyedAlgorithm<TContext, TResult>(
                body.Key, AlgorithmHelpers.ThrowIfNull(named, $"Catalog algorithm names startup key '{body.Revision.Source}', which is not registered."));
        }
        else if (body.Revision.Kind == AlgorithmKind.Expression) {
            var result = _compiler.Compile<TContext, TResult>(body.Revision.Source);
            if (!result.IsSuccess || result.Data is null) {
                body.Failed.TryAdd(pair, 0);
                return LastGoodOrThrow<TContext, TResult>(body, pair, result.Errors?[0].Message);
            }

            compiled = new KeyedAlgorithm<TContext, TResult>(body.Key, result.Data);
        }
        else {
            try {
                compiled = AlgorithmScorer.Compile<TContext, TResult>(body.Key, body.Revision.Kind, body.Revision.Payload ?? string.Empty, _compiler, InvokeChild);
            }
            catch (AlgorithmException ex) {
                body.Failed.TryAdd(pair, 0);
                return LastGoodOrThrow<TContext, TResult>(body, pair, ex.Message);
            }
        }

        compiled = new HostedAlgorithm<TContext, TResult>(body.Key, body.Revision.Kind, compiled, _options.MaxChainDepth, this);
        body.Compiled[pair] = compiled;
        if (reference.Address == AlgorithmAddress.Revision)
            _pins.TryAdd(new(reference.Key, body.Revision.Revision, typeof(TContext), typeof(TResult)), compiled);

        return compiled;
    }

    private static IAlgorithm<TContext, TResult> LastGoodOrThrow<TContext, TResult>(Body body, TypePair pair, string? message = null)
    {
        if (body.LastGood && body.LastGoodDelegates is not null && body.LastGoodDelegates.TryGetValue(pair, out var previous)) {
            var typed = (IAlgorithm<TContext, TResult>)previous;
            body.Compiled[pair] = typed;
            return typed;
        }

        return AlgorithmHelpers.Throw<IAlgorithm<TContext, TResult>>(message ?? $"Algorithm revision {body.Revision.Revision} failed to compile.");
    }

    private async Task<(AlgorithmRevisionRes? Revision, bool LastGood)> SelectAsync(AlgorithmDefinitionRes definition, AlgorithmRef reference, CancellationToken ct)
    {
        switch (reference.Address) {
            case AlgorithmAddress.Latest:
                return (await _store!.GetRevisionAsync(definition.Key, definition.CurrentRevision, ct).ConfigureAwait(false), true);
            case AlgorithmAddress.Revision:
                return (await _store!.GetRevisionAsync(definition.Key, reference.RevisionNumber!.Value, ct).ConfigureAwait(false), false);
            case AlgorithmAddress.Label:
                var label = await _store!.GetLabelAsync(definition.Key, reference.LabelName!, ct).ConfigureAwait(false);
                if (label is null)
                    return (null, false);

                var revision = await _store.GetRevisionAsync(definition.Key, label.Revision, ct).ConfigureAwait(false);
                return (revision, label.IsMutable);
            default:
                return (null, false);
        }
    }

    private SlotHolder? Holder(AlgorithmRef reference, bool create = false)
    {
        if (reference.Address == AlgorithmAddress.Revision)
            return null;

        var map = reference.Address == AlgorithmAddress.Label ? _labels : _latest;
        var key = reference.Address == AlgorithmAddress.Label ? LabelKey(reference.Key, reference.LabelName!) : reference.Key;
        if (create)
            return map.GetOrAdd(key, static _ => new());

        return map.TryGetValue(key, out var holder) ? holder : null;
    }

    private static string LabelKey(string key, string label) => key + "\n" + label;

    private static string Describe(AlgorithmRef reference)
        => reference.Address switch {
            AlgorithmAddress.Revision => $"revision {reference.RevisionNumber}",
            AlgorithmAddress.Label => $"label '{reference.LabelName}'",
            var _ => "latest"
        };

    private readonly record struct TypePair(Type Context, Type Result);

    private readonly record struct PinKey(string Key, int Revision, Type Context, Type Result);

    private sealed class HostedAlgorithm<TContext, TResult>(string key, AlgorithmKind kind, IAlgorithm<TContext, TResult> inner, int maxDepth, RuntimeAlgorithmStore store)
        : IAlgorithm<TContext, TResult>
    {
        private readonly bool _callsChildren = (inner is ICallsChildren calls && calls.CallsChildren) || kind == AlgorithmKind.Composite;

        public string Key { get; } = key;

        public TResult Execute(TContext context)
        {
            if (!_callsChildren && !AlgorithmTrace.IsActive && !AlgorithmCallScope.HasFrame)
                return inner.Execute(context);

            var began = AlgorithmCallScope.Begin(maxDepth);
            if (began)
                AlgorithmCallScope.SetInvoker(child => store.InvokeChild(context, child));

            AlgorithmCallScope.Enter(Key);
            try {
                if (kind is AlgorithmKind.Expression or AlgorithmKind.Named)
                    return AlgorithmTrace.Run(Key, kind, null, () => inner.Execute(context), AlgorithmCalls.AsDouble);

                return inner.Execute(context);
            }
            finally {
                AlgorithmCallScope.Exit(Key);
                if (began)
                    AlgorithmCallScope.End();
            }
        }
    }

    private sealed class SlotHolder
    {
        private Body? _body;

        public Body? Read() => Volatile.Read(ref _body);

        public void Publish(Body body) => Volatile.Write(ref _body, body);
    }

    private sealed class Body
    {
        public Guid DefinitionId { get; }

        public string Key { get; }

        public AlgorithmRevisionRes Revision { get; }

        public bool LastGood { get; }

        public ConcurrentDictionary<TypePair, object>? LastGoodDelegates { get; }

        public ConcurrentDictionary<TypePair, object> Compiled { get; } = new();

        public ConcurrentDictionary<TypePair, byte> Failed { get; } = new();

        public Body(Guid definitionId, string key, AlgorithmRevisionRes revision, bool lastGood, ConcurrentDictionary<TypePair, object>? lastGoodDelegates)
        {
            DefinitionId = definitionId;
            Key = key;
            Revision = revision;
            LastGood = lastGood;
            LastGoodDelegates = lastGoodDelegates;
        }

        public IAlgorithm<TContext, TResult>? Find<TContext, TResult>()
        {
            if (Compiled.TryGetValue(new(typeof(TContext), typeof(TResult)), out var found))
                return (IAlgorithm<TContext, TResult>)found;

            return null;
        }
    }
}