using Lyo.Common;

namespace Lyo.Config;

/// <summary>Interface for managing config definitions and per-entity bindings.</summary>
public interface IConfigStore
{
    /// <summary>Adds or updates a config definition for an entity type.</summary>
    Task SaveDefinitionAsync(ConfigDefinitionRecord definition, CancellationToken ct = default);

    /// <summary>Gets a definition by id.</summary>
    Task<ConfigDefinitionRecord?> GetDefinitionByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets a definition by entity type and key.</summary>
    Task<ConfigDefinitionRecord?> GetDefinitionAsync(string forEntityType, string key, CancellationToken ct = default);

    /// <summary>Gets all definitions for an entity type.</summary>
    Task<IReadOnlyList<ConfigDefinitionRecord>> GetDefinitionsAsync(string forEntityType, CancellationToken ct = default);

    /// <summary>Deletes a definition by id. Deletes all bindings for that definition (PostgreSQL: ON DELETE CASCADE on <c>config_binding.definition_id</c>).</summary>
    Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default);

    /// <summary>Adds or updates an entity-specific binding.</summary>
    Task SaveBindingAsync(ConfigBindingRecord binding, CancellationToken ct = default);

    /// <summary>Gets a binding by id.</summary>
    Task<ConfigBindingRecord?> GetBindingByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets a binding by target entity and key.</summary>
    Task<ConfigBindingRecord?> GetBindingAsync(EntityRef forEntity, string key, CancellationToken ct = default);

    /// <summary>Gets all bindings for an entity.</summary>
    Task<IReadOnlyList<ConfigBindingRecord>> GetBindingsAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Deletes a binding by id. Fails when the definition is <see cref="ConfigDefinitionRecord.IsRequired" /> and has no default (a binding must remain).</summary>
    Task DeleteBindingAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deletes all bindings for an entity. Fails if any binding removed is for a required definition without a default.</summary>
    Task DeleteBindingsAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>
    /// Loads the resolved config for an entity, merging definitions with bindings and defaults. Throws when a definition has <see cref="ConfigDefinitionRecord.IsRequired" /> and
    /// the resolved value is missing (no binding and no default).
    /// </summary>
    Task<ResolvedConfigRecord> LoadConfigAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Lists value revisions for a binding, newest first.</summary>
    Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(Guid bindingId, CancellationToken ct = default);

    /// <summary>Lists value revisions for the binding matching <paramref name="forEntity" /> and <paramref name="key" />, newest first.</summary>
    Task<IReadOnlyList<ConfigBindingRevisionRecord>> GetBindingRevisionsAsync(EntityRef forEntity, string key, CancellationToken ct = default);

    /// <summary>Gets a single revision by number, or null if missing.</summary>
    Task<ConfigBindingRevisionRecord?> GetBindingRevisionAsync(Guid bindingId, int revision, CancellationToken ct = default);

    /// <summary>Sets the binding&apos;s current value to the snapshot at <paramref name="revision" /> and appends a new revision row (so history stays linear).</summary>
    Task RevertBindingToRevisionAsync(Guid bindingId, int revision, CancellationToken ct = default);

    /// <summary>
    /// Same as <see cref="RevertBindingToRevisionAsync(System.Guid,int,System.Threading.CancellationToken)" /> for the binding resolved from <paramref name="forEntity" /> and
    /// <paramref name="key" />.
    /// </summary>
    Task RevertBindingToRevisionAsync(EntityRef forEntity, string key, int revision, CancellationToken ct = default);
}