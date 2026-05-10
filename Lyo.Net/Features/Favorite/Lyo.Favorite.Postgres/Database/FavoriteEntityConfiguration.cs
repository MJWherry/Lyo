using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Favorite.Postgres.Database;

public sealed class FavoriteEntityConfiguration : EntityRefConfiguration<FavoriteEntity>
{
    public FavoriteEntityConfiguration()
        : base("favorite") { }

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<FavoriteEntity> builder)
    {
        builder.ToTable("favorite");
        builder.HasKey(e => e.Id);
        base.Configure(builder);
    }
}
