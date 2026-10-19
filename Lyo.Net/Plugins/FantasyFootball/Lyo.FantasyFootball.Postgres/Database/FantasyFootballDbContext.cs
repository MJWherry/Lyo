using Microsoft.EntityFrameworkCore;

namespace Lyo.FantasyFootball.Postgres.Database;

/// <summary>EF Core context for schema <c>fantasy_football</c>.</summary>
public sealed class FantasyFootballDbContext : DbContext
{
    public DbSet<LeagueEntity> Leagues { get; set; } = null!;

    public DbSet<MemberEntity> Members { get; set; } = null!;

    public DbSet<TeamEntity> Teams { get; set; } = null!;

    public DbSet<RosterEntryEntity> RosterEntries { get; set; } = null!;

    public DbSet<ConversationEntity> Conversations { get; set; } = null!;

    public DbSet<ConversationMemberEntity> ConversationMembers { get; set; } = null!;

    public DbSet<MessageEntity> Messages { get; set; } = null!;

    public DbSet<MessagePlayerEntity> MessagePlayers { get; set; } = null!;

    public DbSet<TradeEntity> Trades { get; set; } = null!;

    public DbSet<TradeItemEntity> TradeItems { get; set; } = null!;

    public FantasyFootballDbContext(DbContextOptions<FantasyFootballDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresFantasyFootballOptions.Schema);
        modelBuilder.ApplyConfiguration(new LeagueEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MemberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TeamEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RosterEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationMemberEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MessagePlayerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TradeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TradeItemEntityConfiguration());
    }
}
