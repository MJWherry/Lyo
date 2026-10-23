# lyo-feature-flags-api-client

Feature flag HTTP client for `GET /features`, `GET /features/manage`, and `PUT /features/manage/{name}`.

`enabled` returns the names that are on for the caller. `list` returns every flag's name and mode. `setEnabled` turns one flag on or off and clears that flag's percentage and rules.

```ts
import {createAsyncFeatureFlagApiClient} from "lyo-feature-flags-api-client";
```
