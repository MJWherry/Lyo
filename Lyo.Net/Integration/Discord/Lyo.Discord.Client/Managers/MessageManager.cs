using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord message endpoints on the Lyo API.</summary>
public sealed class MessageManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordMessageRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordMessageRes>>($"{Constants.Rest.Discord.Messages}/Query", request, ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordMessageRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordMessageReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordMessageReq>>, UpsertBulkResult<DiscordMessageRes>>(
            $"{Constants.Rest.Discord.Messages}/Bulk/Upsert", requests, ct: ct);
}