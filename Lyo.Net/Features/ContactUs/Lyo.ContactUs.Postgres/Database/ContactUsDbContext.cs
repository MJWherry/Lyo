using Microsoft.EntityFrameworkCore;

namespace Lyo.ContactUs.Postgres.Database;

public class ContactUsDbContext : DbContext
{
    public DbSet<ContactSubmissionEntity> ContactSubmissions { get; set; } = null!;

    public ContactUsDbContext(DbContextOptions<ContactUsDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("contact");
        modelBuilder.ApplyConfiguration(new ContactSubmissionEntityConfiguration());
    }
}