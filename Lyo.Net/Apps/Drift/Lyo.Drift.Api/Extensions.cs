using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Models.Common.Response;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;
using Lyo.Drift.Postgres;
using Lyo.Drift.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using DriftRoutes = Lyo.Drift.Models.Constants.Rest.Drift;

namespace Lyo.Drift.Api;

/// <summary>HTTP mapping for the Drift collector API. Register store DI with <c>AddPostgresDriftManagement</c> first.</summary>
public static class Extensions
{
    /// <summary>Maps Drift ingest and query endpoints. Invoke after <c>AddPostgresDriftManagement</c>.</summary>
    public static WebApplication BuildDriftGroup(this WebApplication app)
    {
        app.CreateBuilder<DriftDbContext, DriftInstance, DriftInstanceReq, DriftInstanceRes, Guid>(DriftRoutes.Instances, "Drift")
            .WithCrud(
                ApiFeatureSet.ReadOnly + ApiFeature.Patch + ApiFeature.Delete, new() {
                    BeforePatch = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow
                })
            .Build();

        app.CreateBuilder<DriftDbContext, DriftStructureSnapshot, DriftSnapshotReq, DriftSnapshotRes, Guid>(DriftRoutes.Snapshots, "Drift")
            .WithQuery()
            .WithGet()
            .Build();

        app.CreateBuilder<DriftDbContext, DriftDiffSnapshot, DriftDiffReq, DriftDiffRes, Guid>(DriftRoutes.Diffs, "Drift")
            .WithQuery()
            .WithGet()
            .Build();

        app.CreateBuilder<DriftDbContext, DriftChangeEvent, DriftChangeBatchReq, DriftChangeRes, Guid>(DriftRoutes.Changes, "Drift")
            .WithQuery()
            .WithGet()
            .Build();

        app.MapPost(
                $"/{DriftRoutes.InstanceUpsert}", async (DriftInstanceReq req, DriftService drift, CancellationToken ct) => {
                    var result = await drift.UpsertInstanceAsync(req, ct).ConfigureAwait(false);
                    return Results.Ok(ResultFactory.CreateSuccess(result));
                })
            .WithTags("Drift")
            .WithName("UpsertDriftInstance");

        app.MapPatch(
                $"/{DriftRoutes.Instances}/{{id:guid}}/Heartbeat", async (Guid id, DriftInstanceHeartbeatReq? req, DriftService drift, CancellationToken ct) => {
                    var result = await drift.HeartbeatAsync(id, req, ct).ConfigureAwait(false);
                    return Results.Ok(result);
                })
            .WithTags("Drift")
            .WithName("HeartbeatDriftInstance");

        app.MapPost(
                $"/{DriftRoutes.Instances}/{{id:guid}}/Stop", async (Guid id, DriftService drift, CancellationToken ct) => {
                    var result = await drift.StopAsync(id, ct).ConfigureAwait(false);
                    return Results.Ok(result);
                })
            .WithTags("Drift")
            .WithName("StopDriftInstance");

        app.MapPost(
                $"/{DriftRoutes.Snapshots}", async (DriftSnapshotReq req, DriftService drift, CancellationToken ct) => {
                    var result = await drift.SaveSnapshotAsync(req, ct).ConfigureAwait(false);
                    return Results.Ok(ResultFactory.CreateSuccess(result));
                })
            .WithTags("Drift")
            .WithName("IngestDriftSnapshot");

        app.MapPost(
                $"/{DriftRoutes.Diffs}", async (DriftDiffReq req, DriftService drift, CancellationToken ct) => {
                    var result = await drift.SaveDiffAsync(req, ct).ConfigureAwait(false);
                    return Results.Ok(ResultFactory.CreateSuccess(result));
                })
            .WithTags("Drift")
            .WithName("IngestDriftDiff");

        app.MapPost(
                $"/{DriftRoutes.Changes}", async (DriftChangeBatchReq req, DriftService drift, CancellationToken ct) => {
                    var result = await drift.SaveChangesAsync(req, ct).ConfigureAwait(false);
                    return Results.Ok(result);
                })
            .WithTags("Drift")
            .WithName("IngestDriftChanges");

        app.MapPost(
                $"/{DriftRoutes.Snapshots}/{{id:guid}}/DiffAgainst/{{otherId:guid}}", async (Guid id, Guid otherId, DriftService drift, CancellationToken ct) => {
                    var result = await drift.DiffAgainstAsync(id, otherId, ct).ConfigureAwait(false);
                    return Results.Ok(ResultFactory.CreateSuccess(result));
                })
            .WithTags("Drift")
            .WithName("DiffDriftSnapshots");

        return app;
    }
}
