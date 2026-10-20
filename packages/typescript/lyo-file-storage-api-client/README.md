# lyo-file-storage-api-client

FileStorage HTTP client. Async and sync factories over `lyo-api-client`.

Four file operations:

- `download` — `GET FileStorage/files/{id}/download`. The service returns plaintext.
- `saveStream` / `saveBytes` — upload through the service. The service compresses and encrypts when those flags are set.
- `presignedDownloadUrl` — `GET FileStorage/files/{id}/presigned-read` when the file is neither encrypted nor compressed.
- `presignedUploadUrl` — `POST FileStorage/direct-upload/begin` when the upload is neither compressed nor encrypted. Returns `presignedPutUrl`.

```ts
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncFileStorageApiClient} from "lyo-file-storage-api-client";

const api = createAsyncApiClient({baseUrl: "http://localhost:5251", transport: fetchTransport});
const files = createAsyncFileStorageApiClient(api);

await files.saveStream({file, fileName: "a.bin", encrypt: true, keyId: "k1"});
const saved = await files.download(id);

const stores = await files.stores();
const archive = createAsyncFileStorageApiClient(api, {storeRoute: stores.data?.[0]?.route});
await archive.health();
```

`stores()` requests `GET FileStorage/stores`. `storeRoute` replaces the `FileStorage` base. The metadata query stays `FileStorage/FileMetadata`. The sync client has the same `stores()` method.
