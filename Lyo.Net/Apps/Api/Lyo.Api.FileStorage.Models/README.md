# Lyo.Api.FileStorage.Models

Request and response DTOs for the file-storage HTTP API. `Lyo.Api.FileStorage` maps them onto `IFileStorageService`. `Lyo.FileStorage.Web.Components` posts them through `IApiClient` and never references the storage engine.

## Examples

### POST from a client

```csharp
await api.PostAsAsync<CopyFileRequest, FileStoreResult>(
    "FileStorage/files/copy",
    new(sourceId, pathPrefix: "archive"));
```