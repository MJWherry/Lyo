# Lyo.HomeInventory

Contract for household inventory: large purchases (electronics, appliances) with warranty tracking, kitchen consumables stocked across pantries / freezers, and bin locations in
garages. Domain records (`HomeItemRecord`, `HomeCategoryRecord`, `HomeLocationRecord`, `HomeItemStockRecord`, `HomeItemMovementRecord`) are keyed by `Guid`. All operations go
through `IHomeInventoryStore`.

## `IHomeInventoryStore`

- `SaveItemAsync(HomeItemRecord item, CancellationToken ct = default)` upserts. When `item.Id` matches an existing row it is updated in place. Otherwise a new row is inserted. An
  `Id` and `CreatedTimestamp` are generated when missing.
- `GetItemByIdAsync(Guid id, CancellationToken ct = default)` one item by id.
- `DeleteItemAsync(Guid id, CancellationToken ct = default)` hard delete. Item rows have no soft-delete column.

## Enums

- `HomeItemStatus`. `Active = 0`, `Discontinued = 1`, `Archived = 2`.
- `HomeItemCondition`. `Unknown = 0`, `New = 1`, `Used = 2`, `Refurbished = 3`, `OpenBox = 4`, `Damaged = 5`.
- `HomeItemMovementType`. `Receipt = 0`, `Issue = 1`, `TransferOut = 2`, `TransferIn = 3`, `Adjustment = 4`, `Return = 5`, `Loss = 6`, `StockTransfer = 7`. `TransferOut` /
  `TransferIn` are paired-ticket movements recorded individually, while `StockTransfer` is the single-row movement emitted by `TransferStockAsync` (both endpoints populated).
- `HomeAttachmentRole`. `Photo`, `Receipt`, `Manual`, `Warranty`, `Document` (defined in `Lyo.HomeInventory.Models`).

## Records

- `HomeItemRecord`. The item itself. Carries Owner (`EntityRef`), optional `CategoryId` / `ParentItemId` / `LocationId` (room assignment, independent of stock), and `ProductKeys`
  (`List<HomeProductKeyEntry>` of kind/value/note; the same kind may appear more than once, e.g. two Wi-Fi MACs). Free-text comments live on `INoteStore`.
- `HomeCategoryRecord`. Optional grouping with `ParentCategoryId`, `Name`, `Slug`, `Description`, `SortOrder`.
- `HomeLocationRecord`. Physical location with `ParentLocationId`, `Name`, `Code` (short label/scanner code), `Description`, `IsActive`.
- `HomeItemStockRecord`. Composite key `(ItemId, LocationId)` with `QuantityOnHand`, `QuantityReserved`, optional `ReorderPoint`, `UpdatedTimestamp`.
- `HomeItemMovementRecord`. Immutable audit line with `ItemId`, `MovementType`, `Quantity`, optional `FromLocationId` / `ToLocationId`, `ReferenceNumber`, optional `CreatedBy`
  (`EntityRef`), `CreatedTimestamp`.
- `HomeAttachmentRecord`. FileStorage file linked to an item, location, or category (`EntityType`, `EntityId`, `FileId`, `Role`, `Caption`, `SortOrder`, `IsPrimary`).

## Out of scope (by design)

- Full-text fuzzy search. Wrap with `Lyo.Api` projections externally.
- Multi-home tenancy. Compose higher-level partition keys. The interface is deliberately single-scope.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.HomeInventory.Models` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)