using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.EntityFramework;

/// <summary>One DI-registered relationship applied to a host <see cref="DbContext" /> after <c>OnModelCreating</c>.</summary>
public sealed class CrossSchemaNavigationRegistration
{
    /// <summary>Root entity CLR type that owns the foreign key.</summary>
    public required Type RootEntityType { get; init; }

    /// <summary>Related entity CLR type (may live in another schema / module).</summary>
    public required Type RelatedEntityType { get; init; }

    /// <summary>CLR navigation property name on the root entity.</summary>
    public required string NavigationName { get; init; }

    /// <summary>Foreign-key property name on the root entity.</summary>
    public required string ForeignKeyPropertyName { get; init; }

    /// <summary>When true, the related type is already mapped on the context; only the relationship is added.</summary>
    public bool SameContext { get; init; }

    /// <summary>Applies table mapping (when cross-schema) and <c>HasOne</c> relationship via <see cref="Apply" />.</summary>
    public required Action<ModelBuilder> Apply { get; init; }
}