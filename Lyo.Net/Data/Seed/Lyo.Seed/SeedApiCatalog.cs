using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Exceptions;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Seed;

/// <summary>Maps generated request types to Lyo.Api routes (<c>POST {route}/Bulk</c> and <c>POST {route}/QueryConcrete</c>).</summary>
public sealed class SeedApiCatalog
{
    private readonly Dictionary<Type, SeedApiBinding> _bindings = [];

    /// <summary>Max items per bulk POST. Must stay at or below the host's <c>BulkOperationOptions.MaxAmount</c> (starts at 2000).</summary>
    public int MaxBulkAmount { get; set; } = 2000;

    internal IReadOnlyDictionary<Type, SeedApiBinding> Bindings => _bindings;

    /// <summary>Maps <typeparamref name="TRequest"/> to <paramref name="route"/>. Bulk create and query share that CLR type for the response body.</summary>
    public SeedApiCatalog Map<TRequest>(string route)
        where TRequest : class
        => Map<TRequest, TRequest>(route);

    /// <summary>
    /// Maps generated <typeparamref name="TRequest"/> items to <c>{route}/Bulk</c> and emptiness checks to <c>{route}/QueryConcrete</c>, which returns
    /// <typeparamref name="TResponse"/>.
    /// </summary>
    public SeedApiCatalog Map<TRequest, TResponse>(string route, bool allowDeleteBulk = false)
        where TRequest : class
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(route);
        var trimmed = route.Trim().Trim('/');
        _bindings[typeof(TRequest)] = new(
            trimmed,
            PersistCore<TRequest, TResponse>,
            IsEmptyCore<TResponse>,
            allowDeleteBulk ? DeleteCore<TResponse> : null);
        return this;
    }

    internal SeedApiBinding GetBinding(Type requestType)
    {
        if (_bindings.TryGetValue(requestType, out var binding))
            return binding;

        throw new SeedException($"No API route mapped for {requestType.Name}. Call SeedApiCatalog.Map<{requestType.Name}>(route).");
    }

    private static async Task PersistCore<TRequest, TResponse>(IApiClient client, string route, IReadOnlyList<object> items, int maxAmount, CancellationToken ct)
        where TRequest : class
    {
        if (maxAmount <= 0)
            throw new SeedException($"{nameof(MaxBulkAmount)} must be > 0.");

        var typed = new List<TRequest>(items.Count);
        foreach (var item in items) {
            if (item is not TRequest request)
                throw new SeedException($"Expected {typeof(TRequest).Name} but received {item.GetType().Name}.");

            typed.Add(request);
        }

        var errors = new List<string>();
        for (var offset = 0; offset < typed.Count; offset += maxAmount) {
            var take = Math.Min(maxAmount, typed.Count - offset);
            var chunk = typed.GetRange(offset, take);
            var result = await client.PostAsAsync<List<TRequest>, CreateBulkResult<TResponse>>($"{route}/Bulk", chunk, ct: ct).ConfigureAwait(false);
            if (result.FailedCount <= 0)
                continue;

            foreach (var row in result.Results.Where(r => !r.IsSuccess))
                errors.Add(row.Error?.GetFullMessage() ?? row.Error?.Detail ?? "Bulk create row failed.");
        }

        if (errors.Count > 0)
            throw new SeedException($"Bulk create to {route} failed: {string.Join("; ", errors)}");
    }

    private static async Task<bool> IsEmptyCore<TResponse>(IApiClient client, string route, CancellationToken ct)
    {
        var result = await client.PostAsAsync<QueryConcreteReq, QueryRes<TResponse>>($"{route}/QueryConcrete", new() { Amount = 1 }, ct: ct).ConfigureAwait(false);
        if (result.Total is > 0)
            return false;

        return result.Items == null || result.Items.Count == 0;
    }

    private static async Task DeleteCore<TResponse>(IApiClient client, string route, CancellationToken ct)
    {
        var query = await client.PostAsAsync<QueryConcreteReq, QueryRes<TResponse>>($"{route}/QueryConcrete", new() { Amount = 1 }, ct: ct).ConfigureAwait(false);
        if (query.Total is not > 0 && (query.Items == null || query.Items.Count == 0))
            return;

        await client.DeleteAsAsync<object, object>($"{route}/Bulk", new List<Lyo.Api.Models.Common.Request.DeleteRequest> { new(allowMultiple: true) }, ct: ct)
            .ConfigureAwait(false);
    }
}

internal sealed record SeedApiBinding(
    string Route,
    Func<IApiClient, string, IReadOnlyList<object>, int, CancellationToken, Task> Persist,
    Func<IApiClient, string, CancellationToken, Task<bool>> IsEmpty,
    Func<IApiClient, string, CancellationToken, Task>? DeleteAll);
