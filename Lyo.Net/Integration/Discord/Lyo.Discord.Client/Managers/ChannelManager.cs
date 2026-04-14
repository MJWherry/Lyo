using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord channel endpoints on the Lyo API.</summary>
public sealed class ChannelManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordChannelRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordChannelRes>>($"{Constants.Rest.Discord.Channels}/Query", request, ct: ct);

    public Task<UpsertResult<DiscordChannelRes>> UpsertAsync(DiscordChannelReq request, CancellationToken ct = default)
        => client.PostAsAsync<UpsertRequest<DiscordChannelReq>, UpsertResult<DiscordChannelRes>>(
            $"{Constants.Rest.Discord.Channels}/Upsert", new(request, "Id", request.Id), ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordChannelRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordChannelReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordChannelReq>>, UpsertBulkResult<DiscordChannelRes>>(
            $"{Constants.Rest.Discord.Channels}/Bulk/Upsert", requests, ct: ct);
}