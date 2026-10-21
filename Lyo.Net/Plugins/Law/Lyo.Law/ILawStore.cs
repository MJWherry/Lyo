using Lyo.Law.Models.Request;
using Lyo.Law.Models.Response;

namespace Lyo.Law;

/// <summary>
/// Stores US state statutes: jurisdictions, named codes, and a node tree with official notes. Listing and search belong in the query layer. Node text changes go through
/// <see cref="UpsertNodeBySourceKeyAsync" /> so a prior revision is kept.
/// </summary>
public interface ILawStore
{
    /// <summary>Inserts or updates a state by key. <c>US</c> is rejected.</summary>
    Task<LawJurisdictionRes> SaveJurisdictionAsync(LawJurisdictionReq jurisdiction, CancellationToken ct = default);

    /// <summary>One state by key.</summary>
    Task<LawJurisdictionRes?> GetJurisdictionByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>Inserts or updates a code inside a state. The jurisdiction must already exist.</summary>
    Task<LawCodeRes> SaveCodeAsync(string jurisdictionKey, LawCodeReq code, CancellationToken ct = default);

    /// <summary>One code by jurisdiction key and code key.</summary>
    Task<LawCodeRes?> GetCodeAsync(string jurisdictionKey, string codeKey, CancellationToken ct = default);

    /// <summary>
    /// Inserts or updates a node by jurisdiction, code, and source key. An unchanged content hash leaves the row and its notes alone. A changed hash stores the previous text,
    /// then replaces heading, body, and notes.
    /// </summary>
    Task<LawNodeUpsertRes> UpsertNodeBySourceKeyAsync(LawNodeUpsertReq request, CancellationToken ct = default);

    /// <summary>One node with body and notes.</summary>
    Task<LawNodeDetailRes?> GetNodeByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>One node in a code by its display citation.</summary>
    Task<LawNodeDetailRes?> GetNodeByCitationAsync(string jurisdictionKey, string codeKey, string citation, CancellationToken ct = default);

    /// <summary>Direct children, sort order then label.</summary>
    Task<IReadOnlyList<LawNodeChildRes>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);

    /// <summary>Parent chain, nearest parent first.</summary>
    Task<IReadOnlyList<LawNodeChildRes>> GetAncestorsAsync(Guid nodeId, CancellationToken ct = default);

    /// <summary>Prior snapshots for a node, revision order.</summary>
    Task<IReadOnlyList<LawRevisionRes>> ListRevisionsAsync(Guid nodeId, CancellationToken ct = default);
}