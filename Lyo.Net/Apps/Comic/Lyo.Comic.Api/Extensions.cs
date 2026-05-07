using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Cache;
using Lyo.Comic.Api.Endpoints;
using Lyo.Comic.Api.Mapping;
using Lyo.Comic.Api.Models.Request;
using Lyo.Comic.Api.Models.Response;
using Lyo.Comic.Api.Services;
using Lyo.Comic.Postgres;
using Lyo.Comic.Postgres.Database;
using Lyo.Comment.Postgres;
using Lyo.Comment.Postgres.Database;
using Lyo.Common.Identifiers;
using Lyo.Encryption.Extensions;
using Lyo.Favorite.Postgres;
using Lyo.Favorite.Postgres.Database;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage;
using Lyo.Keystore;
using Lyo.Rating.Postgres;
using Lyo.Rating.Postgres.Database;
using Lyo.Tag;
using Lyo.Tag.Postgres;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Comic.Api;

/// <summary>Server-side upload policy for comic file storage. Clients never see these settings.</summary>
public sealed record ComicFileUploadOptions(bool Encrypt, bool Compress, string? KeyId);

public static class Extensions
{
    private const string FileStorageKey = "comic-files";

    /// <summary>Maps enriched/sub-resource Comic API route groups under the given prefix (default: /api/comic), and the file retrieval endpoints at /files.</summary>
    public static IEndpointRouteBuilder MapComicApi(this IEndpointRouteBuilder app, string prefix = "/api/comic")
    {
        var group = app.MapGroup(prefix);
        group.MapSeriesEndpoints();
        group.MapVolumeEndpoints();
        group.MapChapterEndpoints();
        app.MapFilesEndpoints();
        return app;
    }

    /// <summary>
    /// Registers standard CRUD + Query endpoints via ApiEndpointBuilder for each Comic entity. Series/Volume/Chapter skip the plain GET (enriched GET is provided by
    /// MapComicApi). Pages include the plain GET since no async enrichment is needed.
    /// </summary>
    public static WebApplication BuildComicApiEndpoints(this WebApplication app, string prefix = "/api/comic")
    {
        // Series: all operations except plain GET (enriched GET is in SeriesEndpoints)
        app.CreateBuilder<ComicDbContext, SeriesEntity, ComicSeriesReq, ComicSeriesRes>($"{prefix}/series", "ComicSeries")
            .AllowAnonymous()
            .WithCrud(
                ApiFeatureFlag.All & ~ApiFeatureFlag.Get, new() {
                    BeforeUpdate = ctx => {
                        // Replace AlternateTitles collection in-place (requires DB context access)
                        ctx.DbContext.Entry(ctx.Entity).Collection(e => e.AlternateTitles).Load();
                        ctx.DbContext.AlternateTitles.RemoveRange(ctx.Entity.AlternateTitles);
                        foreach (var a in ctx.Request.Data.AlternateTitles) {
                            ctx.DbContext.AlternateTitles.Add(
                                new() {
                                    Id = Guid.NewGuid(),
                                    SeriesId = ctx.Entity.Id,
                                    Title = a.Title,
                                    Language = a.Language
                                });
                        }
                    },
                    BeforeUpsert = ctx => {
                        ctx.DbContext.Entry(ctx.Entity).Collection(e => e.AlternateTitles).Load();
                        ctx.DbContext.AlternateTitles.RemoveRange(ctx.Entity.AlternateTitles);
                        foreach (var a in ctx.Request.NewData.AlternateTitles) {
                            ctx.DbContext.AlternateTitles.Add(
                                new() {
                                    Id = Guid.NewGuid(),
                                    SeriesId = ctx.Entity.Id,
                                    Title = a.Title,
                                    Language = a.Language
                                });
                        }
                    },
                    AfterCreateAsync = ApplyInitialSeriesTagsAsync
                })
            .Build();

        // Volumes: all operations except plain GET
        app.CreateBuilder<ComicDbContext, VolumeEntity, ComicVolumeReq, ComicVolumeRes>($"{prefix}/volumes", "ComicVolume")
            .AllowAnonymous()
            .WithCrud(ApiFeatureFlag.All & ~ApiFeatureFlag.Get, new())
            .Build();

        // Chapters: all operations except plain GET
        app.CreateBuilder<ComicDbContext, ChapterEntity, ComicChapterReq, ComicChapterRes>($"{prefix}/chapters", "ComicChapter")
            .AllowAnonymous()
            .WithCrud(ApiFeatureFlag.All & ~ApiFeatureFlag.Get, new())
            .Build();

        // Pages: all operations including plain GET (ComicPageRes has no async enrichment)
        app.CreateBuilder<ComicDbContext, PageEntity, ComicPageReq, ComicPageRes>($"{prefix}/pages", "ComicPage").AllowAnonymous().WithCrud(ApiFeatureFlag.All, new()).Build();

        // Characters: all CRUD + Query; volume appearances are managed via the join table
        app.CreateBuilder<ComicDbContext, CharacterEntity, ComicCharacterReq, ComicCharacterRes>($"{prefix}/characters", "ComicCharacter")
            .AllowAnonymous()
            .WithCrud(ApiFeatureFlag.All, new())
            .Build();

        return app;
    }

