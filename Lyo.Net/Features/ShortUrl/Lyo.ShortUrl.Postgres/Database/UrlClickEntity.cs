using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.ShortUrl.Postgres.Database;

/// <summary>Entity representing a click/access event for a short URL.</summary>
public sealed class UrlClickEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Gets or sets the short URL ID (foreign key).</summary>
    [Required]
    [MaxLength(100)]
    public string ShortUrlId { get; set; } = null!;

    /// <summary>Gets or sets the click timestamp.</summary>
    [Required]
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the IP address of the requester (optional).</summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the user agent (optional).</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>Gets or sets the referrer URL (optional).</summary>
    [MaxLength(2048)]
    public string? Referrer { get; set; }

    /// <summary>Gets or sets the navigation property to the short URL.</summary>
    [ForeignKey(nameof(ShortUrlId))]
    public ShortUrlEntity? ShortUrl { get; set; }
}