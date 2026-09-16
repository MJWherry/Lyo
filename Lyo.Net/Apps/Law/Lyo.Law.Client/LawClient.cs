using Lyo.Api.Client;
using Lyo.Exceptions;
using Lyo.Law.Models;
using Lyo.Law.Models.Request;
using Lyo.Law.Models.Response;

namespace Lyo.Law.Client;

/// <summary>Typed facade over Law upsert and lookup routes. Sit it on any <see cref="IApiClient"/> implementation.</summary>
public sealed class LawClient : ILawClient
{
    private readonly IApiClient _api;
    private readonly string? _routePrefix;

    /// <summary>Constructs the client. Null <paramref name="options"/> keeps routes relative to the inner client's base address.</summary>
    public LawClient(IApiClient apiClient, LawClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        _api = apiClient;
        _routePrefix = options?.RoutePrefix;
    }

    /// <inheritdoc />
    public Task<LawNodeUpsertRes> UpsertBySourceAsync(LawNodeUpsertReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PutAsAsync<LawNodeUpsertReq, LawNodeUpsertRes>(Route(Constants.Rest.Law.NodesBySource), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<LawNodeDetailRes?> GetNodeAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsAsync<LawNodeDetailRes>(Route(Constants.Rest.Law.Node(id)), ct: ct);

    /// <inheritdoc />
    public Task<LawNodeDetailRes?> GetByCitationAsync(string jurisdictionKey, string codeKey, string citation, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jurisdictionKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(codeKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(citation);
        return _api.GetAsAsync<LawNodeDetailRes>(Route(Constants.Rest.Law.NodeByCitation(jurisdictionKey, codeKey, citation)), ct: ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LawNodeChildRes>> GetChildrenAsync(Guid id, CancellationToken ct = default)
        => await _api.GetAsAsync<List<LawNodeChildRes>>(Route(Constants.Rest.Law.NodeChildren(id)), ct: ct).ConfigureAwait(false) ?? [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<LawNodeChildRes>> GetAncestorsAsync(Guid id, CancellationToken ct = default)
        => await _api.GetAsAsync<List<LawNodeChildRes>>(Route(Constants.Rest.Law.NodeAncestors(id)), ct: ct).ConfigureAwait(false) ?? [];

    private string Route(string relative)
    {
        if (string.IsNullOrEmpty(_routePrefix))
            return relative;

        return $"{_routePrefix.Trim('/')}/{relative.TrimStart('/')}";
    }
}
