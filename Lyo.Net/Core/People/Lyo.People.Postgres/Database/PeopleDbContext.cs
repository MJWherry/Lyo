using Microsoft.EntityFrameworkCore;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity Framework Core DbContext for People data in PostgreSQL.</summary>
public class PeopleDbContext : DbContext
{
    public DbSet<PersonEntity> Persons { get; set; } = null!;

    public DbSet<PhoneNumberEntity> PhoneNumbers { get; set; } = null!;

    public DbSet<EmailAddressEntity> EmailAddresses { get; set; } = null!;

    public DbSet<ContactPhoneNumberEntity> ContactPhoneNumbers { get; set; } = null!;

    public DbSet<ContactEmailAddressEntity> ContactEmailAddresses { get; set; } = null!;

    public DbSet<SocialMediaProfileEntity> SocialMediaProfiles { get; set; } = null!;

    public DbSet<AddressEntity> Addresses { get; set; } = null!;

    public DbSet<ContactAddressEntity> ContactAddresses { get; set; } = null!;

    public DbSet<IdentificationEntity> Identifications { get; set; } = null!;

    public DbSet<PersonRelationshipEntity> PersonRelationships { get; set; } = null!;

    public DbSet<EmploymentEntity> Employments { get; set; } = null!;

    public PeopleDbContext(DbContextOptions<PeopleDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("people");
        modelBuilder.ApplyConfiguration(new PersonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PhoneNumberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContactPhoneNumberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContactEmailAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SocialMediaProfileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContactAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new IdentificationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PersonRelationshipEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmploymentEntityConfiguration());
    }
}