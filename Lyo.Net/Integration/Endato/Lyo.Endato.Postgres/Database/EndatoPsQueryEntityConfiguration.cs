using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoPsQueryEntityConfiguration : IEntityTypeConfiguration<EndatoPsQueryEntity>
{
    public void Configure(EntityTypeBuilder<EndatoPsQueryEntity> builder)
    {
        builder.ToTable("endato_ps_query");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.FirstName).HasMaxLength(25).IsRequired().HasColumnName("first_name");
        builder.Property(e => e.LastName).HasMaxLength(50).IsRequired().HasColumnName("last_name");
        builder.Property(e => e.DateOfBirth).IsRequired().HasColumnName("date_of_birth");
        builder.Property(e => e.TotalRequestExecutionTime).HasColumnName("total_request_execution_time");
        builder.Property(e => e.RequestId).IsRequired().HasColumnName("request_id");
        builder.Property(e => e.RequestTime).IsRequired().HasColumnName("request_time");
        builder.Property(e => e.RequestTimestamp).IsRequired().HasColumnName("request_timestamp");
        builder.HasIndex(e => new { e.FirstName, e.LastName, e.DateOfBirth }).IsUnique().HasDatabaseName("ix_endato_ps_query_first_name_last_name_date_of_birth");
        builder.HasMany(e => e.People).WithOne(p => p.Query).HasForeignKey(p => p.QueryId).OnDelete(DeleteBehavior.Cascade);
    }
}