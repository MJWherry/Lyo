# Lyo.Api.FileStorage.Models

Request and response DTOs for the file-storage HTTP API. `Lyo.Api.FileStorage` maps them onto `IFileStorageService`. `Lyo.FileStorage.Web.Components` posts them through `IApiClient` and never references the storage engine. Folder listing uses `FileStorageFolderListResponse` and `FileStoragePresence` flags (`Store` / `Physical` / `Both`; integers match `Lyo.FileStorage.FileStoragePresence`).

## Examples

### POST from a client

```csharp
await api.PostAsAsync<CopyFileRequest, FileStoreResult>(
    "FileStorage/files/copy",
    new(sourceId, pathPrefix: "archive"));
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)