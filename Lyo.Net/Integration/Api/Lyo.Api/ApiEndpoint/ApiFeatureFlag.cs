namespace Lyo.Api.ApiEndpoint;

[Flags]
public enum ApiFeatureFlag
{
    None = 0,
    Query = 1 << 0,
    Get = 1 << 1,
    Create = 1 << 2,
    CreateBulk = 1 << 3,
    Update = 1 << 4,
    UpdateBulk = 1 << 5,
    Patch = 1 << 6,
    PatchBulk = 1 << 7,
    Delete = 1 << 8,
    DeleteBulk = 1 << 9,
    Upsert = 1 << 10,
    UpsertBulk = 1 << 11,
    UpsertInheritCreate = 1 << 12,
    UpsertInheritUpdate = 1 << 13,
    PatchInheritsUpdate = 1 << 14,
    Export = 1 << 15,
    Metadata = 1 << 16,
    ProjectionComputedFields = 1 << 17,

    // Convenience combinations
    ReadOnly = Query | Get,
    BasicCrud = Query | Get | Create | Update | Patch | Delete,
    FullCrud = BasicCrud | Upsert,
    BulkOperations = CreateBulk | UpdateBulk | PatchBulk | DeleteBulk | UpsertBulk,
    All = Query | Get | Create | CreateBulk | Update | UpdateBulk | Patch | PatchBulk | Delete | DeleteBulk | Upsert | UpsertBulk | Export
}