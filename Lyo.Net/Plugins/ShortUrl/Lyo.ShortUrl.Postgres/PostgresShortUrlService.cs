using Lyo.Exceptions;
using Lyo.ShortUrl.Models;
using Lyo.ShortUrl.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Lyo.ShortUrl.ShortUrlErrorCodes;

namespace Lyo.ShortUrl.Postgres;

/// <summary>Persists short URLs in PostgreSQL and expands them on demand.</summary>
public sealed class PostgresShortUrlService : ShortUrlServiceBase
{
    private readonly IDbContextFactory<ShortUrlDbContext> _factory;
    private readonly IShortUrlGenerator _generator;

    /// <summary>Creates the service.</summary>
    public PostgresShortUrlService(
        IDbContextFactory<ShortUrlDbContext> factory,
        IShortUrlGenerator generator,
        ShortUrlServiceOptions options,
        ILogger<PostgresShortUrlService>? logger = null)
        : base(options, logger)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        ArgumentHelpers.ThrowIfNull(generator);
        _factory = factory;
        _generator = generator;
    }

    /// <inheritdoc />
    protected override async Task<UrlShortenResult> ShortenCoreAsync(string longUrl, string? customAlias, DateTime? expirationDate, CancellationToken ct)
    {
        var id = string.IsNullOrWhiteSpace(customAlias) ? _generator.Generate(8) : customAlias;
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await db.ShortUrls.AnyAsync(s => s.Id == id, ct).ConfigureAwait(false))
            return UrlShortenResult.FromError($"Short id '{id}' is already in use.", InvalidAliasLength, longUrl);

        var created = DateTime.UtcNow;
        db.ShortUrls.Add(ShortUrlEntity.FromShortenRequest(id, longUrl, customAlias, expirationDate));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UrlShortenResult.FromSuccess(BuildShortUrl(id), longUrl, customAlias, created, expirationDate, "URL shortened.");
    }

    /// <inheritdoc />
    protected override async Task<UrlExpandResult> ExpandCoreAsync(string shortUrl, CancellationToken ct)
    {
        var id = IdFrom(shortUrl);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await db.ShortUrls.FirstOrDefaultAsync(s => s.Id == id && s.IsActive, ct).ConfigureAwait(false);
        if (row == null)
            return UrlExpandResult.FromError("Short URL was not found.", ExpandFailed, shortUrl);

        if (row.ExpirationDate is { } expires && expires <= DateTime.UtcNow)
            return UrlExpandResult.FromError("Short URL has expired.", ExpandFailed, shortUrl);

        row.ClickCount++;
        row.LastAccessedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UrlExpandResult.FromSuccess(row.LongUrl, id);
    }

    /// <inheritdoc />
    public override Task<bool> TestConnectionAsync(CancellationToken ct = default) => Task.FromResult(true);

    private string BuildShortUrl(string id)
        => string.IsNullOrWhiteSpace(Options.BaseUrl) ? id : $"{Options.BaseUrl.TrimEnd('/')}/{id}";

    private static string IdFrom(string shortUrl)
    {
        var id = shortUrl.Trim().TrimEnd('/');
        var slash = id.LastIndexOf('/');
        return slash < 0 ? id : id[(slash + 1)..];
    }
}
