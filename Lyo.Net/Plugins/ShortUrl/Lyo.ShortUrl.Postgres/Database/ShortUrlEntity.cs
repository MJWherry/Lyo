using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.ShortUrl.Postgres.Database;

/// <summary>Row that stores a short URL in the database.</summary>
public sealed class ShortUrlEntity
{
    /// <summary>Unique identifier (short code/alias) for this record.</summary>
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = null!;

    /// <summary>Original/long URL.</summary>
    [Required]
    [MaxLength(1024)]
    public string LongUrl { get; set; } = null!;

    /// <summary>Value of the custom alias (if provided).</summary>
    [MaxLength(100)]
    public string? CustomAlias { get; set; }

    /// <summary>Creation timestamp for this record.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Last update timestamp for this record.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Expiration date (null if no expiration) for this record.</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Value of the last accessed date.</summary>
    public DateTime? LastAccessedDate { get; set; }

    /// <summary>Total click count.</summary>
    public long ClickCount { get; set; }

    /// <summary>True when the URL is active (not deleted).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Navigation property for clicks for this record.</summary>
    [InverseProperty(nameof(UrlClickEntity.ShortUrl))]
    public ICollection<UrlClickEntity> Clicks { get; set; } = new List<UrlClickEntity>();

    /// <summary>Assembles the complete short URL.</summary>
    public string BuildShortUrl(string? baseUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl))
            return $"{baseUrl.TrimEnd('/')}/{Id}";

        return Id; // Return just the ID if no base URL provided
    }

    /// <summary>Builds an entity from URL shortening request.</summary>
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