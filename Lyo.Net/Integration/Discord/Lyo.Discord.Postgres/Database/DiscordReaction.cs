namespace Lyo.Discord.Postgres.Database;

/// <summary>Reaction row (composite primary key). Not exposed via generated CRUD/query endpoints because the API layer requires a single-column key for default ordering.</summary>
public class DiscordReaction
{
    public long MessageId { get; set; }

    public long ReactorId { get; set; }

    public long EmojiId { get; set; }

    public DateTime? CreatedDate { get; set; }
}