    private static async Task ApplyInitialSeriesTagsAsync(CreateContext<ComicSeriesReq, SeriesEntity, ComicDbContext> ctx)
    {
        var tags = ctx.Request.Tags;
        if (tags is null || tags.Count == 0)
            return;

        var tagStore = ctx.Services.GetRequiredService<ITagStore>();
        var entityRef = new EntityRef("ComicSeries", ctx.Entity.Id.ToString());
        foreach (var item in tags) {
            if (string.IsNullOrWhiteSpace(item.Name))
                continue;

            var tagType = string.IsNullOrWhiteSpace(item.TagType) ? "tag" : item.TagType.Trim();
            var slug = string.IsNullOrWhiteSpace(item.Slug) ? null : item.Slug.Trim();
            await tagStore.AddTagAsync(entityRef, item.Name.Trim(), tagType, slug: slug, ct: default).ConfigureAwait(false);
        }
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers all Comic API services: the five postgres stores, enrichment service, local cache, Lyo.Api query/CRUD services (including <c>IQueryService</c> for
        /// tag/rating/comment/favorite contexts for cached batch enrichment), the custom mapper, and the comic file storage stack (LocalKeyStore → TwoKeyEncryption →
        /// PostgresFileMetadataStore → LocalFileStorageService).
        /// </summary>
        public IServiceCollection AddComicApi(IConfiguration configuration)
        {
            services.AddPostgresComicStoreFromConfiguration(configuration);
            services.AddPostgresTagStoreFromConfiguration(configuration);
            services.AddPostgresCommentStoreFromConfiguration(configuration);
            services.AddPostgresRatingStoreFromConfiguration(configuration);
            services.AddPostgresFavoriteStoreFromConfiguration(configuration);
            services.AddScoped<ComicEnrichmentService>();
            services.AddLocalCache();
            services.AddLyoQueryServices();
            services.AddLyoCrudServices<ComicDbContext>();
            services.AddLyoCrudServices<TagDbContext>();
            services.AddLyoCrudServices<RatingDbContext>();
            services.AddLyoCrudServices<CommentDbContext>();
            services.AddLyoCrudServices<FavoriteDbContext>();
            services.AddSingleton<ILyoMapper, ComicLyoMapper>();
            services.AddComicFileStorage(configuration);
            return services;
        }

        private IServiceCollection AddComicFileStorage(IConfiguration configuration)
        {
            var keyId = configuration["ComicFileEncryption:KeyId"] ?? "comic-images";
            var keySecret = configuration["ComicFileEncryption:KeySecret"] ?? "change-me-in-production";
            var encrypt = configuration.GetValue("ComicFileEncryption:Encrypt", true);
            var compress = configuration.GetValue("ComicFileEncryption:Compress", false);
            var keyStore = new LocalKeyStore();
            keyStore.AddKeyFromString(keyId, "v1", keySecret);
            services.AddKeyedSingleton<IKeyStore>(FileStorageKey, (_, _) => keyStore);
            services.AddKeyedSingleton<LocalKeyStore>(FileStorageKey, (_, _) => keyStore);
            services.AddEncryptionServiceKeyed(FileStorageKey, FileStorageKey);
            services.AddKeyedSingleton(FileStorageKey, new ComicFileUploadOptions(encrypt, compress, encrypt ? keyId : null));
            services.AddFileMetadataStoreDbContextFactoryFromConfiguration(configuration);
            services.AddKeyedScoped<IFileMetadataStore>(
                FileStorageKey, (provider, _) => {
                    var factory = provider.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>();
                    var dbContext = factory.CreateDbContext();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new PostgresFileMetadataStore(dbContext, loggerFactory);
                });

            services.AddFileStorageServiceKeyed(
                FileStorageKey, opts => opts.RootDirectoryPath = configuration["ComicFileStorage:RootDirectoryPath"] ?? "./comic-files",
                provider => provider.GetRequiredKeyedService<IFileMetadataStore>(FileStorageKey), FileStorageKey);

            return services;
        }
    }
}