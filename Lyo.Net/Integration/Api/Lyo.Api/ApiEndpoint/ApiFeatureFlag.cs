namespace Lyo.Api.ApiEndpoint;

/// <summary>Bit flags selecting which HTTP endpoints and behaviors are registered for a typed or dynamic CRUD group.</summary>
[Flags]
public enum ApiFeatureFlag
{
    /// <summary>No endpoints.</summary>
    None = 0,

    /// <summary><c>POST …/Query</c> and <c>POST …/QueryProject</c>.</summary>
    Query = 1 << 0,

    /// <summary><c>GET …/{id}</c> (or key route from <see cref="ApiEndpointBuilderExtensions.GetDefaultEndpoint{TKey}" />).</summary>
    Get = 1 << 1,

    /// <summary><c>POST …/</c> create.</summary>
    Create = 1 << 2,

    /// <summary><c>POST …/Bulk</c> create.</summary>
    CreateBulk = 1 << 3,

    /// <summary><c>POST …/Update</c> full replace.</summary>
    Update = 1 << 4,

    /// <summary><c>POST …/Bulk/Update</c>.</summary>
    UpdateBulk = 1 << 5,

    /// <summary><c>PATCH …/</c> partial update by property dictionary.</summary>
    Patch = 1 << 6,

    /// <summary><c>PATCH …/Bulk</c>.</summary>
    PatchBulk = 1 << 7,

    /// <summary>Delete by key route and delete-by-body.</summary>
    Delete = 1 << 8,

    /// <summary><c>DELETE …/Bulk</c>.</summary>
    DeleteBulk = 1 << 9,

    /// <summary><c>POST …/Upsert</c>.</summary>
    Upsert = 1 << 10,

    /// <summary><c>POST …/Bulk/Upsert</c>.</summary>
    UpsertBulk = 1 << 11,

    /// <summary>Upsert pipeline runs create before/after hooks for the insert branch.</summary>
    UpsertInheritCreate = 1 << 12,

    /// <summary>Upsert pipeline runs update before/after hooks for the update branch.</summary>
    UpsertInheritUpdate = 1 << 13,

    /// <summary>Patch pipeline reuses update before/after hooks where configured.</summary>
    PatchInheritsUpdate = 1 << 14,

    /// <summary><c>POST …/Export</c> (requires export service registration in DI; see host setup in this package README).</summary>
    Export = 1 << 15,

    /// <summary><c>GET …/Metadata</c> OpenAPI-style shape for the group.</summary>
    Metadata = 1 << 16,

    /// <summary>Allows <c>ComputedFields</c> on projection requests (requires formatter service and <see cref="Query" />).</summary>
    ProjectionComputedFields = 1 << 17,

    /// <summary><see cref="Query" /> | <see cref="Get" />.</summary>
    ReadOnly = Query | Get,

    /// <summary>Query, get, create, update, patch, delete (no bulk, upsert, or export).</summary>
    BasicCrud = Query | Get | Create | Update | Patch | Delete,

    /// <summary><see cref="BasicCrud" /> | <see cref="Upsert" />.</summary>
    FullCrud = BasicCrud | Upsert,

    /// <summary>All bulk operation endpoints.</summary>
    BulkOperations = CreateBulk | UpdateBulk | PatchBulk | DeleteBulk | UpsertBulk,

    /// <summary>Standard read/write/export flags; does not include <see cref="Metadata" /> or <see cref="ProjectionComputedFields" />.</summary>
    All = Query | Get | Create | CreateBulk | Update | UpdateBulk | Patch | PatchBulk | Delete | DeleteBulk | Upsert | UpsertBulk | Export
}
