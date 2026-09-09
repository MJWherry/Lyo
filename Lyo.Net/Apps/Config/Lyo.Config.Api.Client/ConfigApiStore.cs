using Lyo.Api.Client;
using Lyo.Config;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Config.Api.Client;

/// <summary>
/// <see cref="IConfigStore" /> that talks to Config.Api / TestApi manage routes. Posts plaintext and <see cref="ConfigDefinitionRecord.IsEncrypted" />; the API encrypts at rest.
/// The server picks tenancy, so the <c>tenantId</c> argument on binding methods is ignored.
/// </summary>
public sealed class ConfigApiStore : IConfigStore
{
    private readonly IApiClient _client;

    /// <summary>Builds a store that hits manage routes through <paramref name="client" /> (usually TestApi or Config.Api).</summary>
    public ConfigApiStore(IApiClient client)
    {
        ArgumentHelpers.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task SaveDefinitionAsync(ConfigDefinitionRecord definition, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(definition);
        var saved = await _client.PutAsAsync<ConfigDefinitionRecord, ConfigDefinitionRecord>(ConfigApiRoutes.ManageDefinitions, definition, ct: ct).ConfigureAwait(false);
        definition.Id = saved.Id;
        definition.CreatedTimestamp = saved.CreatedTimestamp;
        definition.UpdatedTimestamp = saved.UpdatedTimestamp;
    }

    /// <inheritdoc />
    public Task<ConfigDefinitionRecord?> GetDefinitionByIdAsync(Guid id, CancellationToken ct = default)
        => GetOrNullAsync<ConfigDefinitionRecord>(ConfigApiRoutes.Definition(id), ct);

    /// <inheritdoc />
    public Task<ConfigDefinitionRecord?> GetDefinitionAsync(string forEntityType, string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        return GetOrNullAsync<ConfigDefinitionRecord>(ConfigApiRoutes.DefinitionByKey(forEntityType, key), ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigDefinitionRecord>> GetDefinitionsAsync(string forEntityType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        var list = await _client.GetAsAsync<List<ConfigDefinitionRecord>>(ConfigApiRoutes.DefinitionsFor(forEntityType), ct: ct).ConfigureAwait(false);
        return list ?? [];
    }

    /// <inheritdoc />
    public Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default)
        => _client.DeleteAsAsync<object>(ConfigApiRoutes.Definition(id), ct: ct);

    /// <inheritdoc />
    public async Task SaveBindingAsync(ConfigBindingRecord binding, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(binding);
        _ = tenantId;
        var saved = await _client.PutAsAsync<ConfigBindingRecord, ConfigBindingRecord>(ConfigApiRoutes.ManageBindings, binding, ct: ct).ConfigureAwait(false);
        binding.Id = saved.Id;
        binding.DefinitionId = saved.DefinitionId;
        binding.Key = saved.Key;
        binding.CreatedTimestamp = saved.CreatedTimestamp;
        binding.UpdatedTimestamp = saved.UpdatedTimestamp;
    }

    /// <inheritdoc />
    public Task<ConfigBindingRecord?> GetBindingByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        _ = tenantId;
        return GetOrNullAsync<ConfigBindingRecord>(ConfigApiRoutes.Binding(id), ct);
    }

    /// <inheritdoc />
    public Task<ConfigBindingRecord?> GetBindingAsync(EntityRef forEntity, string key, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _ = tenantId;
        return GetOrNullAsync<ConfigBindingRecord>(ConfigApiRoutes.BindingByKey(forEntity.EntityType, forEntity.EntityId, key), ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigBindingRecord>> GetBindingsAsync(EntityRef forEntity, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        _ = tenantId;
        var list = await _client.GetAsAsync<List<ConfigBindingRecord>>(ConfigApiRoutes.BindingsFor(forEntity.EntityType, forEntity.EntityId), ct: ct).ConfigureAwait(false);
        return list ?? [];
    }

    /// <inheritdoc />
    public Task DeleteBindingAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        _ = tenantId;
        return _client.DeleteAsAsync<object>(ConfigApiRoutes.Binding(id), ct: ct);
    }

    /// <inheritdoc />
    public Task DeleteBindingsAsync(EntityRef forEntity, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        _ = tenantId;
        return _client.DeleteAsAsync<object>(ConfigApiRoutes.BindingsFor(forEntity.EntityType, forEntity.EntityId), ct: ct);
    }

    /// <inheritdoc />
    public async Task<ResolvedConfigRecord> LoadConfigAsync(EntityRef forEntity, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        _ = tenantId;
        var resolved = await _client.GetAsAsync<ResolvedConfigRecord>(ConfigApiRoutes.Resolved(forEntity.EntityType, forEntity.EntityId), ct: ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(resolved, "Resolved config deserialization returned null.");
        return resolved;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(Guid bindingId, Guid? tenantId, CancellationToken ct = default)
    {
        _ = tenantId;
        var list = await _client.GetAsAsync<List<ConfigBindingRevisionRecord>>(ConfigApiRoutes.BindingRevisions(bindingId), ct: ct).ConfigureAwait(false);
        return list ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(EntityRef forEntity, string key, Guid? tenantId, CancellationToken ct = default)
    {
        var binding = await GetBindingAsync(forEntity, key, tenantId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(binding, $"No binding for entity type '{forEntity.EntityType}' id '{forEntity.EntityId}' and key '{key}'.");
        return await GetBindingRevisionsAsync(binding.Id, tenantId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ConfigBindingRevisionRecord?> GetBindingRevisionAsync(Guid bindingId, int revision, Guid? tenantId, CancellationToken ct = default)
    {
        _ = tenantId;
        return GetOrNullAsync<ConfigBindingRevisionRecord>(ConfigApiRoutes.BindingRevision(bindingId, revision), ct);
    }

    /// <inheritdoc />
    public Task RevertBindingToRevisionAsync(Guid bindingId, int revision, Guid? tenantId, CancellationToken ct = default)
    {
        _ = tenantId;
        return _client.PostAsAsync<ConfigRevertRevisionRequest, ConfigRevertRevisionRequest>(ConfigApiRoutes.BindingRevert(bindingId), new(revision), ct: ct);
    }

    /// <inheritdoc />
    public async Task RevertBindingToRevisionAsync(EntityRef forEntity, string key, int revision, Guid? tenantId, CancellationToken ct = default)
    {
        var binding = await GetBindingAsync(forEntity, key, tenantId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(binding, $"No binding for entity type '{forEntity.EntityType}' id '{forEntity.EntityId}' and key '{key}'.");
        await RevertBindingToRevisionAsync(binding.Id, revision, tenantId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigDefinitionRevisionRecord>> GetDefinitionRevisionsAsync(Guid definitionId, CancellationToken ct = default)
    {
        var list = await _client.GetAsAsync<List<ConfigDefinitionRevisionRecord>>(ConfigApiRoutes.DefinitionRevisions(definitionId), ct: ct).ConfigureAwait(false);
        return list ?? [];
    }

    /// <inheritdoc />
    public Task<ConfigDefinitionRevisionRecord?> GetDefinitionRevisionAsync(Guid definitionId, int revision, CancellationToken ct = default)
        => GetOrNullAsync<ConfigDefinitionRevisionRecord>(ConfigApiRoutes.DefinitionRevision(definitionId, revision), ct);

    /// <inheritdoc />
    public Task RevertDefinitionToRevisionAsync(Guid definitionId, int revision, CancellationToken ct = default)
        => _client.PostAsAsync<ConfigRevertRevisionRequest, ConfigRevertRevisionRequest>(ConfigApiRoutes.DefinitionRevert(definitionId), new(revision), ct: ct);

    private async Task<T?> GetOrNullAsync<T>(string uri, CancellationToken ct)
        where T : class
    {
        try {
            return await _client.GetAsAsync<T>(uri, ct: ct).ConfigureAwait(false);
        }
        catch (HttpException ex) when (ex.StatusCode == 404) {
            return null;
        }
    }
}
