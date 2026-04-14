using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord attachment endpoints on the Lyo API.</summary>
public sealed class AttachmentManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordAttachmentRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordAttachmentRes>>($"{Constants.Rest.Discord.Attachments}/Query", request, ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordAttachmentRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordAttachmentReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordAttachmentReq>>, UpsertBulkResult<DiscordAttachmentRes>>(
            $"{Constants.Rest.Discord.Attachments}/Bulk/Upsert", requests, ct: ct);
}