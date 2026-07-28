# lyo-query-react

React UI for building Lyo.Query `whereClause` trees (And/Or groups + conditions).

## Install (monorepo)

```json
{
  "dependencies": {
    "lyo-query": "file:../../packages/typescript/lyo-query",
    "lyo-query-react": "file:../../packages/typescript/lyo-query-react"
  }
}
```

Build order: `lyo-query` → `lyo-query-react`.

## Usage

```tsx
import { useState } from "react";
import {
  WhereClauseBuilder,
  defaultGroup,
  type WhereClause,
} from "lyo-query-react";
import "lyo-query-react/styles.css";

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
