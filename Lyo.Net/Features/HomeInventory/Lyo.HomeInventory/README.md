# Lyo.HomeInventory

Portable contract for **household inventory**—large purchases (electronics, appliances) with warranty tracking, resale history, stocking kitchen consumables across pantries/freezers, and bin locations in garages.

Define domain records (**`HomeItemRecord`**, **`HomeCategoryRecord`**, **`HomeLocationRecord`**, **`HomeItemStockRecord`**, **`HomeItemMovementRecord`**) keyed by **`Guid`**. Operations flow through **`IHomeInventoryStore`**.

### Items

**`SaveItemAsync` / `GetItemByIdAsync` / `GetItemBySkuAsync` / `DeleteItemAsync`**

SKU search helps scan barcodes; deletion semantics depend on your EF implementation (**`Lyo.HomeInventory.Postgres`**) enforcing foreign keys vs soft deletes—inspect migrations before exposing destructive endpoints publicly.

### Categories & locations

List APIs support UI pickers (**`ListCategoriesAsync`**, **`ListLocationsAsync(activeOnly)`**). Deletes should fail if dependents exist unless Postgres configuration adds `ON DELETE SET NULL` behaviors—again see EF models.

### Stock & ledger

Core differentiator vs simplistic note-taking apps:

**`UpsertStockAsync`** — idempotent-ish replacement of `{ onHand, reserved, reorderPoint }` per `(itemId, locationId)` pair.

**`GetStock*`** — interrogations for dashboards (“how much oat milk in downstairs fridge?”).

**`AdjustStockAsync`** — **atomic delta** writes with optional operator **`EntityRef`**, textual notes/reference numbers, enumerated **`HomeItemMovementType`** (consume, spoil, recount…).

**`TransferStockAsync`** — multi-row adjustment across two locations in one transactional call pattern (implemented concretely in Postgres store).

### Movements audit

**`ListMovementsForItemAsync`** tail-recency limited (`take`, default bounded) feeding timeline UI / accounting exports.

### Out of scope (by design)

- Full-text fuzzy search (**wrap with `Lyo.Api` projections** externally).
- Image attachments — combine with **`Lyo.FileMetadataStore`**.
- Multi-home tenancy — compose higher-level app partition keys; interface is deliberately single-scope.

See [`Lyo.HomeInventory.Postgres`](../HomeInventory.Postgres/README.md) for schema + **`AddPostgresHomeInventoryStore`**.
