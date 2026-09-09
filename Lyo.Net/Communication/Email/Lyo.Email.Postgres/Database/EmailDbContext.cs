using Microsoft.EntityFrameworkCore;

namespace Lyo.Email.Postgres.Database;

/// <summary>EF Core context for the <c>email</c> schema. Hosts map send or fetch outcomes and insert rows; this package does not send or receive mail.</summary>
public class EmailDbContext : DbContext
{
    /// <summary>Outbound and inbound email log rows.</summary>
    public DbSet<EmailLogEntity> EmailLogs { get; set; } = null!;

    /// <summary>Attachment metadata rows keyed to <see cref="EmailLogs" />. Bytes are not stored.</summary>
    public DbSet<EmailAttachmentLogEntity> EmailAttachmentLogs { get; set; } = null!;

    /// <summary>Creates a context with the given options.</summary>
    /// <param name="options">EF Core options, including the Npgsql connection.</param>
    public EmailDbContext(DbContextOptions<EmailDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("email");
        modelBuilder.ApplyConfiguration(new EmailLogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailAttachmentLogEntityConfiguration());
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is EmailLogEntity log && log.CreatedTimestamp == default)
                    log.CreatedTimestamp = now;
                else if (entry.Entity is EmailAttachmentLogEntity att && att.CreatedTimestamp == default)
                    att.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is EmailLogEntity log)
                    log.UpdatedTimestamp = now;
                else if (entry.Entity is EmailAttachmentLogEntity att)
                    att.UpdatedTimestamp = now;
            }
        }
    }
}
