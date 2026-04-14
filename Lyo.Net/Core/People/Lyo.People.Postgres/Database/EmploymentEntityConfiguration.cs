using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class EmploymentEntityConfiguration : IEntityTypeConfiguration<EmploymentEntity>
{
    public void Configure(EntityTypeBuilder<EmploymentEntity> builder)
    {
        builder.ToTable("employment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.PersonId).HasColumnName("person_id");
        builder.Property(e => e.CompanyName).HasMaxLength(200).IsRequired().HasColumnName("company_name");
        builder.Property(e => e.JobTitle).HasMaxLength(200).HasColumnName("job_title");
        builder.Property(e => e.Department).HasMaxLength(100).HasColumnName("department");
        builder.Property(e => e.StartDate).IsRequired().HasColumnType("date").HasColumnName("start_date");
        builder.Property(e => e.EndDate).HasColumnType("date").HasColumnName("end_date");
        builder.Property(e => e.EmployeeId).HasMaxLength(50).HasColumnName("employee_id");
        builder.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
        builder.Property(e => e.CompanyAddressId).HasColumnName("company_address_id");
        builder.Property(e => e.SupervisorPersonId).HasColumnName("supervisor_person_id");
        builder.Property(e => e.Salary).HasColumnName("salary");
        builder.Property(e => e.SalaryCurrency).HasMaxLength(3).HasColumnName("salary_currency");
        builder.Property(e => e.Type).HasMaxLength(20).HasColumnName("type");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.PersonId).HasDatabaseName("ix_employment_person_id");
        builder.HasIndex(e => e.CompanyName).HasDatabaseName("ix_employment_company_name");
        builder.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.CompanyAddress).WithMany().HasForeignKey(e => e.CompanyAddressId).OnDelete(DeleteBehavior.Restrict);
    }
}