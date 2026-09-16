using Lyo.Exceptions;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Authentication;
using Lyo.Api.Export;
using Lyo.Api.FileStorage;
using Lyo.Reporting.Api;
using Lyo.Api.Services.Crud;
using Lyo.Common.Core.Identifiers;
using Lyo.Config.Api;
using Lyo.Discord.Postgres;
using Lyo.Endato.Postgres.Database;
using Lyo.FileStorage.Abstractions;
using Lyo.Book;
using Lyo.Book.Postgres.Database;
using Lyo.Music;
using Lyo.Music.Postgres.Database;
using Lyo.Video;
using Lyo.Video.Postgres.Database;
using Lyo.Comic.Api;
using Lyo.Algorithms.Api;
using Lyo.Story.Api;
using Lyo.Leaderboard.Api;
using Lyo.HomeInventory.Api;
using Lyo.Recipe.Api;
using Lyo.Ledger.Api;
using Lyo.Billing.Api;
using Lyo.Job.Api;
using Lyo.Web.Automation.Api;
using Lyo.Web.Automation.Postgres;
using Lyo.Job.Postgres;
using Lyo.Drift.Api;
using Lyo.Drift.Postgres;
using Lyo.People.Models;
using Lyo.People.Postgres.Database;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.TestApi.Person.Request;
using Lyo.TestApi.Person.Response;

namespace Lyo.TestApi;

public static class Setup
{
    extension(WebApplication app)
    {
        public WebApplication SetupEndpoints()
        {
            app = app.BuildAuthenticationApi()
                .BuildJobGroup()
                .BuildAutomationGroup()
                .BuildAutomationPlanRecords()
                .BuildDriftGroup()
                .BuildBookGroup()
                .BuildMusicGroup()
                .BuildVideoGroup()
                .BuildComicGroup()
                .BuildStoryGroup(allowAnonymous: true)
                .BuildAlgorithmsGroup()
                .BuildHomeInventoryGroup()
                .BuildRecipeGroup()
                .BuildLedgerGroup()
                .BuildBillingGroup()
                //.BuildDocketGroup()
                // Same as the Job test host: leave open for the Gateway workbench (auth can be tightened later).
                // Stream stored outputs from this host's keyed FileStorage so it matches the AfterRender save hook.
                .BuildReportingGroup(
                    ReportingApiOptions.WithAuth(
                        EndpointAuth.Anonymous(), (ctx, ct) => {
                            var storage = ctx.Services.GetRequiredKeyedService<IFileStorageService>(Constants.FileStorageWorkbench.ServiceKey);
                            return storage.GetFileStreamAsync(ctx.OutputFileId, ct: ct);
                        }))
                .BuildEndatoCeGroup()
                .BuildEndatoPsGroup()
                .BuildPersonGroup()
                .BuildDiscordGroup()
                //.BuildRecipientGroup()
                .BuildTwilioGroup()
                .BuildFileStorageApi(new() {
                    Route = Constants.FileStorageWorkbench.Route,
                    FileMetadataRoute = Constants.FileStorageWorkbench.FileMetadata,
                    ServiceKey = Constants.FileStorageWorkbench.ServiceKey
                });

            app.MapCacheEndpoints("Cache", auth: EndpointAuth.Anonymous());
            app.MapConfigApiEndpoints(requireAuthentication: false);
            app.MapLeaderboardApiEndpoints(requireAuthentication: false);
            return app;
        }

        public WebApplication BuildBookGroup()
        {
            app.MapDynamicCrudEndpoints<BookDbContext>(c => c.AllowAnonymous()
                .WithDefaults(d => d.BaseRoute = "Book")
                .IncludeOnly(
                    typeof(BookEntity),
                    typeof(BookSeriesEntity),
                    typeof(BookSeriesAlternateTitleEntity),
                    typeof(BookAlternateTitleEntity),
                    typeof(BookContributorEntity),
                    typeof(BookIdentifierEntity),
                    typeof(BookTypeEntity),
                    typeof(BookFormatEntity),
                    typeof(BookGenreEntity),
                    typeof(BookBookGenreEntity),
                    typeof(BookChapterEntity)));

            app.MapPost("/Book/search", async (BookQuery query, IBookStore store, CancellationToken ct) => Results.Ok(await store.SearchBooksAsync(query, ct)))
                .WithTags("Book")
                .AllowAnonymous();
            app.MapPost("/Book/pages/search", async (PageQuery query, IBookStore store, CancellationToken ct) => Results.Ok(await store.SearchPagesAsync(query, ct)))
                .WithTags("Book")
                .AllowAnonymous();
            app.MapGet("/Book/page/{id:guid}/text", async (Guid id, IBookStore store, CancellationToken ct) => {
                var text = await store.GetPageTextAsync(id, ct);
                return text is null && await store.GetPageByIdAsync(id, ct: ct) is null ? Results.NotFound() : Results.Ok(text);
            }).WithTags("Book").AllowAnonymous();
            app.MapPut("/Book/page/{id:guid}/text", async (Guid id, BookPageTextBody body, IBookStore store, CancellationToken ct) => {
                await store.SavePageTextAsync(id, body.Text, body.ContentType, ct);
                return Results.NoContent();
            }).WithTags("Book").AllowAnonymous();
            return app;
        }

