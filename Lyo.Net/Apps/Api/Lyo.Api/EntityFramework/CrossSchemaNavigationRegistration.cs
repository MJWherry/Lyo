using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.EntityFramework;

/// <summary>One DI-registered relationship applied to a host <see cref="DbContext" /> after <c>OnModelCreating</c> completes.</summary>
public sealed class CrossSchemaNavigationRegistration
{
    /// <summary>Root entity CLR type that owns that foreign key.</summary>
    public required Type RootEntityType { get; init; }

    /// <summary>Related entity CLR type (may live in another schema or module).</summary>
    public required Type RelatedEntityType { get; init; }

    /// <summary>Name of the CLR navigation property on the root entity.</summary>
    public required string NavigationName { get; init; }

    /// <summary>Foreign-key property name on that root entity.</summary>
    public required string ForeignKeyPropertyName { get; init; }

    /// <summary>When true, the related type is already mapped on the context. Only the relationship is added.</summary>
    public bool SameContext { get; init; }

    /// <summary>Applies table mapping (when cross-schema) and the <c>HasOne</c> relationship via <see cref="Apply" />.</summary>
    public required Action<ModelBuilder> Apply { get; init; }
}