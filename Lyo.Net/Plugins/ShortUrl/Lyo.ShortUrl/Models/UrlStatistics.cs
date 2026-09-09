namespace Lyo.ShortUrl.Models;

/// <summary>A short URL statistics.</summary>
public record UrlStatistics(string ShortUrl, string LongUrl, long ClickCount, DateTime CreatedDate, DateTime? LastAccessedDate = null);