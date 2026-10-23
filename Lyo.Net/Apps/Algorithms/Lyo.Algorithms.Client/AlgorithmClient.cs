using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Lyo.Algorithms.Models.Response;
using Lyo.Api.Client;
using Lyo.Exceptions;

namespace Lyo.Algorithms.Client;

/// <summary>Typed facade over the Lyo algorithm catalog API. Sit it on any <see cref="IApiClient" /> implementation.</summary>
public sealed class AlgorithmClient : IAlgorithmClient
{
    private readonly IApiClient _api;
    private readonly string? _routePrefix;

    /// <summary>Constructs the client. Null <paramref name="options" /> keeps routes relative to the inner client's base address.</summary>
    public AlgorithmClient(IApiClient apiClient, AlgorithmClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        _api = apiClient;
        _routePrefix = options?.RoutePrefix;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlgorithmDefinitionRes>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _api.GetAsAsync<List<AlgorithmDefinitionRes>>(Route(Constants.Rest.Algorithms.Route), ct: ct).ConfigureAwait(false);
        return rows ?? [];
    }

    /// <inheritdoc />
    public Task<AlgorithmDefinitionRes?> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        return _api.GetAsAsync<AlgorithmDefinitionRes>(Route(Constants.Rest.Algorithms.Definition(key)), ct: ct);
    }

    /// <inheritdoc />
    public Task<AlgorithmDefinitionRes> SaveRevisionAsync(string key, AlgorithmRevisionReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<AlgorithmRevisionReq, AlgorithmDefinitionRes>(Route(Constants.Rest.Algorithms.Revisions(key)), request, ct: ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlgorithmRevisionRes>> GetRevisionsAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var rows = await _api.GetAsAsync<List<AlgorithmRevisionRes>>(Route(Constants.Rest.Algorithms.Revisions(key)), ct: ct).ConfigureAwait(false);
        return rows ?? [];
    }

    /// <inheritdoc />
    public Task<AlgorithmDefinitionRes> RevertAsync(string key, AlgorithmRevertReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<AlgorithmRevertReq, AlgorithmDefinitionRes>(Route(Constants.Rest.Algorithms.Revert(key)), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<AlgorithmLabelRes?> GetLabelAsync(string key, string label, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(label);
        return _api.GetAsAsync<AlgorithmLabelRes>(Route(Constants.Rest.Algorithms.Label(key, label)), ct: ct);
    }

    /// <inheritdoc />
    public Task<AlgorithmLabelRes> SetLabelAsync(string key, string label, AlgorithmLabelReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(label);
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PutAsAsync<AlgorithmLabelReq, AlgorithmLabelRes>(Route(Constants.Rest.Algorithms.Label(key, label)), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<AlgorithmPreviewRes> PreviewAsync(AlgorithmPreviewReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<AlgorithmPreviewReq, AlgorithmPreviewRes>(Route(Constants.Rest.Algorithms.Preview), request, ct: ct);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        return _api.DeleteAsAsync<object>(Route(Constants.Rest.Algorithms.Definition(key)), ct: ct);
    }

    private string Route(string relativePath) => ApiRouteBuilder.Build(_routePrefix, relativePath);
}