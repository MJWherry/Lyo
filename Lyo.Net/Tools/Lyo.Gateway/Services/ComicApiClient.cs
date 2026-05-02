using System.Net;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Comic.Api.Models;
using Lyo.Comic.Api.Models.Response;
using Lyo.FileMetadataStore.Models;
using Microsoft.Extensions.Options;

namespace Lyo.Gateway.Services;

/// <summary>Typed HTTP client for the Comic API service.</summary>
public interface IComicApiClient : IApiClient
{
    /// <summary>Downloads the raw bytes of a file from the Comic API's file storage.</summary>
    Task<byte[]> GetFileAsync(Guid id, CancellationToken ct = default);

    /// <summary>Downloads multiple files by ID in a single request. Missing IDs are silently omitted from the result.</summary>
    Task<IReadOnlyList<FileBatchEntry>> GetFilesBatchAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>Uploads a file to the Comic API's file storage and returns the stored file metadata.</summary>
    Task<FileStoreResult?> UploadFileAsync(Stream data, string fileName, CancellationToken ct = default);

    /// <summary>Deletes a file from the Comic API's file storage. Returns true if deleted, false if not found.</summary>
    Task<bool> DeleteFileAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the absolute URL for a file served by the Comic API (for use as an img src).</summary>
    string GetFileUrl(Guid id);
}

/// <summary>HTTP client implementation for the Comic API service.</summary>
public sealed class ComicApiClient : ApiClient, IComicApiClient
{
    public ComicApiClient(HttpClient httpClient, IOptions<ComicApiClientOptions> options, JsonSerializerOptions serializerOptions)
        : base(httpClient: httpClient, options: options.Value, serializerOptions: serializerOptions) { }

    public Task<byte[]> GetFileAsync(Guid id, CancellationToken ct = default) => HttpClient.GetByteArrayAsync($"files/{id}", ct);

    public async Task<IReadOnlyList<FileBatchEntry>> GetFilesBatchAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var result = await PostAsAsync<FilesBatchReq, FileBatchEntry[]>("files/batch", new(ids), ct: ct);
        return result ?? [];
    }

    public Task<FileStoreResult?> UploadFileAsync(Stream data, string fileName, CancellationToken ct = default)
        => PostFileAsAsync<FileStoreResult>("files/upload", data, fileName, ct: ct);

    public async Task<bool> DeleteFileAsync(Guid id, CancellationToken ct = default)
    {
        var result = await DeleteAsAsync<bool>($"files/{id}", ct: ct);
        return result;
    }

    public string GetFileUrl(Guid id) => $"{BaseOptions.BaseUrl?.TrimEnd('/')}/files/{id}";
}

public static class ComicApiClientExtensions
{
    public static IServiceCollection AddComicApiClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = ComicApiClientOptions.SectionName)
    {
        var options = new ComicApiClientOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        services.AddSingleton(Options.Create(options));
        services.AddHttpClient<IComicApiClient, ComicApiClient>()
            .ConfigureHttpClient(client => {
                // Set BaseAddress here so it's in place before ApiClient constructor runs
                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                    client.BaseAddress = new(options.BaseUrl.TrimEnd('/') + "/");

                foreach (var enc in (options.AcceptEncodings ?? []).Select(e => e.Trim().ToLowerInvariant()).Where(e => e is "gzip" or "deflate" or "br").Distinct()) {
                    if (client.DefaultRequestHeaders.AcceptEncoding.All(h => !string.Equals(h.Value, enc, StringComparison.OrdinalIgnoreCase)))
                        client.DefaultRequestHeaders.AcceptEncoding.Add(new(enc));
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();
                if (!options.EnableAutoResponseDecompression)
                    return handler;

                var methods = DecompressionMethods.None;
                foreach (var enc in options.AcceptEncodings ?? []) {
                    if (enc.Equals("gzip", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.GZip;

                    if (enc.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.Deflate;

                    if (enc.Equals("br", StringComparison.OrdinalIgnoreCase))
                        methods |= DecompressionMethods.Brotli;
                }

                handler.AutomaticDecompression = methods;
                return handler;
            });

        return services;
    }
}