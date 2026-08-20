using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Export;
using Lyo.Api.Reporting;
using Lyo.Api.Services.Crud;
using Lyo.Common.Identifiers;
using Lyo.Discord.Postgres;
using Lyo.Endato.Postgres.Database;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Abstractions;
using Lyo.Job.Postgres;
using Lyo.People.Models;
using Lyo.People.Postgres.Database;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.TestApi.FileStorageWorkbench;
using Lyo.TestApi.Person.Request;
using Lyo.TestApi.Person.Response;

namespace Lyo.TestApi;

public static class SetupEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication SetupCourtCanaryEndpoints()
        {
            app = app.BuildJobGroup()
                //.BuildClientGroup()
                //.BuildDocketGroup()
                // Match Job test host: open for Gateway workbench (auth can be tightened later).
                // Stream persisted outputs from this host's keyed FileStorage (matches the AfterRender save hook).
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
                .BuildFileStorageWorkbenchGroup()
                .BuildDirectFileUploadEndpoint()
                .BuildFileStorageWorkbenchFileMetadataQuery();

            app.MapCacheEndpoints("Cache", b => b.AllowAnonymous());
            return app;
        }

        private WebApplication BuildFileStorageWorkbenchFileMetadataQuery()
        {
            app.CreateReadOnlyBuilder<FileMetadataStoreDbContext, FileMetadataEntity, FileMetadataEntity, string>(Constants.FileStorageWorkbench.FileMetadata, "FileMetadata")
                .AllowAnonymous()
                .WithReadOnlyEndpoints()
                .Build();

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

            // Typed Person CRUD owns /Person/*; root From/Joins Query is Option A at POST /Query.
            app.MapRootQueryEndpoints<PeopleDbContext>();
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
            app.MapDynamicCrudEndpoints<EndatoDbContext>(c => c.WithDefaults(d => {
                    d.BaseRoute = Constants.EndatoPs.Route;
                    d.Features = ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance;
                })
                .IncludeOnly<EndatoPsPersonEntity, EndatoPsAddressEntity, EndatoPsPhoneNumberEntity, EndatoPsEmailAddressEntity>());

            return app;
        }

        private WebApplication BuildEndatoCeGroup()
        {
            app.MapDynamicCrudEndpoints<EndatoDbContext>(c => c.WithDefaults(d => {
                    d.BaseRoute = Constants.EndatoCe.Route;
                    d.Features = ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance;
                })
                .IncludeOnly<EndatoCePersonEntity, EndatoCeAddressEntity, EndatoCePhoneNumberEntity, EndatoCeEmailAddressEntity>());

            return app;
        }

        private WebApplication BuildTwilioGroup()
        {
            app.MapDynamicCrudEndpoints<TwilioSmsDbContext>(c => c.BaseRoute = "Twilio");

            //app.CreateBuilder<TwilioSmsDbContext, TwilioSmsLogEntity, TwilioSmsLogEntity, TwilioSmsLogEntity, string>(Constants.Twilio.SmsLog, "Twilio")
            //    .WithCrudAndBulk(i => i.Id)
            //    .Build();
            return app;
        }
    }
}