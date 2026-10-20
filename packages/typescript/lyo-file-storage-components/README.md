# lyo-file-storage-components

React FileStorage management UI. Mirrors `Lyo.FileStorage.Web.Components` (`FileStorageManagement`) using `lyo-web-components` and `lyo-file-storage-api-client`.

Peer React 19 + Emotion. Wrap the tree in `LyoProvider`.

## Usage

```tsx
"use client";
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {FileStorageManagement} from "lyo-file-storage-components";
import "lyo-file-storage-components/styles.css";

const api = createAsyncApiClient({
  baseUrl: "/api",
  transport: fetchTransport,
});

export function FilesPage() {
  return <FileStorageManagement apiClient={api} />;
}
```

`routePrefix` is omitted so the store select chooses the store. File calls use that store's route. The metadata grid stays on `FileStorage/FileMetadata`.

Files uploads through the service. Presigned download and upload actions are shown only when the file is neither encrypted nor compressed. Download always uses the API, which decrypts and decompresses. Access links also go through the API.
