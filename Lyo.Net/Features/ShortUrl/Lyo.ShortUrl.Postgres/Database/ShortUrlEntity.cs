using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.ShortUrl.Postgres.Database;

/// <summary>Entity representing a short URL in the database.</summary>
public sealed class ShortUrlEntity
{
    /// <summary>Gets or sets the unique identifier (short code/alias).</summary>
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = null!;

    /// <summary>Gets or sets the original/long URL.</summary>
    [Required]
    [MaxLength(2048)]
    public string LongUrl { get; set; } = null!;

    /// <summary>Gets or sets the custom alias (if provided).</summary>
    [MaxLength(100)]
    public string? CustomAlias { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets or sets the expiration date (null if no expiration).</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Gets or sets the last accessed date.</summary>
    public DateTime? LastAccessedDate { get; set; }

    /// <summary>Gets or sets the total click count.</summary>
    public long ClickCount { get; set; }

    /// <summary>Gets or sets whether the URL is active (not deleted).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the navigation property for clicks.</summary>
    [InverseProperty(nameof(UrlClickEntity.ShortUrl))]
    public ICollection<UrlClickEntity> Clicks { get; set; } = new List<UrlClickEntity>();

    /// <summary>Builds the full short URL.</summary>
    public string BuildShortUrl(string? baseUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl))
            return $"{baseUrl.TrimEnd('/')}/{Id}";

        return Id; // Return just the ID if no base URL provided
    }

    /// <summary>Creates entity from URL shortening request.</summary>
    public static ShortUrlEntity FromShortenRequest(string id, string longUrl, string? customAlias, DateTime? expirationDate)
        => new() {
            Id = id,
            LongUrl = longUrl,
            CustomAlias = customAlias,
            CreatedTimestamp = DateTime.UtcNow,
            ExpirationDate = expirationDate,
            IsActive = true,
            ClickCount = 0
        };
}