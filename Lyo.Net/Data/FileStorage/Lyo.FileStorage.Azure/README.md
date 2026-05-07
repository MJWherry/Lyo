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

## Dependencies

*(Synchronized from `Lyo.FileStorage.Azure.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package               | Version    |
|-----------------------|------------|
| `Azure.Storage.Blobs` | `[12.27,)` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Compression`](../../Compression/Lyo.Compression/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.FileMetadataStore`](../../FileMetadataStore/Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage`](../Lyo.FileStorage/README.md)