# lyo-import-api-client

Posts an `ImportRequest` to `POST /Import` and returns the rows. Nothing here writes a store. The host persists them.

`tryAddField` rejects a second source aimed at the same target. `showPreview` keeps at most 50 good rows and every issue.

```ts
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncImportApiClient} from "lyo-import-api-client";

const imports = createAsyncImportApiClient(createAsyncApiClient({baseUrl: "http://localhost:5251", transport: fetchTransport}));
const result = await imports.postImport({format: "Csv", content: "Name\nAda\n"});
```
