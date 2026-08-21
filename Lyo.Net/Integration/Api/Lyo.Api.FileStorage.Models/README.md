# Lyo.Api.FileStorage.Models

HTTP request and response DTOs for the file-storage API. `Lyo.Api.FileStorage` maps these onto `IFileStorageService`. Clients (`Lyo.FileStorage.Web.Components`) send them over `IApiClient` and do not reference the storage engine.

## Examples

### Client POST

```csharp
await api.PostAsAsync<CopyFileRequest, FileStoreResult>(
    "Workbench/FileStorage/files/copy",
    new(sourceId, pathPrefix: "archive"));
```