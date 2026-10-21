using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>Process-local catalog. No Docker. Postgres and SQLite implement the same <see cref="IAlgorithmDefinitionStore" />.</summary>
public sealed class MemoryAlgorithmDefinitionStore : IAlgorithmDefinitionStore
{
    private readonly Dictionary<string, AlgorithmDefinitionRes> _definitions = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly Dictionary<(Guid DefinitionId, string Label), AlgorithmLabelRes> _labels = new();
    private readonly Dictionary<Guid, List<AlgorithmRevisionRes>> _revisions = new();

    /// <summary>How many read methods have run. Tests use this to prove a cache hit skips the store.</summary>
    internal int ReadCount;

    /// <inheritdoc />
    public Task<AlgorithmDefinitionRes> SaveRevisionAsync(string key, AlgorithmRevisionReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        AlgorithmKeys.ThrowIfInvalidKey(key);
        lock (_gate) {
            if (!_definitions.TryGetValue(key, out var definition)) {
                definition = new() { Id = LyoGuid.CreateV7(), Key = key, CreatedTimestamp = DateTime.UtcNow };
                _definitions.Add(key, definition);
                _revisions.Add(definition.Id, []);
            }

            var next = _revisions[definition.Id].Count == 0 ? 1 : _revisions[definition.Id].Max(r => r.Revision) + 1;
            var revision = new AlgorithmRevisionRes {
                DefinitionId = definition.Id,
                Revision = next,
                Kind = request.Kind,
                Source = request.Source,
                Payload = request.Payload,
                CreatedTimestamp = DateTime.UtcNow
            };

            _revisions[definition.Id].Add(revision);
            definition.Kind = request.Kind;
            definition.Name = request.Name ?? definition.Name;
            definition.Description = request.Description ?? definition.Description;
            definition.CurrentRevision = next;
            definition.UpdatedTimestamp = revision.CreatedTimestamp;
            return Task.FromResult(Copy(definition));
        }
    }

    /// <inheritdoc />
    public Task<AlgorithmDefinitionRes?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        lock (_gate) {
            ReadCount++;
            return Task.FromResult(_definitions.TryGetValue(key, out var definition) ? Copy(definition) : null);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AlgorithmDefinitionRes>> ListAsync(CancellationToken ct = default)
    {
        lock (_gate) {
            ReadCount++;
            return Task.FromResult<IReadOnlyList<AlgorithmDefinitionRes>>(_definitions.Values.OrderBy(d => d.Key, StringComparer.Ordinal).Select(Copy).ToArray());
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AlgorithmRevisionRes>> GetRevisionsAsync(string key, CancellationToken ct = default)
    {
        lock (_gate) {
            ReadCount++;
            var definition = Require(key);
            var rows = _revisions[definition.Id].OrderByDescending(r => r.Revision).Select(Copy).ToArray();
            return Task.FromResult<IReadOnlyList<AlgorithmRevisionRes>>(rows);
        }
    }

    /// <inheritdoc />
    public Task<AlgorithmRevisionRes?> GetRevisionAsync(string key, int revision, CancellationToken ct = default)
    {
        lock (_gate) {
            ReadCount++;
            if (!_definitions.TryGetValue(key, out var definition))
                return Task.FromResult<AlgorithmRevisionRes?>(null);

            var row = _revisions[definition.Id].FirstOrDefault(r => r.Revision == revision);
            return Task.FromResult(row is null ? null : Copy(row));
        }
    }

    /// <inheritdoc />
    public async Task<AlgorithmDefinitionRes> RevertToRevisionAsync(string key, AlgorithmRevertReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var existing = AlgorithmHelpers.ThrowIfNull(
            await GetRevisionAsync(key, request.Revision, ct).ConfigureAwait(false), $"Algorithm '{key}' has no revision {request.Revision}.", "algorithm.revision.missing");

        var definition = await GetByKeyAsync(key, ct).ConfigureAwait(false);
        return await SaveRevisionAsync(
                key, new() {
                    Kind = existing.Kind,
                    Source = existing.Source,
                    Payload = existing.Payload,
                    Name = definition?.Name,
                    Description = definition?.Description
                }, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<AlgorithmLabelRes> SetLabelAsync(string key, string label, AlgorithmLabelReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        AlgorithmKeys.ThrowIfInvalidLabel(label);
        lock (_gate) {
            var definition = Require(key);
            AlgorithmHelpers.ThrowIf(
                _revisions[definition.Id].All(r => r.Revision != request.Revision), $"Algorithm '{key}' has no revision {request.Revision}.", "algorithm.revision.missing");

            var mapKey = (definition.Id, label);
            if (_labels.TryGetValue(mapKey, out var existing)) {
                AlgorithmHelpers.ThrowIf(existing.IsMutable != request.IsMutable, $"Label '{label}' on '{key}' mutability cannot change.", "algorithm.label.mutability");
                AlgorithmHelpers.ThrowIf(!existing.IsMutable && existing.Revision != request.Revision, $"Label '{label}' on '{key}' is immutable.", "algorithm.label.immutable");
                existing.Revision = request.Revision;
                return Task.FromResult(Copy(existing));
            }

            var created = new AlgorithmLabelRes {
                DefinitionId = definition.Id,
                Label = label,
                Revision = request.Revision,
                IsMutable = request.IsMutable
            };

            _labels.Add(mapKey, created);
            return Task.FromResult(Copy(created));
        }
    }

    /// <inheritdoc />
    public Task<AlgorithmLabelRes?> GetLabelAsync(string key, string label, CancellationToken ct = default)
    {
        lock (_gate) {
            ReadCount++;
            if (!_definitions.TryGetValue(key, out var definition))
                return Task.FromResult<AlgorithmLabelRes?>(null);

            return Task.FromResult(_labels.TryGetValue((definition.Id, label), out var row) ? Copy(row) : null);
        }
    }

    private AlgorithmDefinitionRes Require(string key)
    {
        _definitions.TryGetValue(key, out var definition);
        return AlgorithmHelpers.ThrowIfNull(definition, $"Algorithm '{key}' was not found.", "algorithm.definition.missing");
    }

    private static AlgorithmDefinitionRes Copy(AlgorithmDefinitionRes source)
        => new() {
            Id = source.Id,
            Key = source.Key,
            Name = source.Name,
            Description = source.Description,
            Kind = source.Kind,
            CurrentRevision = source.CurrentRevision,
            CreatedTimestamp = source.CreatedTimestamp,
            UpdatedTimestamp = source.UpdatedTimestamp
        };

    private static AlgorithmRevisionRes Copy(AlgorithmRevisionRes source)
        => new() {
            DefinitionId = source.DefinitionId,
            Revision = source.Revision,
            Kind = source.Kind,
            Source = source.Source,
            Payload = source.Payload,
            CreatedTimestamp = source.CreatedTimestamp
        };

    private static AlgorithmLabelRes Copy(AlgorithmLabelRes source)
        => new() {
            DefinitionId = source.DefinitionId,
            Label = source.Label,
            Revision = source.Revision,
            IsMutable = source.IsMutable
        };
}