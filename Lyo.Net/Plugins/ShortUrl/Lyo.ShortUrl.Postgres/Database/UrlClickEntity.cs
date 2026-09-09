using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.ShortUrl.Postgres.Database;

/// <summary>Row that stores a click/access event for a short URL.</summary>
public sealed class UrlClickEntity
{
    /// <summary>Holds the unique identifier.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Short URL ID (foreign key).</summary>
    [Required]
    [MaxLength(100)]
    public string ShortUrlId { get; set; } = null!;

    /// <summary>Holds the click timestamp.</summary>
    [Required]
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Value of the IP address of the requester (optional).</summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>Value of the user agent (optional).</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>Holds the referrer URL (optional).</summary>
    [MaxLength(512)]
    public string? Referrer { get; set; }

    /// <summary>Value of the navigation property to the short URL.</summary>
    [ForeignKey(nameof(ShortUrlId))]
    public ShortUrlEntity? ShortUrl { get; set; }
}