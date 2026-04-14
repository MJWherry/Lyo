using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord emoji endpoints on the Lyo API.</summary>
public sealed class EmojiManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordEmojiRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordEmojiRes>>($"{Constants.Rest.Discord.Emojis}/Query", request, ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordEmojiRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordEmojiReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordEmojiReq>>, UpsertBulkResult<DiscordEmojiRes>>($"{Constants.Rest.Discord.Emojis}/Bulk/Upsert", requests, ct: ct);
}