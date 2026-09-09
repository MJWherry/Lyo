using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoCeQueryEntityConfiguration : IEntityTypeConfiguration<EndatoCeQueryEntity>
{
    public void Configure(EntityTypeBuilder<EndatoCeQueryEntity> builder)
    {
        builder.ToTable("endato_ce_query");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.FirstName).HasMaxLength(25).IsRequired().HasColumnName("first_name");
        builder.Property(e => e.LastName).HasMaxLength(50).IsRequired().HasColumnName("last_name");
        builder.Property(e => e.AddressLineOne).HasMaxLength(50).HasColumnName("address_line_one");
        builder.Property(e => e.AddressLineTwo).HasMaxLength(50).IsRequired().HasColumnName("address_line_two");
        builder.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(e => e.IdentityScore).IsRequired().HasColumnName("identity_score");
        builder.Property(e => e.TotalRequestExecutionTime).HasColumnName("total_request_execution_time");
        builder.Property(e => e.EndatoCePersonId).HasColumnName("endato_ce_person_id");
        builder.Property(e => e.RequestId).IsRequired().HasColumnName("request_id");
        builder.Property(e => e.RequestTime).IsRequired().HasColumnName("request_time");
        builder.Property(e => e.RequestTimestamp).IsRequired().HasColumnName("request_timestamp");
        builder.HasIndex(e => new { e.FirstName, e.LastName, e.DateOfBirth }).IsUnique().HasDatabaseName("ix_endato_ce_query_first_name_last_name_date_of_birth");
        builder.HasIndex(e => e.EndatoCePersonId).IsUnique().HasDatabaseName("ix_endato_ce_query_endato_ce_person_id");
        builder.HasOne(e => e.EndatoCePerson).WithOne(p => p.Query).HasForeignKey<EndatoCeQueryEntity>(e => e.EndatoCePersonId);
    }
}