# lyo-query-components

React UI for building Lyo.Query `whereClause` trees (And/Or groups + conditions).

## Install (monorepo)

```json
{
  "dependencies": {
    "lyo-query": "file:../../packages/typescript/lyo-query",
    "lyo-query-components": "file:../../packages/typescript/lyo-query-components"
  }
}
```

Build order: `lyo-query` → `lyo-query-components`.

## Usage

```tsx
import { useState } from "react";
import {
  WhereClauseBuilder,
  defaultGroup,
  type WhereClause,
} from "lyo-query-components";
import "lyo-query-components/styles.css";

export function Demo() {
  const [where, setWhere] = useState<WhereClause>(() => defaultGroup("FirstName"));

  return (
    <WhereClauseBuilder
      value={where}
      onChange={setWhere}
      defaultField="FirstName"
      fieldPresets={["FirstName", "LastName", "SourceEntityType", "Id"]}
    />
  );
}
```

Theme via CSS variables: `--lyo-qb-accent`, `--lyo-qb-line`, `--lyo-qb-bg`, `--lyo-qb-ink`, `--lyo-qb-muted`, `--lyo-qb-input-bg`, `--lyo-qb-soft`, `--lyo-qb-radius`.
