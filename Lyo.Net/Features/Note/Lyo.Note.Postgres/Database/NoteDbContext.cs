using Microsoft.EntityFrameworkCore;

namespace Lyo.Note.Postgres.Database;

public class NoteDbContext : DbContext
{
    public DbSet<NoteEntity> Notes { get; set; } = null!;

    public NoteDbContext(DbContextOptions<NoteDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("note");
        modelBuilder.ApplyConfiguration(new NoteEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added && entry.Entity is NoteEntity n && n.CreatedTimestamp == default)
                n.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified && entry.Entity is NoteEntity n2)
                n2.UpdatedTimestamp = now;
        }

        return base.SaveChanges();
    }
}