        public WebApplication BuildMusicGroup()
        {
            app.MapDynamicCrudEndpoints<MusicDbContext>(c => c.AllowAnonymous()
                .WithDefaults(d => d.BaseRoute = "Music")
                .DenySelectFields("Lyrics", "SearchVector")
                .IncludeOnly(
                    typeof(MusicReleaseGroupEntity),
                    typeof(MusicReleaseEntity),
                    typeof(MusicTrackEntity),
                    typeof(MusicCreditEntity),
                    typeof(MusicIdentifierEntity),
                    typeof(MusicGenreEntity),
                    typeof(MusicFormatEntity),
                    typeof(MusicReleaseGenreEntity),
                    typeof(MusicRecordingEntity)));

            app.MapPost("/Music/search", async (MusicReleaseQuery query, IMusicStore store, CancellationToken ct) => Results.Ok(await store.SearchReleasesAsync(query, ct)))
                .WithTags("Music")
                .AllowAnonymous();
            app.MapPost("/Music/lyrics/search", async (MusicLyricsQuery query, IMusicStore store, CancellationToken ct) => Results.Ok(await store.SearchLyricsAsync(query, ct)))
                .WithTags("Music")
                .AllowAnonymous();
            app.MapGet("/Music/track/{id:guid}/lyrics", async (Guid id, IMusicStore store, CancellationToken ct) => {
                try {
                    return Results.Ok(await store.GetTrackLyricsAsync(id, ct));
                }
                catch (NotFoundException) {
                    return Results.NotFound();
                }
            }).WithTags("Music").AllowAnonymous();
            app.MapPut("/Music/track/{id:guid}/lyrics", async (Guid id, CatalogTextBody body, IMusicStore store, CancellationToken ct) => {
                await store.SaveTrackLyricsAsync(id, body.Text, ct);
                return Results.NoContent();
            }).WithTags("Music").AllowAnonymous();
            return app;
        }

        public WebApplication BuildVideoGroup()
        {
            app.MapDynamicCrudEndpoints<VideoDbContext>(c => c.AllowAnonymous()
                .WithDefaults(d => d.BaseRoute = "Video")
                .DenySelectFields("Transcript", "SearchVector")
                .IncludeOnly(
                    typeof(VideoTitleEntity),
                    typeof(VideoSeasonEntity),
                    typeof(VideoEpisodeEntity),
                    typeof(VideoCreditEntity),
                    typeof(VideoTitleIdentifierEntity),
                    typeof(VideoEpisodeIdentifierEntity),
                    typeof(VideoYoutubeLinkEntity),
                    typeof(VideoGenreEntity),
                    typeof(VideoTitleGenreEntity),
                    typeof(VideoAlternateTitleEntity),
                    typeof(VideoCollectionEntity),
                    typeof(VideoCutEntity)));

            app.MapPost("/Video/search", async (VideoTitleQuery query, IVideoStore store, CancellationToken ct) => Results.Ok(await store.SearchTitlesAsync(query, ct)))
                .WithTags("Video")
                .AllowAnonymous();
            app.MapPost("/Video/transcripts/search", async (VideoTranscriptQuery query, IVideoStore store, CancellationToken ct) => Results.Ok(await store.SearchTranscriptsAsync(query, ct)))
                .WithTags("Video")
                .AllowAnonymous();
            app.MapGet("/Video/title/{id:guid}/transcript", async (Guid id, IVideoStore store, CancellationToken ct) => {
                try {
                    return Results.Ok(await store.GetTitleTranscriptAsync(id, ct));
                }
                catch (NotFoundException) {
                    return Results.NotFound();
                }
            }).WithTags("Video").AllowAnonymous();
            app.MapPut("/Video/title/{id:guid}/transcript", async (Guid id, CatalogTextBody body, IVideoStore store, CancellationToken ct) => {
                await store.SaveTitleTranscriptAsync(id, body.Text, ct);
                return Results.NoContent();
            }).WithTags("Video").AllowAnonymous();
            app.MapGet("/Video/episode/{id:guid}/transcript", async (Guid id, IVideoStore store, CancellationToken ct) => {
                try {
                    return Results.Ok(await store.GetEpisodeTranscriptAsync(id, ct));
                }
                catch (NotFoundException) {
                    return Results.NotFound();
                }
            }).WithTags("Video").AllowAnonymous();
            app.MapPut("/Video/episode/{id:guid}/transcript", async (Guid id, CatalogTextBody body, IVideoStore store, CancellationToken ct) => {
                await store.SaveEpisodeTranscriptAsync(id, body.Text, ct);
                return Results.NoContent();
            }).WithTags("Video").AllowAnonymous();
            return app;
        }

