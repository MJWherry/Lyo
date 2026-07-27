using Lyo.Api.Models.Builders;
using Lyo.Config;
using Lyo.Discord.Models;
using Lyo.Discord.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Discord.Postgres;

/// <summary>Minimal API routes for per-guild bot settings (config store). Add authorization at the host when exposing this API.</summary>
public static class DiscordGuildSettingsEndpoints
{
    /// <summary>Maps GET/PUT <c>Discord/Guild/{guildId}/GuildSettings</c>. No-op when <see cref="IConfigStore" /> is not registered.</summary>
    public static WebApplication MapDiscordGuildSettingsEndpoints(this WebApplication app)
    {
        if (app.Services.GetService(typeof(IConfigStore)) == null)
            return app;

        app.MapGet(
                $"{Constants.Rest.Discord.Guilds}/{{guildId:long}}/GuildSettings", async Task<IResult> (long guildId, [FromServices] IConfigStore store, CancellationToken ct) => {
                    var settings = await DiscordGuildSettingsHelper.GetSettingsAsync(store, guildId, ct).ConfigureAwait(false);
                    return Results.Ok(settings);
                })
            .WithTags(Constants.Rest.Discord.Route)
            .WithName("GetDiscordGuildSettings");

        app.MapPut(
                $"{Constants.Rest.Discord.Guilds}/{{guildId:long}}/GuildSettings", async Task<IResult> (
                    long guildId,
                    [FromBody] DiscordGuildSettings body,
                    [FromServices] IConfigStore store,
                    [FromServices] IDbContextFactory<DiscordDbContext> dbFactory,
                    CancellationToken ct) => {
                    await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                    if (await db.DiscordGuilds.FindAsync([guildId], ct).ConfigureAwait(false) == null)
                        return Results.NotFound(LyoProblemDetailsBuilder.CreateWithActivity().NotFound("Discord guild", guildId.ToString()).Build());

                    await DiscordGuildSettingsHelper.EnsureDefaultBindingAsync(store, guildId, ct).ConfigureAwait(false);
                    body.Revision = null;
                    body.NormalizeForPersistence();
                    var r = DiscordGuildSettingsHelper.GuildRef(guildId);
                    await store.SaveBindingAsync(
                            new() {
                                Key = DiscordGuildSettingsHelper.Key,
                                SubjectEntityType = r.EntityType,
                                SubjectEntityId = r.EntityId,
                                Value = ConfigValue.From(body)
                            }, null, ct)
                        .ConfigureAwait(false);

                    var updated = await DiscordGuildSettingsHelper.GetSettingsAsync(store, guildId, ct).ConfigureAwait(false);
                    return Results.Ok(updated);
                })
            .WithTags(Constants.Rest.Discord.Route)
            .WithName("PutDiscordGuildSettings");

        app.MapGet(
                $"{Constants.Rest.Discord.Guilds}/{{guildId:long}}/GuildSettings/Revisions",
                async Task<IResult> (long guildId, [FromServices] IConfigStore store, CancellationToken ct)
                    => {
                    await DiscordGuildSettingsHelper.EnsureDefaultBindingAsync(store, guildId, ct).ConfigureAwait(false);
                    var r = DiscordGuildSettingsHelper.GuildRef(guildId);
                    var list = await store.GetBindingRevisionsAsync(r, DiscordGuildSettingsHelper.Key, null, ct).ConfigureAwait(false);
                    return Results.Ok(list);
                })
            .WithTags(Constants.Rest.Discord.Route)
            .WithName("GetDiscordGuildSettingsRevisions");

        app.MapPost(
                $"{Constants.Rest.Discord.Guilds}/{{guildId:long}}/GuildSettings/Revert/{{revision:int}}", async Task<IResult> (
                    long guildId,
                    int revision,
                    [FromServices] IConfigStore store,
                    [FromServices] IDbContextFactory<DiscordDbContext> dbFactory,
                    CancellationToken ct) => {
                    await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                    if (await db.DiscordGuilds.FindAsync([guildId], ct).ConfigureAwait(false) == null)
                        return Results.NotFound(LyoProblemDetailsBuilder.CreateWithActivity().NotFound("Discord guild", guildId.ToString()).Build());

                    await DiscordGuildSettingsHelper.EnsureDefaultBindingAsync(store, guildId, ct).ConfigureAwait(false);
                    var r = DiscordGuildSettingsHelper.GuildRef(guildId);
                    await store.RevertBindingToRevisionAsync(r, DiscordGuildSettingsHelper.Key, revision, null, ct).ConfigureAwait(false);
                    var settings = await DiscordGuildSettingsHelper.GetSettingsAsync(store, guildId, ct).ConfigureAwait(false);
                    return Results.Ok(settings);
                })
            .WithTags(Constants.Rest.Discord.Route)
            .WithName("PostDiscordGuildSettingsRevert");

        return app;
    }
}