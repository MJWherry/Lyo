# Lyo.FileStorage

A production-ready file storage library for .NET providing secure, scalable file storage with optional compression and encryption support.

## Features

- ✅ **Multiple Storage Backends** - Local file system, AWS S3, and Azure Blob implementations
- ✅ **Compression Support** - Optional file compression using Lyo.Compression
- ✅ **Encryption Support** - Optional two-key encryption using Lyo.Encryption
- ✅ **Metadata Management** - Comprehensive file metadata tracking
- ✅ **Duplicate Detection** - Configurable duplicate file handling strategies
- ✅ **Streaming Operations** - Efficient single-pass streaming for large files
- ✅ **Thread-Safe** - Safe for concurrent operations
- ✅ **Error Recovery** - Automatic cleanup of partial files on failure
- ✅ **Metrics & Logging** - Built-in observability support
- ✅ **Security** - Path traversal attack prevention and input validation

## Quick Start

### Local File Storage

```csharp
using Lyo.FileStorage;
using Lyo.FileStorage.Models;

var options = new LocalFileStorageServiceOptions
{
    RootDirectoryPath = "/path/to/storage",
    EnableDuplicateDetection = true,
    DuplicateStrategy = DuplicateHandlingStrategy.ReturnExisting
};

var service = new LocalFileStorageService(options);

// Save a file
var data = File.ReadAllBytes("document.pdf");
var result = await service.SaveFileAsync(
    data,
    originalFileName: "document.pdf",
    compress: true,
    encrypt: true,
    keyId: "my-encryption-key"
);

// Retrieve a file
var retrievedData = await service.GetFileAsync(result.Id);

// Delete a file
await service.DeleteFileAsync(result.Id);
```

### S3 storage (AWS, Backblaze B2, MinIO, Wasabi, Cloudflare R2, …)

```csharp
using Lyo.FileStorage.S3;
using Lyo.FileStorage.Models;

var options = new S3FileStorageOptions
{
    BucketName = "my-bucket",
    Region = "us-east-1",
    AccessKeyId = "your-access-key",
    SecretAccessKey = "your-secret-key"
};

var metadataStore = new YourMetadataStore(); // Implement IFileMetadataStore
var service = new S3FileStorageService(options, metadataStore);

// Use same API as LocalFileStorageService
```

## Production Ready

This library has been reviewed for production use and includes:

- ✅ Thread-safe operations
- ✅ Comprehensive error handling with automatic cleanup
- ✅ Path traversal attack prevention
- ✅ Input validation and security checks
- ✅ Extensive test coverage
- ✅ Logging and metrics support
- ✅ Streaming support for large files
- ✅ Atomic file operations
- ✅ Detailed error messages for troubleshooting

## Error Handling

The library provides detailed error messages to help diagnose issues:

- **Missing Services**: Clear messages when compression/encryption services are not configured
- **Invalid Input**: Validation errors with specific guidance
- **Security**: Path traversal attempts are blocked with descriptive errors
- **File Not Found**: Detailed messages when files cannot be located

## Security

- Path prefixes are validated to prevent directory traversal attacks
- Empty files are rejected
- Encryption requires explicit keyId configuration
- All operations are logged for audit purposes

## Thread Safety

All file storage operations are thread-safe and can be used concurrently from multiple threads.

## Health Checks

`IFileStorageService` extends `IHealth`. Get health directly from the service: `await fileStorage.CheckHealthAsync()`.




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.FileStorage.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Common`
- `Lyo.Compression`
- `Lyo.Encryption`
- `Lyo.Exceptions`
- `Lyo.FileMetadataStore`
- `Lyo.Health`
- `Lyo.Metrics`
- `Lyo.Streams`

## Public API (generated)

Top-level `public` types in `*.cs` (*36*). Nested types and file-scoped namespaces may omit some entries.

- `AllowAllFileContentPolicy`
- `CompletedPart`
- `CompleteMultipartUploadRequest`
- `Constants`
- `DefaultFileContentPolicy`
- `Extensions`
- `FileAuditEventArgs`
- `FileAuditEventType`
- `FileAuditOutcome`
- `FileAuditPublication`
- `FileNotAvailableException`
- `FileOperationContextAccessor`
- `FilePolicyRejectedException`
- `FileSavePolicyContext`
- `FileScanThreatLevel`
- `FileStorageErrorCodes`
- `FileStorageServiceBase`
- `FileStorageServiceBaseOptions`
- `IFileAuditEventHandler`
- `IFileContentPolicy`
- `IFileMalwareScanner`
- `IFileOperationContext`
- `IFileOperationContextAccessor`
- `IFileStorageService`
- `IMultipartUploadService`
- `IMultipartUploadSessionStore`
- `InMemoryMultipartUploadSessionStore`
- `LocalFileStorageService`
- `LocalFileStorageServiceOptions`
- `LocalMultipartUploadService`
- `Metrics`
- `MultipartBeginRequest`
- `MultipartSessionStatus`
- `MultipartUploadProviderKind`
- `NullFileMalwareScanner`
- `NullFileOperationContextAccessor`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]

