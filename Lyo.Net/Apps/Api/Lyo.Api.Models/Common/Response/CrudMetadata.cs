using System.Diagnostics;

namespace Lyo.Api.Models.Common.Response;

/// <summary>Metadata for one property.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PropertyMetadata(string Name, string Type, bool Nullable);

/// <summary>Metadata for an arbitrary CLR type exposed by a CRUD endpoint.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TypeMetadata(string TypeName, IReadOnlyList<PropertyMetadata> Properties);

/// <summary>Metadata for one entity type exposed by dynamic CRUD endpoints.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EntityTypeMetadata(string EntityType, string KeyPropertyName, string KeyType, IReadOnlyList<PropertyMetadata> Properties);

/// <summary>Metadata body for MapAllCrudEndpoints / MapDynamicCrudEndpoints.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CrudMetadataResponse(IReadOnlyList<EntityTypeMetadata> EntityTypes);

/// <summary>Metadata body for a typed CreateBuilder endpoint.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndpointMetadataResponse(TypeMetadata? Entity, TypeMetadata? Request, TypeMetadata? Response, string KeyPropertyName, string KeyType);