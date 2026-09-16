# lyo-authorization-components

React grant UI. Mirrors `Lyo.Authorization.Web.Components`: `GrantPanel`, `GrantEditDialog`, and `GrantCheckForm`.

`GrantPanel` lists grants from `lyo-authorization-api-client`. `allowEdit` defaults to false. A null resource id or tenant renders as "any". A failed check is an error, not a deny.

```tsx
import {GrantPanel, GrantCheckForm} from "lyo-authorization-components";
import "lyo-authorization-components/styles.css";
```
