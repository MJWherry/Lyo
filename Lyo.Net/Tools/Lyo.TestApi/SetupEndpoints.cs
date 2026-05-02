using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Services.Crud;
using Lyo.Common.Identifiers;
using Lyo.Discord.Postgres;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres;
using Lyo.People.Postgres.Database;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.TestApi.FileStorageWorkbench;
using Lyo.TestApi.Person.Request;
using Lyo.TestApi.Person.Response;
using Lyo.Web.Components.UniqueValueSelector;
using Microsoft.AspNetCore.Mvc;

namespace Lyo.TestApi;

public static class SetupEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication SetupCourtCanaryEndpoints()
        {
            app = app.BuildJobGroup()
                .BuildJobServiceEndpoints()
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

        /// <summary>
        /// Custom endpoints that delegate to <see cref="JobService" /> so the gateway can trigger and cancel runs (the standard CRUD route for JobRun does not expose Create/Cancel —
        /// those require MQ).
        /// </summary>
        private WebApplication BuildJobServiceEndpoints()
        {
            app.MapPost(
                    "Job/Run/Create", async ([FromBody] JobRunReq req, JobService jobService, CancellationToken ct) => {
                        var result = await jobService.CreateJobRun(req, ct);
                        return result.IsSuccess ? Results.Created($"Job/Run/{result.Data!.Id}", result.Data) : Results.BadRequest(result.Error);
                    })
                .WithTags("Job")
                .WithName("CreateJobRun");

            app.MapPost(
                    "Job/Run/{id}/Cancel", async (Guid id, JobService jobService) => {
                        var (run, error) = await jobService.CancelJobRun(id);
                        return error is null ? Results.Ok(run) : Results.BadRequest(error);
                    })
                .WithTags("Job")
                .WithName("CancelJobRun");

            app.MapPost(
                    "Job/Run/{id}/Rerun", async (Guid id, JobService jobService, CancellationToken ct) => {
                        var result = await jobService.RerunJob(id);
                        return result is { IsSuccess: true } ? Results.Ok(result.Data) : Results.BadRequest(result?.Error);
                    })
                .WithTags("Job")
                .WithName("RerunJob");

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
                .WithCrud(crud => crud.WithFlags(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate)
                    .BeforeCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres()))
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