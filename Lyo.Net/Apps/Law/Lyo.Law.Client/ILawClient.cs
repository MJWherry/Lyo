using Lyo.Law.Models.Request;
using Lyo.Law.Models.Response;

namespace Lyo.Law.Client;

/// <summary>Typed HTTP client for Law routes that are not generic CRUD. Jurisdiction, code, node, annotation, and revision queries stay on <see cref="Lyo.Api.Client.IApiClient"/>.</summary>
public interface ILawClient
{
    /// <summary>Creates or updates a node by jurisdiction, code, and source key.</summary>
    Task<LawNodeUpsertRes> UpsertBySourceAsync(LawNodeUpsertReq request, CancellationToken ct = default);

    /// <summary>One node with body and notes.</summary>
    Task<LawNodeDetailRes?> GetNodeAsync(Guid id, CancellationToken ct = default);

    /// <summary>One node in a code by its display citation.</summary>
    Task<LawNodeDetailRes?> GetByCitationAsync(string jurisdictionKey, string codeKey, string citation, CancellationToken ct = default);

    /// <summary>Direct children, sort order then label.</summary>
    Task<IReadOnlyList<LawNodeChildRes>> GetChildrenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Parent chain, nearest parent first.</summary>
    Task<IReadOnlyList<LawNodeChildRes>> GetAncestorsAsync(Guid id, CancellationToken ct = default);
}
