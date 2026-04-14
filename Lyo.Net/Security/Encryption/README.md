# Lyo Encryption Library

A production-ready .NET encryption library providing secure, authenticated encryption with support for multiple
algorithms, key management, and envelope encryption patterns.

## 📑 Table of Contents

- [Features](#-features)
- [Target frameworks: net10.0 and netstandard2.0](#-target-frameworks-net100-and-netstandard20)
- [Quick Start](#-quick-start)
- [Usage Examples](#-usage-examples)
    - [AES-GCM Encryption](#1-aes-gcm-encryption)
    - [ChaCha20Poly1305 Encryption](#2-chacha20poly1305-encryption)
    - [RSA Encryption](#3-rsa-encryption)
    - [Hybrid AES-GCM-RSA Encryption](#4-hybrid-aes-gcm-rsa-encryption)
    - [Two-Key (Envelope) Encryption](#5-two-key-envelope-encryption)
    - [Key Management](#6-key-management)
    - [Stream Operations](#7-stream-operations)
    - [Using Direct Keys](#8-using-direct-keys-without-keystore)
    - [Secure Key Generation](#8a-secure-key-generation)
    - [Custom KeyStore Implementation](#9-custom-keystore-implementation)
    - [Service Configuration](#10-service-configuration-options)
    - [Error Handling](#11-error-handling)
    - [Dependency Injection](#12-dependency-injection-aspnet-core)
- [Security Best Practices](#-security-best-practices)
- [Architecture](#️-architecture)
    - [Lyo ciphertext files and metadata](#lyo-ciphertext-files-and-metadata)
- [API Reference](#-api-reference)
- [Performance](#-performance)
- [Thread Safety](#-thread-safety)
- [Important Notes](#-important-notes)
- [Additional Resources](#-additional-resources)

## 🚀 Features

- **Modern Authenticated Encryption**
    - AES-GCM (256-bit keys)
    - ChaCha20Poly1305
    - RSA (with OAEP padding)
    - AES-GCM-RSA (hybrid encryption)

- **Key Management**
    - Multi-tenant key support (keyId-based)
    - Key versioning and rotation support
    - Pluggable KeyStore interface
    - LocalKeyStore for development/local apps
    - Production-ready KeyStore implementations (AWS KMS, Azure Key Vault, etc.)
    - Easy key rotation with `UpdateKey` methods

- **Envelope Encryption**
    - Two-key encryption service for envelope encryption patterns
    - Unique Data Encryption Key (DEK) per encryption operation
    - Key Encryption Key (KEK) stored securely in KeyStore

- **Security Features**
    - Hybrid nonce generation (prevents nonce reuse)
    - Secure memory clearing
    - Constant-time comparisons
    - Input validation and DoS protection
    - Stream format versioning with `StreamFormatVersion` enum for future compatibility

- **Production Ready**
    - Thread-safe operations
    - Comprehensive error handling
    - Extensive test coverage
    - Well-documented API

On-disk layouts, default file extensions, and MIME types for Lyo ciphertext are **Lyo-specific** (registered on `FileTypeInfo` in **Lyo.Common**); they describe interoperability within this stack, not a single industry-wide “AES-GCM file” or “RSA file” format. See [Lyo ciphertext files and metadata](#lyo-ciphertext-files-and-metadata).

## 🎯 Target frameworks: net10.0 and netstandard2.0

**Lyo.Encryption** multi-targets **`net10.0`** and **`netstandard2.0`**. The **supported algorithms and acceptable key material sizes are the same** on both: blobs encrypted on one target decrypt on the other when keys and formats match.

| Algorithm (typical service) | Key / IV sizes | `net10.0` | `netstandard2.0` |
| --- | --- | --- | --- |
| **AES-GCM** (`AesGcmEncryptionService`, DEK layer in `TwoKeyEncryptionService`, data layer in `AesGcmRsaEncryptionService`) | **AES-128 / 192 / 256** → **16, 24, or 32-byte** keys; **12-byte** nonce (96-bit); **16-byte** tag (128-bit) | `System.Security.Cryptography.AesGcm` | BouncyCastle AES-GCM (**same** sizes and on-the-wire layout) |
| **ChaCha20-Poly1305** (`ChaCha20Poly1305EncryptionService`) | **32-byte** key; **12-byte** nonce; **16-byte** tag | `System.Security.Cryptography.ChaCha20Poly1305` | BouncyCastle (**same** sizes and layout) |
| **AES-CCM** (`AesCcmEncryptionService`) | **16 / 24 / 32-byte** keys; **12-byte** nonce; **16-byte** tag | BouncyCastle | BouncyCastle |
| **AES-SIV** (`AesSivEncryptionService`) | **32, 48, or 64-byte** keys (`AesSivKeySizeBits`: 256 / 384 / 512-bit key material per RFC 5297) | Dorssel.Security.Cryptography.AesExtra | Dorssel.Security.Cryptography.AesExtra |
| **XChaCha20-Poly1305** (`XChaCha20Poly1305EncryptionService`) | **32-byte** key; **24-byte** nonce; **16-byte** tag | BouncyCastle | BouncyCastle |
| **RSA** (`RsaEncryptionService`, RSA leg of `AesGcmRsaEncryptionService`) | **≥ 2048-bit** RSA modulus (enforced in `RsaEncryptionService`; **3072+** recommended for new keys). Default **OAEP-SHA256**. Usable plaintext size per operation depends on modulus and padding. | `RSA` + PEM/PFX via BCL (`ImportSubjectPublicKeyInfo` / `ImportPkcs8PrivateKey`, `X509CertificateLoader` for PFX) | `RSA` + **BouncyCastle PEM** import for SPKI/PKCS#8; PFX via `X509Certificate2` |

**Interop:** File extensions, stream headers, and chunk framing are **not** TFM-specific.

**Errors:** On **net10.0**, BCL AEAD decrypt paths may throw **`AuthenticationTagMismatchException`**, which some call sites map to **`DecryptionFailedException`**. On **netstandard2.0**, the same failure is usually a **`CryptographicException`** (still wrapped as **`DecryptionFailedException`** where applicable).

## 🏁 Quick Start

### Basic Encryption with AES-GCM

```csharp
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

// Create a key store and add a key
var keyStore = new LocalKeyStore();
const string keyId = "my-app-key";
keyStore.UpdateKeyFromString(keyId, "my-secret-key");

// Create encryption service
var encryptionService = new AesGcmEncryptionService(keyStore);

// Encrypt data (specify keyId)
var plaintext = "Hello, World!"u8.ToArray();
var encrypted = encryptionService.Encrypt(plaintext, keyId: keyId);

// Decrypt data (keyId is read from encrypted data, or specify explicitly)
var decrypted = encryptionService.Decrypt(encrypted, keyId: keyId);
var decryptedText = System.Text.Encoding.UTF8.GetString(decrypted);
Console.WriteLine(decryptedText); // "Hello, World!"
```

### Choosing the Right Encryption Algorithm

- **AES-GCM** (Recommended) - Best for most use cases
    - Excellent performance (200-800 MB/s)
    - Widely supported and standardized
    - Hardware acceleration available

- **ChaCha20Poly1305** - Modern alternative
    - Excellent performance (300-1000 MB/s)
    - Good for systems without AES hardware acceleration
    - Modern, well-regarded algorithm

- **RSA** - For small data or key exchange
    - Asymmetric encryption
    - Good for encrypting small amounts of data
    - Use for key exchange scenarios

- **AES-GCM-RSA** - Hybrid encryption
    - Combines AES-GCM (data) with RSA (key exchange)
    - Generates random AES key per encryption
    - Good for scenarios requiring asymmetric key exchange

- **Two-Key Encryption** - Envelope encryption
    - Unique DEK per encryption operation
    - KEK stored in KeyStore
    - Best for cloud storage scenarios

## 📚 Usage Examples

### 1. AES-GCM Encryption

AES-GCM is recommended for most use cases - it provides excellent performance and strong security.

```csharp
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

// Setup
const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKeyFromString(keyId, "my-encryption-key");

var service = new AesGcmEncryptionService(keyStore);

// Encrypt bytes (specify keyId)
var data = System.Text.Encoding.UTF8.GetBytes("Sensitive data");
var encrypted = service.Encrypt(data, keyId: keyId);

// Decrypt bytes (keyId is read from encrypted data, or specify explicitly)
var decrypted = service.Decrypt(encrypted, keyId: keyId);
var result = System.Text.Encoding.UTF8.GetString(decrypted);

// Encrypt/Decrypt strings
var encryptedString = service.EncryptString("Hello World", keyId: keyId);
var decryptedString = service.DecryptString(encryptedString, keyId: keyId);

// Encrypt/Decrypt files
await service.EncryptToFileAsync(data, "encrypted.ag", keyId: keyId);
var decryptedData = await service.DecryptFromFileAsync("encrypted.ag", keyId: keyId);

// Stream encryption (for large files)
await using var inputStream = File.OpenRead("large-file.txt");
await using var outputStream = File.Create("large-file.ag");
await service.EncryptToStreamAsync(inputStream, outputStream, keyId: keyId);
```

### 2. ChaCha20Poly1305 Encryption

ChaCha20Poly1305 offers excellent performance and is a modern alternative to AES-GCM.

```csharp
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Keystore;

const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKeyFromString(keyId, "my-key");

var service = new ChaCha20Poly1305EncryptionService(keyStore);

var encrypted = service.Encrypt("Hello"u8.ToArray(), keyId: keyId);
var decrypted = service.Decrypt(encrypted, keyId: keyId);
```

### 3. RSA Encryption

RSA is suitable for encrypting small amounts of data or for key exchange scenarios.

```csharp
using Lyo.Encryption.Rsa;
using System.Security.Cryptography;

// Using PEM files
using var service = new RsaEncryptionService(
    publicPemPath: "public.pem",
    privatePemPath: "private.pem",
    padding: RSAEncryptionPadding.OaepSHA256
);

var encrypted = service.Encrypt("Small data"u8.ToArray());
var decrypted = service.Decrypt(encrypted);

// Using PFX certificate
using var service2 = new RsaEncryptionService(
    pfxPath: "certificate.pfx",
    password: "pfx-password"
);
```

### 4. Hybrid AES-GCM-RSA Encryption

Combines AES-GCM for data encryption with RSA for key exchange. Generates a random AES key per encryption operation.

```csharp
using Lyo.Encryption.AesGcmRsa;

using var service = new AesGcmRsaEncryptionService(
    publicPemPath: "public.pem",
    privatePemPath: "private.pem"
);

// Encrypts with random AES key, encrypts key with RSA
var encrypted = service.Encrypt("Large data"u8.ToArray());

// Decrypts RSA-encrypted key, then decrypts data with AES
var decrypted = service.Decrypt(encrypted);
```

### 5. Two-Key (Envelope) Encryption

Envelope encryption pattern where each encryption uses a unique Data Encryption Key (DEK) that is encrypted with a Key
Encryption Key (KEK) from the KeyStore.

```csharp
using Lyo.Encryption.TwoKey;
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

// Setup KeyStore with KEK
const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKeyFromString(keyId, "master-key");

// Create DEK encryption service (for encrypting data)
var dekService = new AesGcmEncryptionService(keyStore);

// Create two-key service
var twoKeyService = new TwoKeyEncryptionService(dekService, keyStore);

// Encrypt - generates unique DEK per operation (specify keyId)
var result = twoKeyService.Encrypt("Sensitive data"u8.ToArray(), keyId: keyId);
// result.EncryptedData - encrypted data
// result.EncryptedDataEncryptionKey - encrypted DEK
// result.KeyId - keyId used for KEK

// Decrypt (keyId is read from result, or specify explicitly)
var decrypted = twoKeyService.Decrypt(
    result.EncryptedData,
    result.EncryptedDataEncryptionKey,
    keyId: result.KeyId
);
```

### 6. Key Management

#### Understanding keyId and Multi-Tenancy

The encryption library uses a `keyId`-based approach for multi-tenant key management:

- **`keyId`**: A string identifier for a key (e.g., "client-1-key", "tenant-abc-key")
- **`version`**: An integer version number for key rotation (automatically managed)
- **Key Resolution**: When encrypting/decrypting, you can:
    - Provide `keyId` to use the current key from KeyStore
    - Provide `key` directly (takes precedence over `keyId`)
    - For decryption, `keyId` and `version` are read from encrypted data if not provided

```csharp
// Single service can handle multiple clients
var service = new AesGcmEncryptionService(keyStore);

// Encrypt for client 1
var encrypted1 = service.Encrypt(data1, keyId: "client-1-key");

// Encrypt for client 2
var encrypted2 = service.Encrypt(data2, keyId: "client-2-key");

// Decrypt (keyId is read from encrypted data)
var decrypted1 = service.Decrypt(encrypted1);
```

#### Using LocalKeyStore (Development/Local Apps)

```csharp
using Lyo.Keystore;

var keyStore = new LocalKeyStore();
const string keyId = "my-app-key";

// Add initial key (automatically sets version to 1)
keyStore.UpdateKeyFromString(keyId, "key-v1");

// Get current key
var currentKey = keyStore.GetCurrentKey(keyId);

// Get current version
var currentVersion = keyStore.GetCurrentVersion(keyId); // Returns 1

// Get specific version
var v1Key = keyStore.GetKey(keyId, 1);

// Key rotation (automatically increments version)
keyStore.UpdateKeyFromString(keyId, "key-v2"); // Now version 2 is current
keyStore.UpdateKeyFromString(keyId, "key-v3"); // Now version 3 is current

// Old versions remain available for decryption
var v1Key = keyStore.GetKey(keyId, 1); // Still accessible
var v2Key = keyStore.GetKey(keyId, 2); // Still accessible
var v3Key = keyStore.GetKey(keyId, 3); // Current version
```

#### Multi-Tenant Key Management

The KeyStore supports multiple keys (one per client/tenant):

```csharp
var keyStore = new LocalKeyStore();

// Each client/tenant has their own keyId
keyStore.UpdateKeyFromString("client-1-key", "client-1-secret");
keyStore.UpdateKeyFromString("client-2-key", "client-2-secret");
keyStore.UpdateKeyFromString("client-3-key", "client-3-secret");

// Use the appropriate keyId when encrypting/decrypting
var service = new AesGcmEncryptionService(keyStore);
var encrypted = service.Encrypt(data, keyId: "client-1-key");
var decrypted = service.Decrypt(encrypted, keyId: "client-1-key");
```

#### Key Metadata

```csharp
using Lyo.Keystore;

const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKeyFromString(keyId, "my-key");

// Set metadata for current version
var currentVersion = keyStore.GetCurrentVersion(keyId);
var metadata = new KeyMetadata
{
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddYears(1),
    Algorithm = "AES-256-GCM",
    AdditionalData = new Dictionary<string, string>
    {
        { "Description", "Production encryption key" }
    }
};
keyStore.SetKeyMetadata(keyId, currentVersion, metadata);

// Get metadata
var retrievedMetadata = keyStore.GetKeyMetadata(keyId, currentVersion);
if (retrievedMetadata?.IsExpired == true)
{
    // Handle expired key
}
```

### 7. Stream Operations

For large files, use stream operations to avoid loading everything into memory. The library uses **single-pass streaming
** with **no temporary files** for optimal performance:

#### AES-GCM Stream Operations

```csharp
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKeyFromString(keyId, "stream-key");

var service = new AesGcmEncryptionService(keyStore);

// Encrypt large file (single-pass, no temp files)
await using var inputStream = File.OpenRead("large-file.dat");
await using var encryptedStream = File.Create("large-file.ag");
await service.EncryptToStreamAsync(inputStream, encryptedStream, keyId: keyId, chunkSize: 2 * 1024 * 1024); // 2MB chunks

// Decrypt large file
await using var encryptedInput = File.OpenRead("large-file.ag");
await using var decryptedOutput = File.Create("large-file-decrypted.dat");
await service.DecryptToStreamAsync(encryptedInput, decryptedOutput, keyId: keyId);
```

#### Two-Key Encryption Stream Operations

Two-key encryption streams include a structured header with key metadata:

```csharp
using Lyo.Encryption.TwoKey;
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKeyFromString(keyId, "master-key");

var dekService = new AesGcmEncryptionService(keyStore);
var twoKeyService = new TwoKeyEncryptionService(dekService, keyStore);

// Encrypt large file (single-pass streaming with header). Extension is inner DEK service + two-key suffix (e.g. .ag + 2k => .ag2k).
await using var inputStream = File.OpenRead("large-file.dat");
await using var encryptedStream = File.Create("large-file.ag2k");
await twoKeyService.EncryptToStreamAsync(inputStream, encryptedStream, keyId: keyId, chunkSize: 2 * 1024 * 1024);

// Decrypt large file (reads header automatically)
await using var encryptedInput = File.OpenRead("large-file.ag2k");
await using var decryptedOutput = File.Create("large-file-decrypted.dat");
await twoKeyService.DecryptToStreamAsync(encryptedInput, decryptedOutput, keyId: keyId);
```

#### Reading Stream Headers

You can read the encryption header from a stream without decrypting the entire file:

```csharp
using Lyo.Encryption;

// Read header from encrypted file
await using var fileStream = File.OpenRead("large-file.ag2k");
var header = EncryptionHeader.Read(fileStream);

Console.WriteLine($"KeyId: {header.KeyId}");
Console.WriteLine($"KeyVersion: {header.KeyVersion}");
Console.WriteLine($"Format Version: {header.FormatVersion}"); // byte value (1)
Console.WriteLine($"DEK Algorithm ID: {header.DekAlgorithmId}");
Console.WriteLine($"KEK Algorithm ID: {header.KekAlgorithmId}");

// The stream position is now after the header, ready for chunk reading
```

### 8. Using Direct Keys (Without KeyStore)

You can also provide keys directly without using a KeyStore:

```csharp
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;
using System.Security.Cryptography;

// KeyStore is still required for initialization, but you can pass keys directly
const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKeyFromString(keyId, "dummy"); // Required for initialization

var service = new AesGcmEncryptionService(keyStore);

// Generate a key
var key = RandomNumberGenerator.GetBytes(32); // 32 bytes for AES-256

// Encrypt with direct key (ignores KeyStore key, keyId is ignored when key is provided)
var encrypted = service.Encrypt("data"u8.ToArray(), key: key);

// Decrypt with direct key
var decrypted = service.Decrypt(encrypted, key: key);
```

### 8a. Secure Key Generation

```csharp
using Lyo.Keystore;

// Generate secure random key (32 bytes = 256 bits)
var key = SecureKeyGenerator.GenerateKey(32);

// Generate key with salt
var (key, salt) = SecureKeyGenerator.GenerateKeyWithSalt(32, 16);

// Generate secure key string
var keyString = SecureKeyGenerator.GenerateKeyString(32, includeSpecialChars: true);

// Store in KeyStore
const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();
keyStore.UpdateKey(keyId, key);
```

### 9. Custom KeyStore Implementation

For production, implement `IKeyStore` for your key management system:

```csharp
using Lyo.Keystore;
using System.Threading.Tasks;

// Example: Azure Key Vault KeyStore (pseudo-code)
public class AzureKeyVaultKeyStore : IKeyStore
{
    private readonly KeyVaultClient _client;
    private readonly string _vaultUrl;
    
    public AzureKeyVaultKeyStore(KeyVaultClient client, string vaultUrl)
    {
        _client = client;
        _vaultUrl = vaultUrl;
    }
    
    public byte[]? GetCurrentKey(string keyId)
    {
        var version = GetCurrentVersion(keyId);
        return GetKey(keyId, version);
    }
    
    public byte[]? GetKey(string keyId, int version)
    {
        // Retrieve key from Azure Key Vault
        var secret = _client.GetSecretAsync(_vaultUrl, $"{keyId}/v{version}").Result;
        return Convert.FromBase64String(secret.Value);
    }
    
    // Implement other IKeyStore methods...
}

// Usage
var keyStore = new AzureKeyVaultKeyStore(keyVaultClient, "https://myvault.vault.azure.net/");
var encryptionService = new AesGcmEncryptionService(keyStore);
```

### 10. Service Configuration Options

All encryption services use `EncryptionServiceOptions` for configuration. Each service creates default options in its constructor, but you can understand the available options:

```csharp
using Lyo.Encryption;

// Options are automatically configured by each service:
// - CurrentFormatVersion: Format version for encrypted data (defaults to V1, null for RSA/AES-GCM-RSA)
// - MaxInputSize: Maximum allowed input size in bytes (defaults to long.MaxValue)
// - MinInputSize: Minimum allowed input size in bytes (defaults to 1)
// - FileExtension: File extension for encrypted files (e.g., ".ag", ".rsa", ".chacha")

// Services create options automatically:
var service = new AesGcmEncryptionService(keyStore);
// Options are: CurrentFormatVersion=V1, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".ag"

// Note: Services validate that FileExtension is not null or empty during construction
```

**Default Options by Service:**

- **AES-GCM**: FormatVersion=V1, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".ag"
- **ChaCha20Poly1305**: FormatVersion=V1, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".chacha"
- **RSA**: FormatVersion=null, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".rsa"
- **AES-GCM-RSA**: FormatVersion=null, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".agr"
- **Two-Key**: Uses the inner `IEncryptionService.FileExtension` plus `FileTypeInfo.TwoKeyEnvelopeSuffix` (`"2k"`). With AES-GCM as the DEK service, default ciphertext files use **`.ag2k`**.

### 11. Error Handling

The library throws specific exceptions for different error conditions. Always handle exceptions appropriately:

```csharp
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions;
using Lyo.Keystore;

try
{
    const string keyId = "my-app-key";
    var keyStore = new LocalKeyStore();
    keyStore.UpdateKeyFromString(keyId, "key");
    
    var service = new AesGcmEncryptionService(keyStore);
    
    // Empty data is not allowed
    var encrypted = service.Encrypt([]); // Throws ArgumentOutsideRangeException
}
catch (ArgumentNullException ex)
{
    // Null parameter (for nullable parameters like keyId, key)
    Console.WriteLine($"Null parameter: {ex.Message}");
}
catch (ArgumentOutsideRangeException ex)
{
    // Empty data, data too large, or data too small (for decryption)
    // Empty byte arrays and empty strings are rejected
    Console.WriteLine($"Invalid data size: {ex.Message}");
}
catch (ArgumentException ex)
{
    // Invalid parameter (e.g., invalid keyId/key parameters for RSA)
    Console.WriteLine($"Invalid parameter: {ex.Message}");
}
catch (DecryptionFailedException ex)
{
    // Wrong key, corrupted data, authentication failure, or tampered data
    Console.WriteLine($"Decryption failed: {ex.Message}");
}
catch (InvalidDataException ex)
{
    // Invalid encrypted data format, unsupported format version, or corrupted data
    Console.WriteLine($"Invalid data format: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Missing keyId or key parameter, or keyId not found in KeyStore
    Console.WriteLine($"Configuration error: {ex.Message}");
}
catch (FileNotFoundException ex)
{
    // File not found (for file operations)
    Console.WriteLine($"File not found: {ex.Message}");
}
catch (EndOfStreamException ex)
{
    // Stream ended unexpectedly while reading encrypted data
    Console.WriteLine($"Stream error: {ex.Message}");
}
catch (NotSupportedException ex)
{
    // Unsupported stream format version
    Console.WriteLine($"Unsupported format: {ex.Message}");
}
catch (OperationCanceledException ex)
{
    // Operation cancelled via CancellationToken
    Console.WriteLine($"Operation cancelled: {ex.Message}");
}
```

**Exception Reference:**

- `ArgumentNullException` - Thrown when nullable parameters (like `keyId`, `key`) are null and required
- `ArgumentOutsideRangeException` - Thrown when:
    - Data is empty (length < MinInputSize, typically 1)
    - Data exceeds maximum allowed size (MaxInputSize)
    - Encrypted data is too small (below minimum required size for the algorithm)
- `ArgumentException` - Thrown for invalid parameters (e.g., providing keyId/key to RSA service)
- `DecryptionFailedException` - Thrown when decryption fails due to wrong key, corrupted data, authentication failure, or tampered data
- `InvalidDataException` - Thrown when encrypted data format is invalid, unsupported format version, or corrupted
- `InvalidOperationException` - Thrown when no encryption/decryption key is available (neither keyId nor key provided, or keyId not found in KeyStore)
- `FileNotFoundException` - Thrown when a required file does not exist
- `EndOfStreamException` - Thrown when a stream ends unexpectedly while reading encrypted data
- `NotSupportedException` - Thrown when stream format version is not supported
- `OperationCanceledException` - Thrown when an operation is cancelled via CancellationToken

### 12. Dependency Injection (ASP.NET Core)

```csharp
using Lyo.Encryption.Extensions;
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;
using Lyo.Keystore.Extensions;

// In Program.cs or Startup.cs

// Option 1: Register LocalKeyStore and AES-GCM encryption
services.AddLocalKeyStore(configure: keyStore =>
{
    keyStore.UpdateKeyFromString("default-key", "production-key");
});
services.AddAesGcmEncryption<LocalKeyStore>();

// Option 2: Register with custom KeyStore
services.AddSingleton<IKeyStore>(provider => 
{
    var keyStore = new LocalKeyStore();
    keyStore.UpdateKeyFromString("default-key", "production-key");
    return keyStore;
});
services.AddAesGcmEncryption<LocalKeyStore>();

// Option 3: Register Two-Key encryption (envelope encryption)
services.AddLocalKeyStore(configure: keyStore =>
{
    keyStore.UpdateKeyFromString("default-key", "master-key");
});
services.AddTwoKeyEncryption<LocalKeyStore>();

// In your service
public class MyService
{
    private readonly AesGcmEncryptionService _encryption;
    
    public MyService(AesGcmEncryptionService encryption)
    {
        _encryption = encryption;
    }
    
    public byte[] EncryptData(byte[] data, string keyId = "default-key")
    {
        return _encryption.Encrypt(data, keyId: keyId);
    }
}
```

## 🔐 Security Best Practices

### Key Management

1. **Production Key Storage**
    - ❌ **Don't use** `LocalKeyStore` in production servers
    - ✅ **Use** HSM, Azure Key Vault, AWS KMS, or similar
    - ✅ Implement `IKeyStore` interface for your key management system

2. **Key Rotation**
   ```csharp
   const string keyId = "my-app-key";
   
   // Initial key (version 1)
   keyStore.UpdateKeyFromString(keyId, "initial-key");
   
   // Rotate to new key (automatically increments to version 2)
   keyStore.UpdateKeyFromString(keyId, "new-key");
   
   // Old data encrypted with version 1 can still be decrypted
   // New encryptions automatically use version 2
   
   // Rotate again (now version 3)
   keyStore.UpdateKeyFromString(keyId, "latest-key");
   ```

3. **Key Generation**
   ```csharp
   using Lyo.Keystore;
   
   const string keyId = "my-app-key";
   
   // Generate secure random key
   var key = SecureKeyGenerator.GenerateKey(32); // 32 bytes = 256 bits
   
   // Store in KeyStore (automatically sets version to 1)
   keyStore.UpdateKey(keyId, key);
   
   // Or generate and store in one step
   var keyString = SecureKeyGenerator.GenerateKeyString(32);
   keyStore.UpdateKeyFromString(keyId, keyString);
   ```

### Encryption Practices

1. **Always use authenticated encryption** (AES-GCM, ChaCha20Poly1305)
2. **Never reuse nonces** - The library handles this automatically with hybrid nonce generation
3. **Validate input sizes** - The library enforces maximum sizes (configurable via `MaxInputSize` in options) to prevent DoS attacks
4. **Empty data is not allowed** - The library rejects empty byte arrays and empty strings (enforced by `MinInputSize` in options, default 1) to prevent invalid encryption
   operations
5. **Handle exceptions properly** - Don't expose sensitive information in error messages
6. **Service options are configured automatically** - Each service creates appropriate default options, including format version, input size limits, and file extension

### Production Deployment

1. **Use production KeyStore**
   ```csharp
   // Example: Azure Key Vault KeyStore (pseudo-code)
   public class AzureKeyVaultKeyStore : IKeyStore
   {
       // Implementation using Azure Key Vault SDK
   }
   
   var keyStore = new AzureKeyVaultKeyStore();
   var service = new AesGcmEncryptionService(keyStore);
   ```

2. **Monitor encryption failures**
    - Log `DecryptionFailedException` occurrences
    - Alert on repeated failures (possible attack)

3. **Key rotation strategy**
    - Rotate keys periodically (e.g., annually)
    - Keep old keys for decryption
    - Use key expiration metadata

## 🏗️ Architecture

### Lyo ciphertext files and metadata

Algorithms such as AES-GCM and RSA are standardized; **how** ciphertext is framed on disk (headers, chunks, default extensions) is defined by this library and related packages. Those defaults are not interchangeable with arbitrary third-party “`.aes`” or ad-hoc blobs.

- **`FileTypeInfo` (Lyo.Common)** registers human-readable names, canonical extensions (for example `.ag`, `.chacha`, `.ag2k` for two-key with an AES-GCM inner encryptor), and vendor MIME types such as `application/x-lyo-ciphertext-aes-gcm`. Use these when you need consistent content typing or UI labels.
- **Two-key file names** append `FileTypeInfo.TwoKeyEnvelopeSuffix` (`"2k"`) to the inner encryption service’s extension (for example `.ag` → `.ag2k`). `ITwoKeyEncryptionService.FileExtension` exposes the combined value.
- **Lyo.FileStorage** (when resolving stored blobs without explicit metadata) considers `FileTypeInfo.CommonStorageResolutionSuffixes`, which includes stream-compression suffixes and Lyo ciphertext extensions.

### Encryption Services

All encryption services implement `IEncryptionService`:

- `AesGcmEncryptionService` - AES-GCM authenticated encryption
- `ChaCha20Poly1305EncryptionService` - ChaCha20Poly1305 authenticated encryption
- `RsaEncryptionService` - RSA asymmetric encryption
- `AesGcmRsaEncryptionService` - Hybrid AES-GCM + RSA
- `TwoKeyEncryptionService` - Envelope encryption pattern

### Key Management

- `IKeyStore` - Interface for key storage and retrieval with multi-tenant support
- `LocalKeyStore` - In-memory KeyStore for development/local apps
- Production KeyStores should implement `IKeyStore` (e.g., AWS KMS, Azure Key Vault)
- Keys are identified by `keyId` (for multi-tenancy) and `version` (for rotation)
- `UpdateKey` and `UpdateKeyFromString` methods simplify key rotation

### Stream Format

Encryption services use optimized single-pass streaming formats with structured headers:

#### Standard Encryption Services (AES-GCM, ChaCha20Poly1305, etc.)

Stream format: `[FormatVersion: 1 byte][AlgorithmId: 1 byte][Reserved: 2 bytes][Chunks...]`

- **FormatVersion**: `StreamFormatVersion` enum value (currently `V1 = 1`)
- **AlgorithmId**: Algorithm identifier (0=AES-GCM, 1=ChaCha20Poly1305, 2=RSA, 3=AES-GCM-RSA, 4=TwoKey)
- **Reserved**: 2 bytes reserved for future use
- **Chunks**: `[Length: 4 bytes][EncryptedChunk]...` (encrypted data chunks)

#### Two-Key Encryption Service

Stream format:
`[FormatVersion: 1 byte][DEKAlgorithmId: 1 byte][KEKAlgorithmId: 1 byte][KeyIdLength: 4 bytes][KeyId][KeyVersionLength: 4 bytes][KeyVersion][EncryptedDEKLength: 4 bytes][EncryptedDEK][Chunks...]`

- **FormatVersion**: `StreamFormatVersion` enum value (currently `V1 = 1`)
- **DEKAlgorithmId**: Algorithm ID for Data Encryption Key encryption
- **KEKAlgorithmId**: Algorithm ID for Key Encryption Key encryption
- **KeyIdLength**: Length of the KeyId string in bytes
- **KeyId**: UTF-8 encoded key identifier (variable length)
- **KeyVersionLength**: Length of the KeyVersion string in bytes
- **KeyVersion**: String version of the Key Encryption Key (KEK)
- **EncryptedDEKLength**: Length of the encrypted Data Encryption Key
- **EncryptedDEK**: The encrypted DEK (variable length)
- **Chunks**: `[Length: 4 bytes][EncryptedChunk]...` (encrypted data chunks)

#### Byte Array Format (AES-GCM, ChaCha20Poly1305)

Format: `[FormatVersion: 1 byte][KeyIdLength: 4 bytes][KeyId][KeyVersionLength: 4 bytes][KeyVersion][nonceLength: 4 bytes][nonce][tag][ciphertext]`

- **FormatVersion**: `StreamFormatVersion` enum value (currently `V1 = 1`)
- **KeyIdLength**: Length of the KeyId string in bytes (0 if using direct key)
- **KeyId**: UTF-8 encoded key identifier (variable length, omitted if length is 0)
- **KeyVersionLength**: Length of the KeyVersion string in bytes
- **KeyVersion**: String version of the key (empty if using direct key)
- **nonceLength**: Length of the nonce in bytes
- **nonce**: Nonce/IV used for encryption
- **tag**: Authentication tag
- **ciphertext**: Encrypted data

#### Single-Pass Streaming

The `EncryptToStreamAsync` and `DecryptToStreamAsync` methods use **single-pass streaming** with **no temporary files**:

- **Memory Efficient**: Data flows through the pipeline without buffering entire files
- **No Temp Files**: All processing happens in memory using `MemoryStream` for intermediate stages
- **Single Pass**: Data is read once and processed through compression → encryption → output in one pass
- **Header Support**: The `EncryptionHeader` helper class (`Lyo.Encryption`) provides easy reading/writing of the stream
  header
- **Version Management**: `StreamFormatVersion` enum ensures type-safe version handling

#### Using EncryptionHeader Helper

```csharp
using Lyo.Encryption;

// Read header from a stream (two-key example path)
using var fileStream = File.OpenRead("encrypted.ag2k");
var header = EncryptionHeader.Read(fileStream);

// Access header properties
Console.WriteLine($"KeyId: {header.KeyId}");
Console.WriteLine($"KeyVersion: {header.KeyVersion}");
Console.WriteLine($"Format Version: {header.FormatVersion}"); // byte value (1)
Console.WriteLine($"DEK Algorithm ID: {header.DekAlgorithmId}");
Console.WriteLine($"KEK Algorithm ID: {header.KekAlgorithmId}");

// Create a new header with updated values
var updatedHeader = header.With(
    keyId: "new-key-id",
    keyVersion: 2,
    encryptedDataEncryptionKey: newEncryptedDek);

// Write header to a buffer
var buffer = new List<byte>();
updatedHeader.Write(buffer);
```

## 📖 API Reference

### Core Interfaces

- `IEncryptionService` - Core encryption interface
- `IKeyStore` - Key management interface
- `ITwoKeyEncryptionService` - Envelope encryption interface

### Main Classes

- `EncryptionServiceBase` - Base class with common functionality (requires `EncryptionServiceOptions`)
- `AesGcmEncryptionService` - AES-GCM implementation
- `ChaCha20Poly1305EncryptionService` - ChaCha20Poly1305 implementation
- `RsaEncryptionService` - RSA implementation
- `AesGcmRsaEncryptionService` - Hybrid implementation
- `TwoKeyEncryptionService` - Envelope encryption with single-pass streaming
- `LocalKeyStore` - Development KeyStore
- `NonceGenerator` - Hybrid nonce generation utility

### Configuration Classes

- `EncryptionServiceOptions` - Options for configuring encryption service behavior:
    - `CurrentFormatVersion` (byte?) - Format version for encryption (defaults to V1, null for services that don't use it)
    - `MaxInputSize` (long) - Maximum allowed input size in bytes (defaults to long.MaxValue)
    - `MinInputSize` (long) - Minimum allowed input size in bytes (defaults to 1)
    - `FileExtension` (string) - File extension for encrypted files (required, set by each service)

### Helper Classes (Lyo.Encryption)

- `EncryptionHeader` - Sealed record for reading/writing encryption stream headers
- `StreamFormatVersion` - Enum for stream format versions (`Unknown = 0`, `V1 = 1`)
- `EncryptionHeaderVersion` - Enum for encryption header format versions (`Unknown = 0`, `V1 = 1`)

### Exceptions

- `EncryptionException` - Base exception for encryption errors
- `DecryptionFailedException` - Thrown when decryption fails (wrong key, corrupted data, authentication failure, tampered data)
- `ArgumentOutsideRangeException` - Thrown when data is empty, too large, or too small
- `InvalidDataException` - Thrown when encrypted data format is invalid, unsupported format version, or corrupted
- `InvalidOperationException` - Thrown when no encryption/decryption key is available
- `FileNotFoundException` - Thrown when a required file does not exist
- `EndOfStreamException` - Thrown when a stream ends unexpectedly
- `NotSupportedException` - Thrown when stream format version is not supported
- `OperationCanceledException` - Thrown when an operation is cancelled

## 🔍 Performance

Approximate performance on typical hardware:

- **AES-GCM**: 200-800 MB/s
- **ChaCha20Poly1305**: 300-1000 MB/s
- **RSA**: 100-500 MB/s (depends on key size)
- **AES-GCM-RSA**: 100-500 MB/s

For large files, use `EncryptToStreamAsync` / `DecryptToStreamAsync` to avoid memory issues. These methods use *
*single-pass streaming** with **no temporary files**, processing data efficiently through compression → encryption →
output in one pass.

## 🧪 Testing

The library includes comprehensive test coverage. See `Lyo.Encryption.Tests` for examples.

## 📝 Thread Safety

All encryption services are **thread-safe**. Multiple threads can safely call methods concurrently on the same instance.
Each operation uses its own cryptographic context (nonce, key material).

## 🚨 Important Notes

1. **LocalKeyStore is for local use only** - Don't use in production servers
2. **KeyStore must be thread-safe** - If using a custom KeyStore, ensure thread safety
3. **Key security** - Protect your keys! Use secure key storage in production
4. **Nonce management** - The library handles nonce generation automatically (hybrid IV + counter)

## 📚 Additional Resources

- See `PRODUCTION_REVIEW.md` for detailed security analysis
- See XML documentation in code for detailed API documentation
- See test projects for usage examples

## 🤝 Contributing

This is a production library. Before making changes:

1. Review the security implications
2. Ensure thread safety
3. Add comprehensive tests
4. Update documentation

## 🔧 Advanced Usage

### Custom Encoding

```csharp
var service = new AesGcmEncryptionService(keyStore);
service.DefaultEncoding = Encoding.UTF32; // Change default encoding

// Or specify per operation
var encrypted = service.EncryptString("Hello 世界", encoding: Encoding.UTF32);
var decrypted = service.DecryptString(encrypted, encoding: Encoding.UTF32);
```

### Key Rotation Example

```csharp
const string keyId = "my-app-key";
var keyStore = new LocalKeyStore();

// Initial setup (version 1)
keyStore.UpdateKeyFromString(keyId, "initial-key");

var service = new AesGcmEncryptionService(keyStore);
var encryptedV1 = service.Encrypt("data"u8.ToArray(), keyId: keyId);

// Rotate to new key (automatically increments to version 2)
keyStore.UpdateKeyFromString(keyId, "new-key");

// New encryptions use version 2
var encryptedV2 = service.Encrypt("new-data"u8.ToArray(), keyId: keyId);

// Old data can still be decrypted (keyId and version are stored in encrypted data)
// For two-key encryption, keyId is stored in the result
```

### Working with Large Files

The library uses **single-pass streaming** with **no temporary files** for optimal performance:

```csharp
// Encrypt large file efficiently (single-pass, no temp files)
await using var input = File.OpenRead("large-file.dat");
await using var output = File.Create("large-file.ag");

// Use larger chunks for better performance (default is 1MB)
const string keyId = "my-app-key";
await service.EncryptToStreamAsync(input, output, keyId: keyId, chunkSize: 4 * 1024 * 1024); // 4MB chunks

// Decrypt (also single-pass)
await using var encryptedInput = File.OpenRead("large-file.ag");
await using var decryptedOutput = File.Create("large-file-decrypted.dat");
await service.DecryptToStreamAsync(encryptedInput, decryptedOutput, keyId: keyId);
```

#### Performance Benefits

- **No Temporary Files**: All processing happens in memory using `MemoryStream`
- **Single Pass**: Data flows through compression → encryption → output in one pass
- **Memory Efficient**: Only chunks are buffered, not entire files
- **Header Support**: Two-key encryption includes structured headers with `EncryptionHeader` helper

## 📄 License

Lyo is licensed under the [Apache License, Version 2.0](../../../LICENSE). See the repository root for the full text and copyright line.

## 🙏 Acknowledgments

Built with security best practices in mind, following:

- OWASP 2023 recommendations
- NIST SP 800-38D (AES-GCM)
- RFC 9106 (Argon2)
- NIST SP 800-57 (RSA key sizes)

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Encryption.csproj`.)*

**Target frameworks:** `netstandard2.0`, `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `BouncyCastle.Cryptography` | `2.6.2` |
| `Dorssel.Security.Cryptography.AesExtra` | `2.0.0` |
| `Microsoft.Bcl.AsyncInterfaces` | `10.0.0` *(netstandard2.0 only)* |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `8.0.2` *(netstandard2.0)* / `[10,)` *(net10.0)* |
| `System.Threading.Tasks.Extensions` | `4.6.3` *(netstandard2.0 only)* |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Keystore`
- `Lyo.Streams`

## Public API (generated)

Top-level `public` types in `*.cs` (*representative list*). Nested types and file-scoped namespaces may omit some entries.

- `AesCcmEncryptionService`
- `AesGcmEncryptionService`
- `AesGcmHelper`
- `AesGcmRsaEncryptionService`
- `AesSivEncryptionService`
- `ChaCha20Poly1305EncryptionService`
- `ChaCha20Poly1305Helper`
- `DecryptionFailedException`
- `EncryptionAlgorithm`
- `EncryptionAlgorithmDiscovery`
- `EncryptionErrorCodes`
- `EncryptionException`
- `EncryptionHeader`
- `EncryptionHeaderVersion`
- `EncryptionServiceBase`
- `EncryptionServiceExtensions`
- `EncryptionServiceOptions`
- `IEncryptionService`
- `ISymmetricKeyMaterialSize`
- `ITwoKeyEncryptionService`
- `NonceGenerator`
- `RsaEncryptionService`
- `RsaKeyLoader`
- `SecurityUtilities`
- `StreamFormatVersion`
- `TwoKeyDekValidation`
- `TwoKeyEncryptionService`
- `XChaCha20Poly1305EncryptionService`

<!-- LYO_README_SYNC:END -->

