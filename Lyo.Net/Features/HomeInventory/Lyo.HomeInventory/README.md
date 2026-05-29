# Lyo.HomeInventory

Portable contract for **household inventory** — large purchases (electronics,
appliances) with warranty tracking, kitchen consumables stocked across
pantries / freezers, and bin locations in garages. Domain records
(`HomeItemRecord`, `HomeCategoryRecord`, `HomeLocationRecord`,
`HomeItemStockRecord`, `HomeItemMovementRecord`) are keyed by `Guid`, and all
operations flow through `IHomeInventoryStore`.

## Surface

### `IHomeInventoryStore`

**Items**

- `SaveItemAsync(HomeItemRecord item, CancellationToken ct = default)` —
  upserts. When `item.Id` matches an existing row it is updated in place;
  otherwise a new row is inserted (an `Id` and `CreatedTimestamp` are
  generated when missing).
- `GetItemByIdAsync(Guid id, CancellationToken ct = default)` — single item by id.
- `GetItemBySkuAsync(string sku, CancellationToken ct = default)` —
  case-insensitive SKU lookup (trims and lower-cases the input before
  matching).
- `DeleteItemAsync(Guid id, CancellationToken ct = default)` — hard delete
  (no soft-delete column on item rows).

**Categories**

- `SaveCategoryAsync` / `GetCategoryByIdAsync` / `ListCategoriesAsync` /
  `DeleteCategoryAsync`. The Postgres store refuses to delete a category
  that still has child categories or assigned items.

**Locations**

- `SaveLocationAsync` / `GetLocationByIdAsync` / `DeleteLocationAsync`.
- `ListLocationsAsync(bool activeOnly = true, CancellationToken ct = default)`
  — by default returns only rows with `IsActive == true`.
- The Postgres store refuses to delete a location with child locations,
  stock rows, or movement history.

**Stock**

- `UpsertStockAsync(Guid itemId, Guid locationId, decimal quantityOnHand, decimal quantityReserved, decimal? reorderPoint, CancellationToken ct = default)`
  — creates or replaces the quantity row for `(itemId, locationId)`.
- `GetStockAsync(Guid itemId, Guid locationId, CancellationToken ct = default)`
  — single stock row.
- `GetStockForItemAsync(Guid itemId, CancellationToken ct = default)` — every
  location that has a stock row for the item.
-

`AdjustStockAsync(Guid itemId, Guid locationId, decimal quantityDelta, HomeItemMovementType movementType, string? referenceNumber, string? notes, EntityRef? createdBy, CancellationToken ct = default)`
— applies a signed delta to `QuantityOnHand` and writes a paired movement
row in one transaction. Positive deltas land in `ToLocationId`, negative
deltas come out of `FromLocationId`. Refuses to drive on-hand negative or
to seed a brand-new row with a non-positive delta.

-

`TransferStockAsync(Guid itemId, Guid fromLocationId, Guid toLocationId, decimal quantity, string? referenceNumber, string? notes, EntityRef? createdBy, CancellationToken ct = default)`
— moves quantity between two locations inside a single transaction,
emitting a `StockTransfer` movement. Requires `quantity > 0` and fails when
the source row is missing or has insufficient on-hand.

**Movements**

- `ListMovementsForItemAsync(Guid itemId, int take = 200, CancellationToken ct = default)`
  — most-recent-first movement ledger for an item. `take <= 0` returns the
  entire history.

### Enums

- `HomeItemStatus` — `Active = 0`, `Discontinued = 1`, `Archived = 2`.
- `HomeItemCondition` — `Unknown = 0`, `New = 1`, `Used = 2`,
  `Refurbished = 3`, `OpenBox = 4`, `Damaged = 5`.
- `HomeItemMovementType` — `Receipt = 0`, `Issue = 1`, `TransferOut = 2`,
  `TransferIn = 3`, `Adjustment = 4`, `Return = 5`, `Loss = 6`,
  `StockTransfer = 7`. `TransferOut` / `TransferIn` are paired-ticket movements
  recorded individually, while `StockTransfer` is the single-row movement
  emitted by `TransferStockAsync` (both endpoints populated).

### Records

- **`HomeItemRecord`** — the item itself. Carries Owner (`EntityRef`), optional
  `CategoryId` / `ParentItemId`, all the catalog metadata (`Sku`, `Upc`,
  `Ean`, `Isbn`, `Manufacturer`, `ManufacturerPartNumber`, `Seller`,
  `VendorSku`, `ModelNumber`, `Color`, `SerialNumber`, `Imei`,
  `EthernetMacAddress`, `WifiMacAddress`, `BluetoothMacAddress`), pricing
  (`Msrp`, `Cost`, `Currency`), dimensions (`WeightGrams`, `LengthMm`,
  `WidthMm`, `HeightMm`), lifecycle (`AcquiredDate`, `WarrantyExpires`),
  provenance (`CountryOfOrigin`, `LotNumber`, `BatchNumber`), and a
  `CustomAttributesJson` escape hatch.
- **`HomeCategoryRecord`** — optional grouping with `ParentCategoryId`,
  `Name`, `Slug`, `Description`, `SortOrder`.
- **`HomeLocationRecord`** — physical location with `ParentLocationId`,
  `Name`, `Code` (short label/scanner code), `Description`, `IsActive`.
- **`HomeItemStockRecord`** — composite key `(ItemId, LocationId)` with
  `QuantityOnHand`, `QuantityReserved`, optional `ReorderPoint`,
  `UpdatedTimestamp`.
- **`HomeItemMovementRecord`** — immutable audit line with `ItemId`,
  `MovementType`, `Quantity`, optional `FromLocationId` / `ToLocationId`,
  `ReferenceNumber`, `Notes`, optional `CreatedBy` (`EntityRef`),
  `CreatedTimestamp`.

## Out of scope (by design)

- Full-text fuzzy search — wrap with `Lyo.Api` projections externally.
- Image attachments — combine with `Lyo.FileMetadataStore`.
- Multi-home tenancy — compose higher-level partition keys; the interface
  is deliberately single-scope.

## Related projects

- [`Lyo.HomeInventory.Postgres`](../Lyo.HomeInventory.Postgres/README.md) —
  EF Core implementation, schema, and DI extensions.
- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
  — `EntityRef` used by `Owner` and `CreatedBy`.
