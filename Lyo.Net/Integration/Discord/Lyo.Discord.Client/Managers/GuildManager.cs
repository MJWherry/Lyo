using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Guild and guild-scoped config endpoints on the Lyo Discord API.</summary>
public sealed class GuildManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordGuildRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordGuildRes>>($"{Constants.Rest.Discord.Guilds}/Query", request, ct: ct);

    public Task<DiscordGuildRes?> GetAsync(long id, CancellationToken ct = default) => client.GetAsAsync<DiscordGuildRes>($"{Constants.Rest.Discord.Guilds}/{id}", ct: ct);

    /// <summary>GET per-guild bot settings from the config store (creates default null binding if missing).</summary>
    public Task<DiscordGuildSettings?> GetSettingsAsync(long guildId, CancellationToken ct = default)
        => client.GetAsAsync<DiscordGuildSettings>(Constants.Rest.Discord.GuildSettings(guildId), ct: ct);

    /// <summary>PUT guild settings (unauthenticated until the host adds authorization).</summary>
    public Task<DiscordGuildSettings> PutSettingsAsync(long guildId, DiscordGuildSettings body, CancellationToken ct = default)
        => client.PutAsAsync<DiscordGuildSettings, DiscordGuildSettings>(Constants.Rest.Discord.GuildSettings(guildId), body, ct: ct);

    public Task<UpsertResult<DiscordGuildRes>> UpsertAsync(DiscordGuildReq request, CancellationToken ct = default)
        => client.PostAsAsync<UpsertRequest<DiscordGuildReq>, UpsertResult<DiscordGuildRes>>($"{Constants.Rest.Discord.Guilds}/Upsert", new(request, "Id", request.Id), ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordGuildRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordGuildReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordGuildReq>>, UpsertBulkResult<DiscordGuildRes>>($"{Constants.Rest.Discord.Guilds}/Bulk/Upsert", requests, ct: ct);
}