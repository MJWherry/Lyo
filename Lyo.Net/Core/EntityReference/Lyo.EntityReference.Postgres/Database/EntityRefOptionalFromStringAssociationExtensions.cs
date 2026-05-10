using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Fluent mapping extensions for <see cref="EntityRefOptionalFromStringAssociationBase"/> columns.</summary>
public static class EntityRefOptionalFromStringAssociationExtensions
{
    /// <summary>Maps for/from string columns with a shared max length (default 500, matching change-tracker conventions).</summary>
    /// <typeparam name="TEntity">Entity deriving from <see cref="EntityRefOptionalFromStringAssociationBase"/>.</typeparam>
    /// <param name="builder">EF Core entity type builder.</param>
    /// <param name="maxLength">Maximum length for all four string columns.</param>
    public static void MapOptionalFromStringAssociationColumns<TEntity>(this EntityTypeBuilder<TEntity> builder, int maxLength = 500)
        where TEntity : EntityRefOptionalFromStringAssociationBase
    {
        builder.Property(e => e.ForEntityType).HasMaxLength(maxLength).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(maxLength).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.FromEntityType).HasMaxLength(maxLength).HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(maxLength).HasColumnName("from_entity_id");
    }
}
