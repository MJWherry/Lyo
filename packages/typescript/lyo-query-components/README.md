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

Build order: `lyo-query` → `lyo-web-components` → `lyo-query-components`.

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

Theme via MUI (`lyo-web-components`). This package re-exports the query builders for existing imports. Build order: `lyo-query` → `lyo-web-components` → `lyo-query-components`.
