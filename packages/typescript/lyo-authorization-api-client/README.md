# lyo-authorization-api-client

Grant HTTP client for `GET/POST /authorization/grants`, `POST /authorization/grants/{id}/revoke`, and `POST /authorization/grants/check`.

`grantFormToWrite` turns a blank resource id or tenant into null, stores a guid resource id as lowercase `D`, and marks a local expiry as UTC without shifting the clock. A whitespace resource id throws.

```ts
import {createAsyncAuthorizationApiClient, grantFormToWrite} from "lyo-authorization-api-client";
```
