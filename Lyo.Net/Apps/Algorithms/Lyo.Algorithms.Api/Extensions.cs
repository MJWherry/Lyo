using Lyo.Algorithms;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Lyo.Algorithms.Models.Response;
using Lyo.Algorithms.Postgres;
using Lyo.Exceptions;
using Lyo.Result;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Api;

/// <summary>HTTP mapping for the Lyo algorithm catalog. Register with <c>AddAlgorithmsApiFromConfiguration</c> before <c>BuildAlgorithmsGroup</c>.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Postgres catalog and the in-process registry so <see cref="IAlgorithmCatalog" /> can validate writes.</summary>
        public IServiceCollection AddAlgorithmsApi(Action<PostgresAlgorithmOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAlgorithmOptions();
            configure(options);
            return services.AddAlgorithmsApi(options);
        }

        /// <summary>Registers the catalog from configuration section <see cref="PostgresAlgorithmOptions.SectionName" />.</summary>
        public IServiceCollection AddAlgorithmsApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresAlgorithmOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresAlgorithmOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddAlgorithmsApi(options);
        }

        /// <summary>Registers the Postgres catalog and <c>AddAlgorithms</c>.</summary>
        public IServiceCollection AddAlgorithmsApi(PostgresAlgorithmOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresAlgorithmStore(options);
            services.AddAlgorithms();
            return services;
        }
    }

    /// <summary>Maps catalog list, get, revision, revert, and label routes. Invoke after <c>AddAlgorithmsApi</c>.</summary>
    public static WebApplication BuildAlgorithmsGroup(this WebApplication app)
    {
        var tag = Constants.Rest.Algorithms.Route;
        app.MapGet(
                "/" + Constants.Rest.Algorithms.Route, async (IAlgorithmDefinitionStore store, CancellationToken ct) => Results.Ok(await store.ListAsync(ct).ConfigureAwait(false)))
            .WithTags(tag)
            .WithName("ListAlgorithms")
            .AllowAnonymous();

        app.MapGet(
                "/" + Constants.Rest.Algorithms.Route + "/{key}", async (string key, IAlgorithmDefinitionStore store, CancellationToken ct) => {
                    var row = await store.GetByKeyAsync(key, ct).ConfigureAwait(false);
                    return row is null ? Results.NotFound() : Results.Ok(row);
                })
            .WithTags(tag)
            .WithName("GetAlgorithm")
            .AllowAnonymous();

        app.MapGet(
                "/" + Constants.Rest.Algorithms.Route + "/{key}/revisions",
                async (string key, IAlgorithmDefinitionStore store, CancellationToken ct) => Results.Ok(await store.GetRevisionsAsync(key, ct).ConfigureAwait(false)))
            .WithTags(tag)
            .WithName("ListAlgorithmRevisions")
            .AllowAnonymous();

        app.MapPost(
                "/" + Constants.Rest.Algorithms.Route + "/{key}/revisions", async (string key, AlgorithmRevisionReq req, IAlgorithmCatalog catalog, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    return ToHttp(await catalog.SaveRevisionAsync(key, req, ct).ConfigureAwait(false));
                })
            .WithTags(tag)
            .WithName("SaveAlgorithmRevision")
            .AllowAnonymous();

        app.MapPost(
                "/" + Constants.Rest.Algorithms.Route + "/{key}/revert", async (string key, AlgorithmRevertReq req, IAlgorithmCatalog catalog, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    return ToHttp(await catalog.RevertToRevisionAsync(key, req, ct).ConfigureAwait(false));
                })
            .WithTags(tag)
            .WithName("RevertAlgorithm")
            .AllowAnonymous();

        app.MapGet(
                "/" + Constants.Rest.Algorithms.Route + "/{key}/labels/{label}", async (string key, string label, IAlgorithmDefinitionStore store, CancellationToken ct) => {
                    var row = await store.GetLabelAsync(key, label, ct).ConfigureAwait(false);
                    return row is null ? Results.NotFound() : Results.Ok(row);
                })
            .WithTags(tag)
            .WithName("GetAlgorithmLabel")
            .AllowAnonymous();

        app.MapPut(
                "/" + Constants.Rest.Algorithms.Route + "/{key}/labels/{label}", async (
                    string key, string label, AlgorithmLabelReq req, IAlgorithmCatalog catalog, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    return ToHttp(await catalog.SetLabelAsync(key, label, req, ct).ConfigureAwait(false));
                })
            .WithTags(tag)
            .WithName("SetAlgorithmLabel")
            .AllowAnonymous();

        app.MapDelete(
                "/" + Constants.Rest.Algorithms.Route + "/{key}", async (string key, IAlgorithmCatalog catalog, CancellationToken ct) => {
                    var result = await catalog.DeleteAsync(key, ct).ConfigureAwait(false);
                    if (!result.IsSuccess)
                        return Results.BadRequest(result.Errors?.FirstOrDefault()?.Message ?? "Request failed.");

                    return Results.NoContent();
                })
            .WithTags(tag)
            .WithName("DeleteAlgorithm")
            .AllowAnonymous();

        app.MapPost(
                "/" + Constants.Rest.Algorithms.Preview, async (AlgorithmPreviewReq req, IAlgorithmPreview preview, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    try {
                        var result = await preview.PreviewAsync(new() { Kind = req.Kind, Source = req.Source, Payload = req.Payload }, req.Context, req.Key, ct)
                            .ConfigureAwait(false);
                        return Results.Ok(ToPreview(result));
                    }
                    catch (AlgorithmException ex) {
                        return Results.BadRequest(ex.Message);
                    }
                    catch (ArgumentException ex) {
                        return Results.BadRequest(ex.Message);
                    }
                })
            .WithTags(tag)
            .WithName("PreviewAlgorithm")
            .AllowAnonymous();

        return app;

        static IResult ToHttp<T>(Result<T> result)
            where T : class
        {
            if (!result.IsSuccess || result.Data is null)
                return Results.BadRequest(result.Errors?.FirstOrDefault()?.Message ?? "Request failed.");

            return Results.Ok(result.Data);
        }

        static AlgorithmPreviewRes ToPreview(AlgorithmPreviewResult result)
            => new() { Value = result.Value, Result = result.Result, Steps = result.Steps.Select(ToStep).ToList() };

        static AlgorithmStepRes ToStep(AlgorithmStep step)
            => new() {
                Key = step.Key,
                Kind = step.Kind,
                Input = step.Input,
                Points = step.Points,
                Value = step.Value,
                Children = step.Children.Select(ToStep).ToList()
            };
    }
}