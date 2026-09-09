using Lyo.Api.Client;
using Lyo.Exceptions;

namespace Lyo.Seed;

/// <summary>Writes generated items through Lyo.Api bulk create, chunked to <see cref="SeedApiCatalog.MaxBulkAmount"/>.</summary>
public sealed class ApiSeedTransport : ISeedTransport
{
    private readonly IApiClient _client;
    private readonly SeedApiCatalog _catalog;

    /// <summary>Builds a transport over <paramref name="client"/>. <see cref="HttpClient.BaseAddress"/> (or <c>ApiClientOptions.BaseUrl</c>) must already be set.</summary>
    public ApiSeedTransport(IApiClient client, SeedApiCatalog catalog)
    {
        ArgumentHelpers.ThrowIfNull(client);
        ArgumentHelpers.ThrowIfNull(catalog);
        _client = client;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public SeedTransportKind Kind => SeedTransportKind.Api;

    /// <inheritdoc />
    public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
        where T : class
    {
        var binding = _catalog.GetBinding(typeof(T));
        return binding.IsEmpty(_client, binding.Route, ct);
    }

    /// <inheritdoc />
    public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
        where T : class
    {
        ArgumentHelpers.ThrowIfNull(items);
        if (items.Count == 0)
            return Task.CompletedTask;

        var binding = _catalog.GetBinding(typeof(T));
        IReadOnlyList<object> boxed = items.Count == 0 ? [] : items.Cast<object>().ToArray();
        return binding.Persist(_client, binding.Route, boxed, _catalog.MaxBulkAmount, ct);
    }

    /// <summary>Optional replace helper. Issues <c>DELETE {route}/Bulk</c> when the catalog mapped the type with <c>allowDeleteBulk</c>.</summary>
    public Task DeleteAllAsync<T>(CancellationToken ct = default)
        where T : class
    {
        var binding = _catalog.GetBinding(typeof(T));
        if (binding.DeleteAll == null)
            throw new SeedException($"DeleteBulk is not mapped for {typeof(T).Name}.");

        return binding.DeleteAll(_client, binding.Route, ct);
    }
}
