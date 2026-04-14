namespace Lyo.Api.Models.Common.Response;

/// <summary>Metadata for a single property.</summary>
public sealed record PropertyMetadata(string Name, string Type, bool Nullable);

/// <summary>Metadata for an arbitrary CLR type exposed by a CRUD endpoint.</summary>
public sealed record TypeMetadata(string TypeName, IReadOnlyList<PropertyMetadata> Properties);

/// <summary>Metadata for a single entity type exposed by dynamic CRUD endpoints.</summary>
public sealed record EntityTypeMetadata(string EntityType, string KeyPropertyName, string KeyType, IReadOnlyList<PropertyMetadata> Properties);

/// <summary>Metadata response for MapAllCrudEndpoints / MapDynamicCrudEndpoints.</summary>
public sealed record CrudMetadataResponse(IReadOnlyList<EntityTypeMetadata> EntityTypes);

/// <summary>Metadata response for a typed CreateBuilder endpoint.</summary>
public sealed record EndpointMetadataResponse(TypeMetadata? Entity, TypeMetadata? Request, TypeMetadata? Response, string KeyPropertyName, string KeyType);