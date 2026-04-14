using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Discord.Client.Managers;

/// <summary>Discord guild member endpoints on the Lyo API.</summary>
public sealed class MemberManager(LyoDiscordClient client)
{
    public Task<QueryRes<DiscordMemberRes>> QueryAsync(QueryReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryReq, QueryRes<DiscordMemberRes>>($"{Constants.Rest.Discord.Members}/Query", request, ct: ct);

    public Task<UpsertResult<DiscordMemberRes>> UpsertAsync(DiscordMemberReq request, CancellationToken ct = default)
    {
        var query = WhereClauseBuilder.And(and => and.Equals("UserId", request.UserId).Equals("GuildId", request.GuildId));
        var upsert = new UpsertRequest<DiscordMemberReq>(request, query);
        return client.PostAsAsync<UpsertRequest<DiscordMemberReq>, UpsertResult<DiscordMemberRes>>($"{Constants.Rest.Discord.Members}/Upsert", upsert, ct: ct);
    }

    /// <summary>POST <c>{entity}/Bulk/Upsert</c> (same contract as API <c>WithUpsertBulk</c> / <c>ApiFeatureFlag.UpsertBulk</c>).</summary>
    public Task<UpsertBulkResult<DiscordMemberRes>> UpsertBulkAsync(IReadOnlyList<UpsertRequest<DiscordMemberReq>> requests, CancellationToken ct = default)
        => client.PostAsAsync<IReadOnlyList<UpsertRequest<DiscordMemberReq>>, UpsertBulkResult<DiscordMemberRes>>(
            $"{Constants.Rest.Discord.Members}/Bulk/Upsert", requests, ct: ct);
}