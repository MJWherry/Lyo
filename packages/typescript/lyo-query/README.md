# lyo-query

Shared Lyo Query **where-clause** models and helpers for TypeScript.

Use this from API clients, BFFs, and UI packages (`lyo-query-components`, `lyo-web-components`). Domain clients
(e.g. `lyo-person-api-client`) re-export these types on request models.

Grid helpers: `mergeWhere`, `buildGridWhere`, `buildQuickSearchWhere`, `operatorsFor`, `FilterPropertyDefinition`.

## Usage

```ts
import {
  defaultGroup,
  isWhereClause,
  type WhereClause,
} from "lyo-query";

const where: WhereClause = defaultGroup("FirstName");
if (!isWhereClause(where)) throw new Error("invalid");
```

## Wire shape

```json
{
  "$type": "group",
  "Operator": "And",
  "Children": [
    { "$type": "condition", "Field": "FirstName", "Comparison": "NotEquals", "Value": null }
  ]
}
```
