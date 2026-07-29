# Lyo.HomeInventory

Portable contract for **household inventory** — large purchases (electronics,
appliances) with warranty tracking, kitchen consumables stocked across
pantries / freezers, and bin locations in garages. Domain records
(`HomeItemRecord`, `HomeCategoryRecord`, `HomeLocationRecord`,
`HomeItemStockRecord`, `HomeItemMovementRecord`) are keyed by `Guid`, and all
operations flow through `IHomeInventoryStore`.

## Surface — `IHomeInventoryStore`

- `SaveItemAsync(HomeItemRecord item, CancellationToken ct = default)` — upserts. When `item.Id` matches an existing row it is updated in place; otherwise a new row is inserted (an `Id` and `CreatedTimestamp` are generated when missing).
- `GetItemByIdAsync(Guid id, CancellationToken ct = default)` — single item by id.
- `GetItemBySkuAsync(string sku, CancellationToken ct = default)` — case-insensitive SKU lookup (trims and lower-cases the input before matching).
- `DeleteItemAsync(Guid id, CancellationToken ct = default)` — hard delete (no soft-delete column on item rows).

## Surface — Enums

- `HomeItemStatus` — `Active = 0`, `Discontinued = 1`, `Archived = 2`.
- `HomeItemCondition` — `Unknown = 0`, `New = 1`, `Used = 2`, `Refurbished = 3`, `OpenBox = 4`, `Damaged = 5`.
- `HomeItemMovementType` — `Receipt = 0`, `Issue = 1`, `TransferOut = 2`, `TransferIn = 3`, `Adjustment = 4`, `Return = 5`, `Loss = 6`, `StockTransfer = 7`. `TransferOut` / `TransferIn` are paired-ticket movements recorded individually, while `StockTransfer` is the single-row movement emitted by `TransferStockAsync` (both endpoints populated).

## Surface — Records

- **`HomeItemRecord`** — the item itself. Carries Owner (`EntityRef`), optional `CategoryId` / `ParentItemId`, all the catalog metadata (`Sku`, `Upc`, `Ean`, `Isbn`, `Manufacturer`, `ManufacturerPartNumber`, `Seller`, `VendorSku`, `ModelNumber`, `Color`, `SerialNumber`, `Imei`, `EthernetMacAddress`, `WifiMacAddress`, `BluetoothMacAddress`), pricing (`Msrp`, `Cost`, `Currency`), dimensions (`WeightGrams`, `LengthMm`, `WidthMm`, `HeightMm`), lifecycle (`AcquiredDate`, `WarrantyExpires`), provenance (`CountryOfOrigin`, `LotNumber`, `BatchNumber`), and a `CustomAttributesJson` escape hatch.
- **`HomeCategoryRecord`** — optional grouping with `ParentCategoryId`, `Name`, `Slug`, `Description`, `SortOrder`.
- **`HomeLocationRecord`** — physical location with `ParentLocationId`, `Name`, `Code` (short label/scanner code), `Description`, `IsActive`.
- **`HomeItemStockRecord`** — composite key `(ItemId, LocationId)` with `QuantityOnHand`, `QuantityReserved`, optional `ReorderPoint`, `UpdatedTimestamp`.
- **`HomeItemMovementRecord`** — immutable audit line with `ItemId`, `MovementType`, `Quantity`, optional `FromLocationId` / `ToLocationId`, `ReferenceNumber`, `Notes`, optional `CreatedBy` (`EntityRef`), `CreatedTimestamp`.

## Out of scope (by design)

- Full-text fuzzy search — wrap with `Lyo.Api` projections externally.
- Image attachments — combine with `Lyo.FileMetadataStore`.
- Multi-home tenancy — compose higher-level partition keys; the interface is deliberately single-scope.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)