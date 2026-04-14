# Lyo.FileStorage.Azure

Azure Blob Storage implementation of the Lyo FileStorage service. Provides the same API as Lyo.FileStorage.S3 and Lyo.FileStorage (Local) for storing files in Azure Blob Storage.

## Features

- **Azure Blob Storage** - Full implementation of IFileStorageService using Azure.Storage.Blobs
- **Compression & Encryption** - Optional compression and two-key encryption (same as AWS/Local)
- **Pre-signed URLs** - Generate SAS URLs for direct blob access
- **Metrics & Logging** - Optional integration with Lyo.Metrics and ILogger

## Quick Start

```csharp
using Lyo.FileStorage.Azure;
using Lyo.FileStorage.Models;

var options = new AzureFileStorageOptions
{
    ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    ContainerName = "my-container",
    BlobPrefix = "files"  // optional
};

services.AddAzureFileStorageService(options);

// Or from configuration
services.AddAzureFileStorageService("AzureFileStorageOptions");
```

## Configuration

```json
{
  "AzureFileStorageOptions": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    "ContainerName": "my-container",
    "BlobPrefix": "files",
    "EnableMetrics": false
  }
}
```

## Pre-signed URLs (SAS)

```csharp
var url = await azureFileStorage.GetPreSignedUrlAsync(fileId, TimeSpan.FromHours(1), pathPrefix, ct);
```

## Health Checks

`IFileStorageService` extends `IHealth`. Get health directly from the service: `await fileStorage.CheckHealthAsync()`.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.FileStorage.Azure.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Azure.Storage.Blobs` | `[12.27,)` |

### Project references

- `Lyo.Common`
- `Lyo.Compression`
- `Lyo.Encryption`
- `Lyo.Exceptions`
- `Lyo.FileMetadataStore`
- `Lyo.FileStorage`

## Public API (generated)

Top-level `public` types in `*.cs` (*6*). Nested types and file-scoped namespaces may omit some entries.

- `AzureFileStorageOptions`
- `AzureFileStorageService`
- `AzureMultipartUploadService`
- `Constants`
- `Extensions`
- `Metrics`

<!-- LYO_README_SYNC:END -->

