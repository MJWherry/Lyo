namespace Lyo.ShortUrl.Models;

/// <summary>Statistics for a short URL.</summary>
public record UrlStatistics(string ShortUrl, string LongUrl, long ClickCount, DateTime CreatedDate, DateTime? LastAccessedDate = null);