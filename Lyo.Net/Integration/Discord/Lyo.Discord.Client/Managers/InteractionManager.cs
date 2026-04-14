using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord interaction endpoints on the Lyo API.</summary>
public sealed class InteractionManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordInteractionRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordInteractionRes>>($"{Constants.Rest.Discord.Interactions}/Query", request, ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordInteractionRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordInteractionReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordInteractionReq>>, UpsertBulkResult<DiscordInteractionRes>>(
            $"{Constants.Rest.Discord.Interactions}/Bulk/Upsert", requests, ct: ct);
}