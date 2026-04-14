using Lyo.Api.Client;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Web.Components.Services;

namespace Lyo.Gateway.Services;

public sealed class TestApiFileStorageWorkbenchQueryService : IFileStorageWorkbenchQueryService
{
    private readonly IApiClient _apiClient;
    private readonly string _routePrefix;

    public TestApiFileStorageWorkbenchQueryService(IApiClient apiClient, string routePrefix)
    {
        _apiClient = apiClient;
        _routePrefix = routePrefix.Trim('/');
    }

    public async Task<IReadOnlyList<FileStoreResult>> SearchFilesAsync(FileStorageWorkbenchFileQuery query, CancellationToken ct = default)
        => await _apiClient.GetAsAsync<object, List<FileStoreResult>>(
                BuildUri("files/search"), new {
                    searchText = query.SearchText,
                    keyId = query.KeyId,
                    keyVersion = query.KeyVersion,
                    take = query.Take
                }, ct: ct)
            .ConfigureAwait(false) ?? [];

    public async Task<IReadOnlyList<FileStorageWorkbenchKeyRecord>> SearchKeysAsync(FileStorageWorkbenchKeyQuery query, CancellationToken ct = default)
    {
        var results = await _apiClient.GetAsAsync<object, List<FileStorageWorkbenchApiKeyRecord>>(
                BuildUri("keys/search"), new { searchText = query.SearchText, take = query.Take }, ct: ct)
            .ConfigureAwait(false) ?? [];

        return results.Select(i => new FileStorageWorkbenchKeyRecord(i.KeyId, i.Version, i.IsCurrent, i.Metadata, i.FileCount)).ToList();
    }

    private string BuildUri(string relativePath) => $"{_routePrefix}/{relativePath}";
}