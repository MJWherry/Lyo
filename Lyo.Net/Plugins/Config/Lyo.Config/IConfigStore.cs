using Lyo.EntityReference.Models;

namespace Lyo.Config;

/// <summary>Contract for managing config definitions and per-entity bindings.</summary>
public interface IConfigStore
{
    /// <summary>Upserts a config definition for an entity type.</summary>
    Task SaveDefinitionAsync(ConfigDefinitionRecord definition, CancellationToken ct = default);

    /// <summary>Looks up a definition by id.</summary>
    Task<ConfigDefinitionRecord?> GetDefinitionByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a definition by entity type and key.</summary>
    Task<ConfigDefinitionRecord?> GetDefinitionAsync(string forEntityType, string key, CancellationToken ct = default);

    /// <summary>Lists all definitions for an entity type.</summary>
    Task<IReadOnlyList<ConfigDefinitionRecord>> GetDefinitionsAsync(string forEntityType, CancellationToken ct = default);

    /// <summary>Removes a definition by id. Deletes all bindings for that definition (PostgreSQL: ON DELETE CASCADE on <c>config_binding.definition_id</c>).</summary>
    Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts an entity-specific binding.</summary>
    Task SaveBindingAsync(ConfigBindingRecord binding, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Looks up a binding by id.</summary>
    Task<ConfigBindingRecord?> GetBindingByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Returns a binding by target entity and key.</summary>
    Task<ConfigBindingRecord?> GetBindingAsync(EntityRef forEntity, string key, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Lists all bindings for an entity.</summary>
    Task<IReadOnlyList<ConfigBindingRecord>> GetBindingsAsync(EntityRef forEntity, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Removes a binding by id. Fails when the definition is <see cref="ConfigDefinitionRecord.IsRequired" /> and has no default (a binding must remain).</summary>
    Task DeleteBindingAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Removes every bindings for an entity. Fails if any binding removed is for a required definition without a default.</summary>
    Task DeleteBindingsAsync(EntityRef forEntity, Guid? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Reads the resolved config for an entity, merging definitions with bindings and defaults. Throws when a definition has <see cref="ConfigDefinitionRecord.IsRequired" /> and
    /// nothing resolved (no binding and no default).
    /// </summary>
    Task<ResolvedConfigRecord> LoadConfigAsync(EntityRef forEntity, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Returns value revisions for a binding, newest first.</summary>
    Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(Guid bindingId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Returns value revisions for the binding matching <paramref name="forEntity" /> and <paramref name="key" />, newest first.</summary>
    Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(EntityRef forEntity, string key, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Returns a single revision by number, or null if missing.</summary>
    Task<ConfigBindingRevisionRecord?> GetBindingRevisionAsync(Guid bindingId, int revision, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Assigns the binding&apos;s current value to the snapshot at <paramref name="revision" /> and appends a new revision row (so history stays linear).</summary>
    Task RevertBindingToRevisionAsync(Guid bindingId, int revision, Guid? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Equivalent to <see cref="RevertBindingToRevisionAsync(System.Guid,int,System.Guid?,System.Threading.CancellationToken)" /> for the binding looked up from
    /// <paramref name="forEntity" /> plus <paramref name="key" />.
    /// </summary>
    Task RevertBindingToRevisionAsync(EntityRef forEntity, string key, int revision, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Returns definition metadata revisions, newest first.</summary>
    Task<IReadOnlyList<ConfigDefinitionRevisionRecord>> GetDefinitionRevisionsAsync(Guid definitionId, CancellationToken ct = default);

    /// <summary>Loads a single definition revision by number, or null if missing.</summary>
    Task<ConfigDefinitionRevisionRecord?> GetDefinitionRevisionAsync(Guid definitionId, int revision, CancellationToken ct = default);

    /// <summary>Assigns the definition to the snapshot at <paramref name="revision" /> and appends a new revision row (so history stays linear).</summary>
    Task RevertDefinitionToRevisionAsync(Guid definitionId, int revision, CancellationToken ct = default);
}