using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Algorithms;

/// <summary>Checks syntax and startup shadowing, then writes through <see cref="IAlgorithmDefinitionStore" />.</summary>
public sealed class AlgorithmCatalog : IAlgorithmCatalog
{
    private readonly IExpressionCompiler _compiler;
    private readonly AlgorithmOptions _options;
    private readonly RuntimeAlgorithmStore _runtime;
    private readonly IAlgorithmStartupStore _startup;
    private readonly IAlgorithmDefinitionStore _store;

    /// <summary>Creates a catalog. <paramref name="startup" /> is the frozen store used for the shadow check.</summary>
    public AlgorithmCatalog(IAlgorithmDefinitionStore store, IExpressionCompiler compiler, AlgorithmOptions options, IAlgorithmStartupStore startup, RuntimeAlgorithmStore runtime)
    {
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(compiler);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(startup);
        ArgumentHelpers.ThrowIfNull(runtime);
        _store = store;
        _compiler = compiler;
        _options = options;
        _startup = startup;
        _runtime = runtime;
    }

    /// <inheritdoc />
    public async Task<Result<AlgorithmDefinitionRes>> SaveRevisionAsync(string key, AlgorithmRevisionReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var denied = Deny(key, request);
        if (denied is not null)
            return denied;

        var saved = await _store.SaveRevisionAsync(key, request, ct).ConfigureAwait(false);
        _runtime.NoteLatest(
            saved, new() {
                DefinitionId = saved.Id,
                Revision = saved.CurrentRevision,
                Kind = request.Kind,
                Source = request.Source,
                Payload = request.Payload,
                CreatedTimestamp = saved.UpdatedTimestamp ?? saved.CreatedTimestamp
            });

        return Result<AlgorithmDefinitionRes>.Success(saved);
    }

    /// <inheritdoc />
    public Task<AlgorithmDefinitionRes?> GetByKeyAsync(string key, CancellationToken ct = default) => _store.GetByKeyAsync(key, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<AlgorithmRevisionRes>> GetRevisionsAsync(string key, CancellationToken ct = default) => _store.GetRevisionsAsync(key, ct);

    /// <inheritdoc />
    public async Task<Result<AlgorithmDefinitionRes>> RevertToRevisionAsync(string key, AlgorithmRevertReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        try {
            var saved = await _store.RevertToRevisionAsync(key, request, ct).ConfigureAwait(false);
            var snapshot = await _store.GetRevisionAsync(key, saved.CurrentRevision, ct).ConfigureAwait(false);
            if (snapshot is not null)
                _runtime.NoteLatest(saved, snapshot);

            return Result<AlgorithmDefinitionRes>.Success(saved);
        }
        catch (AlgorithmException ex) {
            return Result<AlgorithmDefinitionRes>.Failure(ex.Message, ex.Code ?? "algorithm.revision.missing");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AlgorithmLabelRes>> SetLabelAsync(string key, string label, AlgorithmLabelReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        try {
            AlgorithmKeys.ThrowIfInvalidKey(key);
            AlgorithmKeys.ThrowIfInvalidLabel(label);
            var saved = await _store.SetLabelAsync(key, label, request, ct).ConfigureAwait(false);
            var snapshot = await _store.GetRevisionAsync(key, saved.Revision, ct).ConfigureAwait(false);
            if (snapshot is not null)
                _runtime.NoteLabel(key, saved, snapshot);

            return Result<AlgorithmLabelRes>.Success(saved);
        }
        catch (AlgorithmException ex) {
            return Result<AlgorithmLabelRes>.Failure(ex.Message, ex.Code ?? "algorithm.label.immutable");
        }
        catch (ArgumentException ex) {
            return Result<AlgorithmLabelRes>.Failure(ex.Message, "algorithm.key.invalid");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> DeleteAsync(string key, CancellationToken ct = default)
    {
        if (!AlgorithmKeys.IsKey(key))
            return ResultVoid.Failure($"Algorithm key '{key}' must match {AlgorithmKeys.KeyPattern}.", "algorithm.key.invalid");

        if (!await _store.DeleteAsync(key, ct).ConfigureAwait(false))
            return ResultVoid.Failure($"Algorithm '{key}' was not found.", "algorithm.definition.missing");

        _runtime.Forget(key);
        return ResultVoid.Success();
    }

    private Result<AlgorithmDefinitionRes>? Deny(string key, AlgorithmRevisionReq request)
    {
        if (!AlgorithmKeys.IsKey(key))
            return Result<AlgorithmDefinitionRes>.Failure($"Algorithm key '{key}' must match {AlgorithmKeys.KeyPattern}.", "algorithm.key.invalid");

        if (!_options.AllowRuntimeOverride && _startup.ContainsKey(key))
            return Result<AlgorithmDefinitionRes>.Failure($"Algorithm '{key}' is a startup algorithm and cannot be replaced from the catalog.", "algorithm.key.shadows");

        if (request.Kind is AlgorithmKind.Expression or AlgorithmKind.Named) {
            if (string.IsNullOrWhiteSpace(request.Source))
                return Result<AlgorithmDefinitionRes>.Failure("Algorithm source is required.", "algorithm.expression.parse");

            AlgorithmTypeBinding binding;
            try {
                binding = AlgorithmTypeBinding.ParsePayload(request.Payload);
            }
            catch (AlgorithmException ex) {
                return Result<AlgorithmDefinitionRes>.Failure(ex.Message, ex.Code ?? "algorithm.payload.invalid");
            }

            if (request.Kind == AlgorithmKind.Expression) {
                var validated = _compiler.Validate(request.Source, binding);
                if (!validated.IsSuccess)
                    return Result<AlgorithmDefinitionRes>.Failure(validated.Errors![0].Message, validated.Errors[0].Code);
            }

            return null;
        }

        var checkedPayload = AlgorithmPayload.Validate(request.Kind, request.Payload);
        return !checkedPayload.IsSuccess ? Result<AlgorithmDefinitionRes>.Failure(checkedPayload.Errors![0].Message, checkedPayload.Errors[0].Code) : null;
    }
}