        public WebApplication BuildPersonGroup()
        {
            //app.MapDynamicCrudEndpoints<PeopleDbContext>(c => c.BaseRoute = "Person");
            app.CreateBuilder<PeopleDbContext, PersonEntity, PersonReq, PersonRes, Guid>(Constants.Person.Route, "Person")
                .WithCrud(crud => crud.WithFlags(ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance)
                    .BeforeCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres())
                    .AfterCreate(ctx => {
                        var sourceType = string.IsNullOrWhiteSpace(ctx.Request.Source) ? PeopleSourceTypes.Manual : ctx.Request.Source;
                        ctx.Entity.SourceEntityType = sourceType;
                        ctx.Entity.SourceEntityId = ctx.Entity.Id.ToString();
                        ctx.Entity.ImportedAt = DateTime.UtcNow;
                    }))
                .WithMetadata(m => m.IncludeEntityMetadata())
                .WithProjectionComputedFields()
                .Build();

            var contactReadFeatures = ApiFeatureSet.ReadOnly + ExportApiFeature.Instance;
            app.CreateReadOnlyBuilder<PeopleDbContext, AddressEntity, AddressEntity>(Constants.Person.Address, "Person").WithCrud(contactReadFeatures, new()).Build();
            app.CreateReadOnlyBuilder<PeopleDbContext, PhoneNumberEntity, PhoneNumberEntity>(Constants.Person.PhoneNumber, "Person").WithCrud(contactReadFeatures, new()).Build();
            app.CreateReadOnlyBuilder<PeopleDbContext, EmailAddressEntity, EmailAddressEntity>(Constants.Person.Email, "Person").WithCrud(contactReadFeatures, new()).Build();

            // Typed Person CRUD covers /Person/*; the root From/Joins Query (Option A) is POST /Query.
            app.MapRootQueryEndpoints<PeopleDbContext>(auth: EndpointAuth.Anonymous());
            app.MapGet(
                    "info/{schema}/{table}/{column}/GetUniqueCounts", async (
                        string schema, string table, string column, int? start, int? amount, string? containsFilter, ISprocService sproc, CancellationToken ct) => {
                        var parameters = new Dictionary<string, object?> {
                            ["p_schema_name"] = schema,
                            ["p_table_name"] = table,
                            ["p_column_name"] = column,
                            ["p_contains_filter"] = containsFilter,
                            ["p_start"] = start ?? 0,
                            ["p_amount"] = amount
                        };

                        var results = await sproc.ExecuteStoredProcAsync<SpUniqueValueCount>(StoredProcedures.Info.UniqueValuesWithCount, parameters, ct: ct);
                        return Results.Ok(results);
                    })
                .WithTags("Info");

            return app;
        }

        private WebApplication BuildEndatoPsGroup()
        {
            app.MapDynamicCrudEndpoints<EndatoDbContext>(c => c.AllowAnonymous()
                .WithDefaults(d => {
                    d.BaseRoute = Constants.EndatoPs.Route;
                    d.Features = ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance;
                })
                .IncludeOnly<EndatoPsPersonEntity, EndatoPsAddressEntity, EndatoPsPhoneNumberEntity, EndatoPsEmailAddressEntity>());

            return app;
        }

        private WebApplication BuildEndatoCeGroup()
        {
            app.MapDynamicCrudEndpoints<EndatoDbContext>(c => c.AllowAnonymous()
                .WithDefaults(d => {
                    d.BaseRoute = Constants.EndatoCe.Route;
                    d.Features = ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance;
                })
                .IncludeOnly<EndatoCePersonEntity, EndatoCeAddressEntity, EndatoCePhoneNumberEntity, EndatoCeEmailAddressEntity>());

            return app;
        }

        private WebApplication BuildTwilioGroup()
        {
            app.MapDynamicCrudEndpoints<TwilioSmsDbContext>(c => {
                c.BaseRoute = "Twilio";
                c.AllowAnonymous();
            });

            //app.CreateBuilder<TwilioSmsDbContext, TwilioSmsLogEntity, TwilioSmsLogEntity, TwilioSmsLogEntity, string>(Constants.Twilio.SmsLog, "Twilio")
            //    .WithCrudAndBulk(i => i.Id)
            //    .Build();
            return app;
        }
    }
}