using System.Text.Json;
using Lyo.Diagram.Models;
using Lyo.Diagram.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Json;

namespace Lyo.Diagram.Postgres.Mapping;

/// <summary>Maps <see cref="DiagramDocument" /> to and from the Postgres entity. The JSON payload is the page list, not the id or name.</summary>
public static class DiagramMapper
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();

    /// <summary>Copies <paramref name="diagram" /> onto <paramref name="existing" />, or allocates a new entity.</summary>
    public static DiagramEntity ToEntity(DiagramDocument diagram, DiagramEntity? existing = null)
    {
        ArgumentHelpers.ThrowIfNull(diagram);
        ArgumentHelpers.ThrowIfEmpty(diagram.Id);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(diagram.Name);
        ArgumentHelpers.ThrowIfNull(diagram.Pages);
        var entity = existing ?? new DiagramEntity { Id = diagram.Id };
        entity.Name = diagram.Name;
        entity.DocumentJson = JsonSerializer.Serialize(diagram.Pages, Json);
        return entity;
    }

    /// <summary>Maps an EF entity back to a store document. The row's name and id win over anything inside the JSON.</summary>
    public static DiagramDocument ToModel(DiagramEntity entity)
    {
        ArgumentHelpers.ThrowIfNull(entity);
        var pages = JsonSerializer.Deserialize<List<DiagramPage>>(entity.DocumentJson, Json) ?? [];
        return new() { Id = entity.Id, Name = entity.Name, Pages = pages };
    }
}
