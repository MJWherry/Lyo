using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Note.Postgres.Database;

public class NoteDbContext : EntityRefModuleDbContext
{
    public DbSet<NoteEntity> Notes { get; set; } = null!;

    public NoteDbContext(DbContextOptions<NoteDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("note");
        modelBuilder.ApplyConfiguration(new NoteEntityConfiguration());
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        StampNoteTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampNoteTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampNoteTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<NoteEntity>()) {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedTimestamp = now;
        }
    }
}
