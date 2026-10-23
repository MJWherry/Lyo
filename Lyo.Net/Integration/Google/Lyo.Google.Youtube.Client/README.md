# Lyo.Google.Youtube.Client

Calls the YouTube Data API v3 and returns models from `Lyo.Google.Youtube.Models`. Does not reference `Lyo.Video`. The host receives models and passes a `YoutubeVideoLink` into the
video store. There is no YouTube schema and no sync worker.

## Features

- **Reads.** `GetVideoAsync`, `ListChannelUploadsAsync`, and `ListPlaylistItemsAsync`.
- **DI.** `AddGoogleYoutubeClient`, the options-instance overload, and `AddGoogleYoutubeClientFromConfiguration` (section `GoogleYoutube`). `ApiKey` is required.

## Examples

### Registration

```csharp
services.AddGoogleYoutubeClient(o => o.ApiKey = apiKey);
var video = await client.GetVideoAsync(videoId, ct);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Google.Youtube.Models` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)