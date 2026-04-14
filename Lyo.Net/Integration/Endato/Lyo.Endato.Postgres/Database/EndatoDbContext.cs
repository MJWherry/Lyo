using Microsoft.EntityFrameworkCore;

namespace Lyo.Endato.Postgres.Database;

public class EndatoDbContext : DbContext
{
    public DbSet<EndatoPsQueryEntity> EndatoPsQueries { get; set; } = null!;

    public DbSet<EndatoPsPersonEntity> EndatoPsPersons { get; set; } = null!;

    public DbSet<EndatoPsAddressEntity> EndatoPsAddresses { get; set; } = null!;

    public DbSet<EndatoPsEmailAddressEntity> EndatoPsEmailAddresses { get; set; } = null!;

    public DbSet<EndatoPsPhoneNumberEntity> EndatoPsPhoneNumbers { get; set; } = null!;

    public DbSet<EndatoCePersonEntity> EndatoCePersons { get; set; } = null!;

    public DbSet<EndatoCeQueryEntity> EndatoCeQueries { get; set; } = null!;

    public DbSet<EndatoCeAddressEntity> EndatoCeAddresses { get; set; } = null!;

    public DbSet<EndatoCePhoneNumberEntity> EndatoCePhoneNumbers { get; set; } = null!;

    public DbSet<EndatoCeEmailAddressEntity> EndatoCeEmailAddresses { get; set; } = null!;

    public EndatoDbContext(DbContextOptions<EndatoDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("endato");
        modelBuilder.ApplyConfiguration(new EndatoPsQueryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoPsPersonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoPsAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoPsEmailAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoPsPhoneNumberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoCePersonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoCeQueryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoCeAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoCePhoneNumberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EndatoCeEmailAddressEntityConfiguration());
    }
}