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
using Lyo.Comic.Api;
using Lyo.HomeInventory.Api;
using Lyo.Job.Api;
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
                .BuildDriftGroup()
                .BuildComicGroup()
                .BuildHomeInventoryGroup()
                //.BuildClientGroup()
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