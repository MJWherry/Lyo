using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class SocialMediaProfileEntityConfiguration : IEntityTypeConfiguration<SocialMediaProfileEntity>
{
    public void Configure(EntityTypeBuilder<SocialMediaProfileEntity> builder)
    {
        builder.ToTable("social_media_profile");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.PersonId).HasColumnName("person_id");
        builder.Property(e => e.Platform).HasMaxLength(50).IsRequired().HasColumnName("platform");
        builder.Property(e => e.Username).HasMaxLength(200).IsRequired().HasColumnName("username");
        builder.Property(e => e.ProfileUrl).HasMaxLength(500).HasColumnName("profile_url");
        builder.Property(e => e.VerifiedAt).HasColumnType("timestamp with time zone").HasColumnName("verified_at");
        builder.Property(e => e.DisplayName).HasMaxLength(200).HasColumnName("display_name");
        builder.Property(e => e.AddedAt).HasColumnType("timestamp with time zone").HasColumnName("added_at");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.PersonId).HasDatabaseName("ix_social_media_profile_person_id");
        builder.HasIndex(e => new { e.Platform, e.Username }).HasDatabaseName("ix_social_media_profile_platform_username");
        builder.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Cascade);
    }
}