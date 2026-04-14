using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord user endpoints on the Lyo API.</summary>
public sealed class UserManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordUserRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordUserRes>>($"{Constants.Rest.Discord.Users}/Query", request, ct: ct);

    public Task<DiscordUserRes?> GetAsync(long id, CancellationToken ct = default) => client.GetAsAsync<DiscordUserRes>($"{Constants.Rest.Discord.Users}/{id}", ct: ct);

    public Task<UpsertResult<DiscordUserRes>> UpsertAsync(DiscordUserReq request, CancellationToken ct = default)
        => client.PostAsAsync<UpsertRequest<DiscordUserReq>, UpsertResult<DiscordUserRes>>($"{Constants.Rest.Discord.Users}/Upsert", new(request, "Id", request.Id), ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordUserRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordUserReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordUserReq>>, UpsertBulkResult<DiscordUserRes>>($"{Constants.Rest.Discord.Users}/Bulk/Upsert", requests, ct: ct);
}