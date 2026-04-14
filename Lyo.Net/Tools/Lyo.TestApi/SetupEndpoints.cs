using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Services.Crud;
using Lyo.Discord.Postgres;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.People.Postgres.Database;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.TestApi.FileStorageWorkbench;
using Lyo.TestApi.Person.Request;
using Lyo.TestApi.Person.Response;
using Lyo.Web.Components.UniqueValueSelector;
using UUIDNext;

namespace Lyo.TestApi;

public static class SetupEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication SetupCourtCanaryEndpoints()
        {
            app = app
                //.BuildJobGroup()
                //.BuildClientGroup()
                //.BuildDocketGroup()
                //.BuildEndatoCeGroup()
                //.BuildEndatoPsGroup()
                .BuildPersonGroup()
                .BuildDiscordGroup()
                //.BuildRecipientGroup()
                .BuildTwilioGroup()
                .BuildFileStorageWorkbenchGroup()
                .BuildDirectFileUploadEndpoint()
                .BuildFileStorageWorkbenchFileMetadataQuery();

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
                .WithCrud(crud => crud
                    .WithFlags(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate)
                    .BeforeCreate(ctx => ctx.Entity.Id = Uuid.NewDatabaseFriendly(Database.PostgreSql)))
                .WithMetadata()
                .WithProjectionComputedFields()
                .Build();
            
            

            app.MapGet(
                    "info/{schema}/{table}/{column}/GetUniqueCounts", async (
                        string schema,
                        string table,
                        string column,
                        int? start,
                        int? amount,
                        string? containsFilter,
                        ISprocService<PeopleDbContext> sproc,
                        CancellationToken ct) => {
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