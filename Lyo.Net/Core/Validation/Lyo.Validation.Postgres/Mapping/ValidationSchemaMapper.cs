using System.Text.Json;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Validation.Postgres.Database;

namespace Lyo.Validation.Postgres.Mapping;

/// <summary>Maps <see cref="ValidationSchema" /> to/from the Postgres entity using Lyo JSON defaults.</summary>
public static class ValidationSchemaMapper
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();

    /// <summary>Maps a store document to an EF entity (new id when <paramref name="existing" /> is null).</summary>
    public static ValidationSchemaEntity ToEntity(ValidationSchema schema, ValidationSchemaEntity? existing = null)
    {
        ArgumentHelpers.ThrowIfNull(schema);
        ArgumentHelpers.ThrowIfNull(schema.Constraints);
        var entity = existing ?? new ValidationSchemaEntity { Id = Guid.NewGuid() };
        entity.Key = schema.Key;
        entity.TargetTypeName = schema.TargetTypeName;
        entity.Description = schema.Description;
        entity.ConstraintsJson = JsonSerializer.Serialize(schema.Constraints, Json);
        entity.MessagesJson = schema.Messages == null || schema.Messages.Count == 0 ? null : JsonSerializer.Serialize(schema.Messages, Json);
        return entity;
    }

    /// <summary>Maps an EF entity to a store document.</summary>
    public static ValidationSchema ToModel(ValidationSchemaEntity entity)
    {
        ArgumentHelpers.ThrowIfNull(entity);
        var constraints = JsonSerializer.Deserialize<WhereClause>(entity.ConstraintsJson, Json);
        ArgumentHelpers.ThrowIfNull(constraints);
        IReadOnlyDictionary<string, ValidationMessage>? messages = null;
        if (!string.IsNullOrWhiteSpace(entity.MessagesJson))
            messages = JsonSerializer.Deserialize<Dictionary<string, ValidationMessage>>(entity.MessagesJson, Json);

        return new() {
            Key = entity.Key,
            TargetTypeName = entity.TargetTypeName,
            Description = entity.Description,
            Constraints = constraints,
            Messages = messages
        };
    }
}
