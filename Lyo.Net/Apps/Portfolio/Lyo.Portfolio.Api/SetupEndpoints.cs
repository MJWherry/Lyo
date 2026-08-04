using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Export;
using Lyo.Api.Reporting;
using Lyo.Api.Services.Crud;
using Lyo.Common.Identifiers;
using Lyo.Config.Api;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Abstractions;
using Lyo.Job.Postgres;
using Lyo.People.Models;
using Lyo.People.Postgres.Database;
using Lyo.Portfolio.Api.FileStorageWorkbench;
using Lyo.Portfolio.Api.Person.Request;
using Lyo.Portfolio.Api.Person.Response;

namespace Lyo.Portfolio.Api;

/// <summary>HTTP surface for the portfolio API host.</summary>
public static class SetupEndpoints
{
    extension(WebApplication app)
    {
        /// <summary>Maps Job, Reporting, Person, Config, and file-storage workbench endpoints.</summary>
        public WebApplication SetupPortfolioEndpoints()
        {
            app = app.BuildJobGroup()
                .BuildReportingGroup(
                    ReportingApiOptions.WithAuth(
                        EndpointAuth.Anonymous(), (ctx, ct) => {
                            var storage = ctx.Services.GetRequiredKeyedService<IFileStorageService>(PortfolioRoutes.FileStorageWorkbench.ServiceKey);
                            return storage.GetFileStreamAsync(ctx.OutputFileId, ct: ct);
                        }))
                .BuildPersonGroup()
                .BuildFileStorageWorkbenchGroup()
                .BuildDirectFileUploadEndpoint()
                .BuildFileStorageWorkbenchFileMetadataQuery();

            app.MapConfigApiEndpoints();
            return app;
        }

        private WebApplication BuildFileStorageWorkbenchFileMetadataQuery()
        {
            app.CreateReadOnlyBuilder<FileMetadataStoreDbContext, FileMetadataEntity, FileMetadataEntity, string>(PortfolioRoutes.FileStorageWorkbench.FileMetadata, "FileMetadata")
                .AllowAnonymous()
                .WithReadOnlyEndpoints()
                .Build();

            return app;
        }

        /// <summary>Typed Person CRUD + root <c>POST /Query</c>.</summary>
        public WebApplication BuildPersonGroup()
        {
            app.CreateBuilder<PeopleDbContext, PersonEntity, PersonReq, PersonRes, Guid>(PortfolioRoutes.Person.Route, "Person")
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
            app.CreateReadOnlyBuilder<PeopleDbContext, AddressEntity, AddressEntity>(PortfolioRoutes.Person.Address, "Person").WithCrud(contactReadFeatures, new()).Build();
            app.CreateReadOnlyBuilder<PeopleDbContext, PhoneNumberEntity, PhoneNumberEntity>(PortfolioRoutes.Person.PhoneNumber, "Person").WithCrud(contactReadFeatures, new()).Build();
            app.CreateReadOnlyBuilder<PeopleDbContext, EmailAddressEntity, EmailAddressEntity>(PortfolioRoutes.Person.Email, "Person").WithCrud(contactReadFeatures, new()).Build();

            app.MapRootQueryEndpoints<PeopleDbContext>();
            app.MapGet(
                    "info/{schema}/{table}/{column}/GetUniqueCounts", async (
                        string schema,
                        string table,
                        string column,
                        int? start,
                        int? amount,
                        string? containsFilter,
                        ISprocService sproc,
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
    }
}
