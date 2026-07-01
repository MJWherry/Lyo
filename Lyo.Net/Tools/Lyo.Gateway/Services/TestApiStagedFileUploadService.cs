using Lyo.Api.Client;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Staged;

namespace Lyo.Gateway.Services;

/// <summary>HTTP proxy for <see cref="IStagedFileUploadService" /> TestApi workbench endpoints.</summary>
public sealed class TestApiStagedFileUploadService : IStagedFileUploadService
{
    private readonly IApiClient _apiClient;
    private readonly string _routePrefix;

    public TestApiStagedFileUploadService(IApiClient apiClient, string routePrefix)
    {
        _apiClient = apiClient;
        _routePrefix = routePrefix.Trim('/');
    }

    public Task<StagedUploadBeginResult> BeginAsync(StagedUploadBeginRequest request, CancellationToken ct = default)
        => _apiClient.PostAsAsync<StagedUploadBeginRequest, StagedUploadBeginResult>(BuildUri("stage/begin"), request, ct: ct);

    public Task<StagedFileResult> CompleteAsync(Guid stageId, StagedUploadCompleteRequest? request = null, CancellationToken ct = default)
        => _apiClient.PostAsAsync<StagedUploadCompleteRequest?, StagedFileResult>(BuildUri($"stage/{stageId:D}/complete"), request, ct: ct);

    public Task<FileStoreResult> CommitAsync(Guid stageId, StagedUploadCommitRequest request, CancellationToken ct = default)
        => _apiClient.PostAsAsync<StagedUploadCommitRequest, FileStoreResult>(BuildUri($"stage/{stageId:D}/commit"), request, ct: ct);

    public Task AbortAsync(Guid stageId, CancellationToken ct = default) => _apiClient.PostAsAsync<object>(BuildUri($"stage/{stageId:D}/abort"), ct: ct);

    public async Task<StagedFileResult> GetAsync(Guid stageId, CancellationToken ct = default)
    {
        var result = await _apiClient.GetAsAsync<StagedFileResult>(BuildUri($"stage/{stageId:D}"), ct: ct).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException($"Stage endpoint returned no payload for '{stageId}'.");
    }

    private string BuildUri(string relativePath) => $"{_routePrefix}/{relativePath}";

#pragma warning disable CS0067
    public event EventHandler<StagedUploadPresignedCreatedEventArgs>? PresignedCreated;

    public event EventHandler<StagedUploadCompletedEventArgs>? UploadCompleted;

    public event EventHandler<StagedUploadFailedEventArgs>? UploadFailed;

    public event EventHandler<StagedUploadCommittedEventArgs>? Committed;
#pragma warning restore CS0067
}