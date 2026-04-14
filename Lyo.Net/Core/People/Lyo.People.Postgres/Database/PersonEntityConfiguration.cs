using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class PersonEntityConfiguration : IEntityTypeConfiguration<PersonEntity>
{
    public void Configure(EntityTypeBuilder<PersonEntity> builder)
    {
        builder.ToTable("person");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.NamePrefix).HasMaxLength(12).HasColumnName("name_prefix");
        builder.Property(e => e.FirstName).HasMaxLength(25).IsRequired().HasColumnName("first_name");
        builder.Property(e => e.MiddleName).HasMaxLength(25).HasColumnName("middle_name");
        builder.Property(e => e.LastName).HasMaxLength(25).IsRequired().HasColumnName("last_name");
        builder.Property(e => e.NameSuffix).HasMaxLength(12).HasColumnName("name_suffix");
        builder.Property(e => e.Source).HasMaxLength(30).IsRequired().HasDefaultValue("Manual").HasColumnName("source");
        builder.Property(e => e.PreferredName).HasMaxLength(100).HasColumnName("preferred_name");
        builder.Property(e => e.MaidenName).HasMaxLength(100).HasColumnName("maiden_name");
        builder.Property(e => e.DateOfBirth).HasColumnType("date").HasColumnName("date_of_birth");
        builder.Property(e => e.Sex).HasMaxLength(1).HasColumnName("sex");
        builder.Property(e => e.Nationality).HasMaxLength(3).HasColumnName("nationality");
        builder.Property(e => e.PreferredLanguageBcp47).HasMaxLength(20).HasColumnName("preferred_language_bcp47");
        builder.Property(e => e.Race).HasMaxLength(1).HasColumnName("race");
        builder.Property(e => e.MaritalStatus).HasMaxLength(1).HasColumnName("marital_status");
        builder.Property(e => e.DisabilityStatus).HasMaxLength(2).HasColumnName("disability_status");
        builder.Property(e => e.VeteranStatus).HasMaxLength(2).HasColumnName("veteran_status");
        builder.Property(e => e.PlaceOfBirthAddressId).HasColumnName("place_of_birth_address_id");
        builder.Property(e => e.EmergencyContactPersonId).HasColumnName("emergency_contact_person_id");
        builder.Property(e => e.CurrentJobTitle).HasMaxLength(200).HasColumnName("current_job_title");
        builder.Property(e => e.CurrentCompany).HasMaxLength(200).HasColumnName("current_company");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.Property(e => e.CreatedBy).HasMaxLength(500).HasColumnName("created_by");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.Notes).HasMaxLength(4000).HasColumnName("notes");
        builder.Property(e => e.CitizenshipJson).HasColumnName("citizenship_json").HasColumnType("jsonb");
        builder.Property(e => e.PreferencesJson).HasColumnName("preferences_json").HasColumnType("jsonb");
        builder.Property(e => e.CustomFieldsJson).HasColumnName("custom_fields_json").HasColumnType("jsonb");
        builder.HasIndex(e => e.FirstName).HasDatabaseName("ix_person_first_name");
        builder.HasIndex(e => e.LastName).HasDatabaseName("ix_person_last_name");
        builder.HasIndex(e => new { e.LastName, e.FirstName }).HasDatabaseName("ix_person_last_name_first_name");
        builder.HasIndex(e => e.IsActive).HasDatabaseName("ix_person_is_active");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_person_created_timestamp");
    }
}