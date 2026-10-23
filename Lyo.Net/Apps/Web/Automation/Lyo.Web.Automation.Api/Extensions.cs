using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Web.Automation.Api;

/// <summary>Registers the in-memory run registry and maps <c>/Automation</c>.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the run registry with the declared defaults. The host still registers <c>IAutomationBrowserSessionFactory</c> and <c>IAutomationPlanRunner</c>.</summary>
        public IServiceCollection AddAutomationApi()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddAutomationApi(new AutomationApiOptions());
        }

        /// <summary>Adds the run registry from <paramref name="configure" />. The host still registers <c>IAutomationBrowserSessionFactory</c> and <c>IAutomationPlanRunner</c>.</summary>
        public IServiceCollection AddAutomationApi(Action<AutomationApiOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new AutomationApiOptions();
            configure(options);
            return services.AddAutomationApi(options);
        }

        /// <summary>Adds the run registry. Missing configuration keeps the declared defaults.</summary>
        public IServiceCollection AddAutomationApiFromConfiguration(IConfiguration configuration, string sectionName = AutomationApiOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new AutomationApiOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddAutomationApi(options);
        }

        /// <summary>Adds the run registry from an options instance.</summary>
        public IServiceCollection AddAutomationApi(AutomationApiOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.TryAddSingleton<IAutomationPlanFileStorage, MissingPlanFileStorage>();
            services.AddSingleton<AutomationRunRegistry>();
            services.AddSingleton<AutomationPlanRegistry>();
            services.TryAddSingleton<IAutomationPlanStore>(sp => sp.GetRequiredService<AutomationPlanRegistry>());
            services.AddHostedService(sp => sp.GetRequiredService<AutomationRunRegistry>());
            return services;
        }
    }

    /// <summary>Maps start, get, cancel, log, and snapshot routes. Call after <c>AddAutomationApi</c>.</summary>
    public static WebApplication BuildAutomationGroup(this WebApplication app)
    {
        ArgumentHelpers.ThrowIfNull(app);
        var tag = Constants.Rest.Route;
        var group = app.MapGroup("/" + tag).WithTags(tag);
        group.MapPost(
                "/Run", (AutomationRunReq? request, AutomationRunRegistry runs) => {
                    if (request?.Plan is null || request.Plan.Steps is null)
                        return Results.Problem(title: "Plan is required.", statusCode: StatusCodes.Status400BadRequest);

                    var accepted = runs.TryStart(request);
                    return accepted is null
                        ? Results.Problem(title: "An automation run is already active.", statusCode: StatusCodes.Status429TooManyRequests)
                        : Results.Accepted(null, accepted);
                })
            .WithName("StartAutomationRun")
            .AllowAnonymous();

        group.MapGet(
                "/Run/{id:guid}", (Guid id, AutomationRunRegistry runs) => {
                    var run = runs.Get(id);
                    return run is null ? Results.NotFound() : Results.Ok(run);
                })
            .WithName("GetAutomationRun")
            .AllowAnonymous();

        group.MapPost("/Run/{id:guid}/Cancel", (Guid id, AutomationRunRegistry runs) => runs.Cancel(id) ? Results.Ok() : Results.NotFound())
            .WithName("CancelAutomationRun")
            .AllowAnonymous();

        group.MapGet(
                "/Run/{id:guid}/Log", (Guid id, AutomationRunRegistry runs) => {
                    var path = runs.LogPath(id);
                    return path is null ? Results.NotFound() : Results.Text(File.ReadAllText(path), "text/plain");
                })
            .WithName("GetAutomationRunLog")
            .AllowAnonymous();

        group.MapGet(
                "/Run/{id:guid}/Snapshots", (Guid id, AutomationRunRegistry runs) => {
                    var names = runs.ListSnapshots(id);
                    return names is null ? Results.NotFound() : Results.Ok(names);
                })
            .WithName("ListAutomationRunSnapshots")
            .AllowAnonymous();

        group.MapGet(
                "/Run/{id:guid}/Steps/{index:int}/Screenshot", async (Guid id, int index, AutomationRunRegistry runs, IAutomationPlanFileStorage storage, CancellationToken ct) => {
                    var fileId = runs.ScreenshotFileId(id, index);
                    if (string.IsNullOrWhiteSpace(fileId))
                        return Results.NotFound();

                    var bytes = await storage.ReadSnapshotAsync(fileId, ct).ConfigureAwait(false);
                    return bytes is null ? Results.NotFound() : Results.File(bytes, "image/png");
                })
            .WithName("GetAutomationRunStepScreenshot")
            .AllowAnonymous();

        group.MapGet(
                "/Run/{id:guid}/Snapshot/{name}", (Guid id, string name, AutomationRunRegistry runs) => {
                    if (!AutomationSnapshotNames.IsSingleFileName(name))
                        return Results.Problem(title: "Snapshot name is not a single file name.", statusCode: StatusCodes.Status400BadRequest);

                    var path = runs.SnapshotPath(id, name);
                    return path is null ? Results.NotFound() : Results.File(File.ReadAllBytes(path), "image/png");
                })
            .WithName("GetAutomationRunSnapshot")
            .AllowAnonymous();

        group.MapGet("/Plan", (IAutomationPlanStore plans) => Results.Ok(plans.List())).WithName("ListAutomationPlans").AllowAnonymous();
        group.MapGet(
                "/Plan/id/{id:guid}", (Guid id, IAutomationPlanStore plans) => {
                    var plan = plans.GetById(id);
                    return plan is null ? Results.NotFound() : Results.Ok(plan);
                })
            .WithName("GetAutomationPlanById")
            .AllowAnonymous();

        group.MapPost(
                "/Plan", (AutomationPlanCreateReq? request, IAutomationPlanStore plans) => {
                    if (!AutomationPlanNames.IsSingleSegment(request?.Name))
                        return Results.Problem(title: "Plan name must be one segment.", statusCode: StatusCodes.Status400BadRequest);

                    var plan = plans.Create(request!.Name!);
                    return Results.Created("/" + Constants.Rest.Plan(plan.Name!), plan);
                })
            .WithName("CreateAutomationPlan")
            .AllowAnonymous();

        group.MapGet(
                "/Plan/{name}", (string name, IAutomationPlanStore plans) => {
                    if (!AutomationPlanNames.IsSingleSegment(name))
                        return Results.Problem(title: "Plan name must be one segment.", statusCode: StatusCodes.Status400BadRequest);

                    var plan = plans.GetByName(name);
                    return plan is null ? Results.NotFound() : Results.Ok(plan);
                })
            .WithName("GetAutomationPlan")
            .AllowAnonymous();

        group.MapPut(
                "/Plan/{name}", async (string name, HttpRequest request, IAutomationPlanStore plans, CancellationToken ct) => {
                    if (!AutomationPlanNames.IsSingleSegment(name))
                        return Results.Problem(title: "Plan name must be one segment.", statusCode: StatusCodes.Status400BadRequest);

                    using var reader = new StreamReader(request.Body);
                    var parsed = AutomationPlanRegistry.Parse(await reader.ReadToEndAsync(ct));
                    if (parsed is null)
                        return Results.Problem(title: "Plan JSON needs a steps array.", statusCode: StatusCodes.Status400BadRequest);

                    return plans.TryReplace(name, parsed) ? Results.Ok(plans.GetByName(name)) : Results.NotFound();
                })
            .WithName("SaveAutomationPlan")
            .AllowAnonymous();

        return app;
    }
}

/// <summary>Storage stand-in used until the host registers a real <see cref="IAutomationPlanFileStorage" />.</summary>
internal sealed class MissingPlanFileStorage : IAutomationPlanFileStorage
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([]);

    /// <inheritdoc />
    public Task<string?> SaveSnapshotAsync(byte[] png, Guid runId, Guid stepId, CancellationToken ct) => Task.FromResult<string?>(null);

    /// <inheritdoc />
    public Task<string?> SaveAsync(byte[] data, string fileName, string pathPrefix, string contentType, CancellationToken ct) => Task.FromResult<string?>(null);

    /// <inheritdoc />
    public Task<byte[]?> ReadSnapshotAsync(string fileId, CancellationToken ct) => Task.FromResult<byte[]?>(null);
}