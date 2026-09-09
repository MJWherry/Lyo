using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Maps canonical source columns and indexes for <see cref="EntitySourceEntityBase" /> subclasses.</summary>
/// <typeparam name="TEntity">Concrete entity type that carries inline provenance.</typeparam>
public abstract class EntitySourceConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : EntitySourceEntityBase
{
    private readonly string _indexPrefix;

    /// <summary>Mints configuration using <paramref name="indexPrefix" /> for PostgreSQL index names (for example <c>person</c>, <c>address</c>).</summary>
    protected EntitySourceConfiguration(string indexPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexPrefix);
        _indexPrefix = indexPrefix;
    }

    /// <inheritdoc />
    public virtual void Configure(EntityTypeBuilder<TEntity> builder) => MapSourceColumns(builder);

    /// <summary>Maps shared source columns; call from <see cref="Configure" /> after <c>ToTable</c> / <c>HasKey</c>.</summary>
    protected void MapSourceColumns(EntityTypeBuilder<TEntity> builder) => builder.ConfigureEntitySourceColumns(_indexPrefix);
}