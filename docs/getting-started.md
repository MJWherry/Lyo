# Getting started

Lyo is a collection of independent NuGet-style packages (one folder per package under [`Lyo.Net/`](../Lyo.Net/)) plus sample apps and tooling. You consume the pieces you need;
there is no single "Lyo" meta-package to install.

## Prerequisites

- **.NET SDK 10** — packages multi-target `net10.0` and, where noted,
  `netstandard2.0`. The shared build settings live in
  [`Lyo.Net/Directory.Build.props`](../Lyo.Net/Directory.Build.props).
- A package source containing the Lyo packages. There is no public feed by default; build them locally (see [Publishing](publishing.md)) into a local feed, then reference that feed
  from your `nuget.config`.
- For the Postgres-backed packages (`*.Postgres`), a reachable PostgreSQL instance. For Redis-backed packages, a Redis instance.

## Build the solution

```bash
dotnet restore Lyo.Net/Lyo.slnx
dotnet build   Lyo.Net/Lyo.slnx -c Release
```

## Produce local packages

The build script packs each library and its Lyo dependencies into a local feed (default `~/nuget-local`):

```bash
# All packages (local packs are 1.0.0-preview)
python3 scripts/nuget/build_nuget.py

# A single package (plus its Lyo dependencies), pinned to a version
python3 scripts/nuget/build_nuget.py -v 1.0.0 Lyo.Encryption

# Release / deploy: no preview label
python3 scripts/nuget/build_nuget.py --release
```

Add the output directory as a NuGet source, then reference packages normally:

```bash
dotnet nuget add source "$HOME/nuget-local" --name lyo-local
dotnet add <your-project> package Lyo.Encryption --version 1.0.0-preview
```

See [Publishing](publishing.md) for version/change-detection behavior.

## A minimal example: authenticated encryption

Most Lyo services are registered through dependency injection, but they also work standalone. Here is AES-GCM encryption with the in-memory key store (development only — see
the [security docs](security/README.md) for production key storage):

```csharp
using Lyo.Encryption.AesGcm;
using Lyo.KeyStore;

var keyStore = new LocalKeyStore();
const string keyId = "my-app-key";
keyStore.UpdateKeyFromString(keyId, "a-strong-secret");

var service = new AesGcmEncryptionService(keyStore);

var ciphertext = service.Encrypt("Hello, World!"u8.ToArray(), keyId: keyId);
var plaintext  = service.Decrypt(ciphertext, keyId: keyId); // keyId is read back from the blob
```

Wired through DI in an ASP.NET Core app:

```csharp
using Lyo.Encryption.Extensions;
using Lyo.KeyStore;

const string keyName = "primary";

builder.Services.AddKeyedLocalKeyStore(keyName, store =>
    store.UpdateKeyFromString("default-key", builder.Configuration["Encryption:KekSecret"]!));

builder.Services.AddEncryptionServiceKeyed(keyName, keyStoreName: keyName);
```

For the full encryption surface area, see
[`Lyo.Net/Security/Encryption/README.md`](../Lyo.Net/Security/Encryption/README.md).

## Where to go next

- The root [`README.md`](../README.md) lists every documented package by area.
- [Architecture](architecture.md) explains how the areas relate and the rules a package must obey.
- [Testing](testing.md) shows how to run tests and benchmarks.
- For the API/query engine, the
  [`Lyo.Api` README](../Lyo.Net/Integration/Api/Lyo.Api/README.md) is the authoritative overview.
