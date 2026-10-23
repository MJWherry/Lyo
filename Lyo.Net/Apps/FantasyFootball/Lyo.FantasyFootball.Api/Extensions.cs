using Lyo.Exceptions;
using Lyo.FantasyFootball.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FantasyFootball.Api;

/// <summary>Registers the fantasy football store and maps the chat and trade routes.</summary>
public static class Extensions
{
    /// <summary>Registers the Postgres store and <see cref="FantasyFootballInteractions" /> for a host that already has <c>FantasyFootballClient</c>.</summary>
    public static IServiceCollection AddFantasyFootballApiForWorkbench(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddPostgresFantasyFootballStore(new PostgresFantasyFootballOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddSingleton<FantasyFootballInteractions>();
        return services;
    }

    /// <summary>Maps group <c>FantasyFootball</c>. Pass <paramref name="allowAnonymous" /> for a workbench host.</summary>
    public static WebApplication MapFantasyFootballApi(this WebApplication app, bool allowAnonymous = false)
    {
        ArgumentHelpers.ThrowIfNull(app);
        var group = app.MapGroup("FantasyFootball").WithTags("FantasyFootball");
        if (allowAnonymous)
            group.AllowAnonymous();

        group.MapPost(
            "/{leagueId:int}/{seasonId:int}/refresh",
            async (int leagueId, int seasonId, HttpRequest request, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.RefreshAsync(leagueId, seasonId, FantasyFootballCredentials.From(request), ct)));

        group.MapGet(
            "/{leagueId:int}/{seasonId:int}",
            async (int leagueId, int seasonId, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.GetLeagueAsync(leagueId, seasonId, ct)));

        group.MapGet(
            "/{leagueId:int}/{seasonId:int}/conversations",
            async (int leagueId, int seasonId, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.ListConversationsAsync(leagueId, seasonId, ct)));

        group.MapGet(
            "/{leagueId:int}/{seasonId:int}/conversations/{topicId}/messages",
            async (int leagueId, int seasonId, string topicId, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.ListMessagesAsync(leagueId, seasonId, topicId, ct)));

        group.MapPost(
            "/{leagueId:int}/{seasonId:int}/conversations/{topicId}/messages",
            async (int leagueId, int seasonId, string topicId, PostChatMessageRequest body, HttpRequest request, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.PostMessageAsync(leagueId, seasonId, topicId, body.Content, FantasyFootballCredentials.From(request), ct)));

        group.MapPost(
            "/{leagueId:int}/{seasonId:int}/conversations/direct",
            async (int leagueId, int seasonId, PostDirectMessageRequest body, HttpRequest request, FantasyFootballInteractions interactions, CancellationToken ct) => Results.Ok(
                await interactions.PostDirectAsync(leagueId, seasonId, body.Recipient, body.Content, FantasyFootballCredentials.From(request), ct)));

        group.MapGet(
            "/{leagueId:int}/{seasonId:int}/trades",
            async (int leagueId, int seasonId, bool? pending, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.ListTradesAsync(leagueId, seasonId, pending, ct)));

        group.MapPost(
            "/{leagueId:int}/{seasonId:int}/trades",
            async (int leagueId, int seasonId, ProposeTradeRequest body, HttpRequest request, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.ProposeTradeAsync(leagueId, seasonId, body, FantasyFootballCredentials.From(request), ct)));

        group.MapPost(
            "/{leagueId:int}/{seasonId:int}/trades/{transactionId}/respond",
            async (int leagueId, int seasonId, string transactionId, RespondTradeRequest body, HttpRequest request, FantasyFootballInteractions interactions, CancellationToken ct)
                => Results.Ok(await interactions.RespondToTradeAsync(leagueId, seasonId, transactionId, body, FantasyFootballCredentials.From(request), ct)));

        return app;
    }
}