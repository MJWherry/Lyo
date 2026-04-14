using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord role endpoints on the Lyo API.</summary>
public sealed class RoleManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordRoleRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordRoleRes>>($"{Constants.Rest.Discord.Roles}/Query", request, ct: ct);

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordRoleRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordRoleReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordRoleReq>>, UpsertBulkResult<DiscordRoleRes>>($"{Constants.Rest.Discord.Roles}/Bulk/Upsert", requests, ct: ct);
}