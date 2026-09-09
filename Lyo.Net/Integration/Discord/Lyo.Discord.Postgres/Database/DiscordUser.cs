namespace Lyo.Discord.Postgres.Database;

public class DiscordUser
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public int Discriminator { get; set; }

    public string? Email { get; set; }

    public string? Locale { get; set; }

    public bool? IsVerified { get; set; }

    public bool IsBot { get; set; }

    public bool? IsSystem { get; set; }

    public bool? IsMfaEnabled { get; set; }

    public string? PremiumLevel { get; set; }

    public DateTime UserCreatedDate { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime UpdatedTimestamp { get; set; }
}