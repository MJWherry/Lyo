using Microsoft.EntityFrameworkCore;

namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>EF Core context for schema <c>leaderboard</c>.</summary>
public sealed class LeaderboardDbContext : DbContext
{
    public DbSet<BoardEntity> Boards { get; set; } = null!;

    public DbSet<ScoringPropertyEntity> Properties { get; set; } = null!;

    public DbSet<MemberEntity> Members { get; set; } = null!;

    public DbSet<InviteEntity> Invites { get; set; } = null!;

    public DbSet<EntryEntity> Entries { get; set; } = null!;

    public DbSet<EntryValueEntity> EntryValues { get; set; } = null!;

    public DbSet<AdjustmentEntity> Adjustments { get; set; } = null!;

    public DbSet<StandingEntity> Standings { get; set; } = null!;

    public LeaderboardDbContext(DbContextOptions<LeaderboardDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresLeaderboardOptions.Schema);
        modelBuilder.ApplyConfiguration(new BoardEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ScoringPropertyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MemberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InviteEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EntryValueEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AdjustmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StandingEntityConfiguration());
    }
}